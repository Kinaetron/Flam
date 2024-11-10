﻿using Flam.Shapes;
using Flam.src.Shapes;
using MoonWorks;
using MoonWorks.Graphics;
using System.Numerics;
using System.Runtime.InteropServices;

using Buffer = MoonWorks.Graphics.Buffer;

namespace Flam.Graphics;

public class ShapeBatcher
{
    private int _circleCount = 0;
    private int _rectangleCount = 0;
    const int MAX_FILLED_RECTANGLE_COUNT = 4096;
    const int MAX_WIRE_CIRCLE_COUNT = 1;
    const int FILLED_RECTANGLE_INDEX_COUNT = 6;
    const int FILLED_RECTANGLE_VERTEX_COUNT = 4;
    const int CIRCLE_LINE_VERTEX_COUNT = 2;

    struct RectangleInstanceData
    {
        public Vector3 Position;
        public float Rotation;
        public Vector2 Size;
        public Vector4 Color;
    }

    struct CircleInstanceData
    {
        public float Rotation;
        public Vector2 Position;
        public Vector4 Color;
    }

    RectangleInstanceData[] InstanceData = new RectangleInstanceData[MAX_FILLED_RECTANGLE_COUNT];

    [StructLayout(LayoutKind.Explicit, Size = 32)]
    public struct PositionColorVertex : IVertexType
    {
        [FieldOffset(0)]
        public Vector4 Position;

        [FieldOffset(16)]
        public Vector4 Color;

        public static VertexElementFormat[] Formats { get; } =
        [
            VertexElementFormat.Float4,
            VertexElementFormat.Float4
        ];

        public static uint[] Offsets { get; } =
        [0, 16];

        public override string ToString()
        {
            return Position + " | " + Color;
        }
    }

    private Color _clearColor;
    private readonly Window _window;
    private readonly GraphicsDevice _graphicsDevice;

    private readonly Matrix4x4 _worldSpace;
    private Matrix4x4 _batchMatrix = Matrix4x4.Identity;


    private Buffer _filledRectangleIndexBuffer;
    private Buffer _filledRectangleVertexBuffer;
    private TransferBuffer _filledRectangleVertexTransferBuffer;
    private GraphicsPipeline _filledRectangleRenderPipeline;

    private Buffer _lineCircleVertexBuffer;
    private TransferBuffer _lineCircleVertexTransferBuffer;
    private GraphicsPipeline _lineCircleRenderPipeline;

    private Shader _vertexShader;
    private Shader _fragmentShader;

    public ShapeBatcher(Window window, GraphicsDevice graphicsDevice)
    {
        _window  = window;
        _graphicsDevice = graphicsDevice;

        _vertexShader = ShaderCross.Create(
         _graphicsDevice,
         $"{SDL3.SDL.SDL_GetBasePath()}/shaders/ColorPositonMatrix.vert.hlsl",
         "main",
          ShaderCross.ShaderFormat.HLSL,
          ShaderStage.Vertex,
          new ShaderCross.ShaderResourceInfo
          {
              NumUniformBuffers = 1
          });

        _fragmentShader = ShaderCross.Create(
            _graphicsDevice,
            $"{SDL3.SDL.SDL_GetBasePath()}/shaders/Color.frag.hlsl",
            "main",
             ShaderCross.ShaderFormat.HLSL,
             ShaderStage.Fragment
             );

        _worldSpace = Matrix4x4.CreateOrthographicOffCenter(
            0,
            _window.Width,
            _window.Height,
            0,
            0,
            -1f);

        LineCirclePipelineInitalization();
        FilledRectanglePipelineInitalization();
    }

    private void LineCirclePipelineInitalization()
    {
        var renderPipelineCreateInfo = new GraphicsPipelineCreateInfo
        {
            TargetInfo = new GraphicsPipelineTargetInfo
            {
                ColorTargetDescriptions = [
                   new ColorTargetDescription
                    {
                        Format = _window.SwapchainFormat,
                        BlendState = ColorTargetBlendState.Opaque
                    }
               ]
            },
            DepthStencilState = DepthStencilState.Disable,
            MultisampleState = MultisampleState.None,
            PrimitiveType = PrimitiveType.LineStrip,
            RasterizerState = RasterizerState.CCW_CullNone,
            VertexInputState = VertexInputState.CreateSingleBinding<PositionColorVertex>(),
            VertexShader = _vertexShader,
            FragmentShader = _fragmentShader
        };

        _lineCircleRenderPipeline =
            GraphicsPipeline.Create(_graphicsDevice, renderPipelineCreateInfo);

        _lineCircleVertexBuffer = Buffer.Create<PositionColorVertex>(
           _graphicsDevice,
           BufferUsageFlags.Vertex,
           MAX_WIRE_CIRCLE_COUNT * CIRCLE_LINE_VERTEX_COUNT);

        _lineCircleVertexTransferBuffer = TransferBuffer.Create<PositionColorVertex>(
            _graphicsDevice,
            TransferBufferUsage.Upload,
            MAX_WIRE_CIRCLE_COUNT * CIRCLE_LINE_VERTEX_COUNT);
    }

    private void FilledRectanglePipelineInitalization()
    {
        var renderPipelineCreateInfo = new GraphicsPipelineCreateInfo
        {
            TargetInfo = new GraphicsPipelineTargetInfo
            {
                ColorTargetDescriptions = [
                   new ColorTargetDescription
                    {
                        Format = _window.SwapchainFormat,
                        BlendState = ColorTargetBlendState.Opaque
                    }
               ]
            },
            DepthStencilState = DepthStencilState.Disable,
            MultisampleState = MultisampleState.None,
            PrimitiveType = PrimitiveType.TriangleList,
            RasterizerState = RasterizerState.CCW_CullNone,
            VertexInputState = VertexInputState.CreateSingleBinding<PositionColorVertex>(),
            VertexShader = _vertexShader,
            FragmentShader = _fragmentShader
        };

        _filledRectangleRenderPipeline = 
            GraphicsPipeline.Create(_graphicsDevice, renderPipelineCreateInfo);

        _filledRectangleVertexBuffer = Buffer.Create<PositionColorVertex>(
            _graphicsDevice,
            BufferUsageFlags.Vertex,
            MAX_FILLED_RECTANGLE_COUNT * FILLED_RECTANGLE_VERTEX_COUNT);

        _filledRectangleVertexTransferBuffer = TransferBuffer.Create<PositionColorVertex>(
            _graphicsDevice,
            TransferBufferUsage.Upload,
            MAX_FILLED_RECTANGLE_COUNT * FILLED_RECTANGLE_VERTEX_COUNT);

        _filledRectangleIndexBuffer = Buffer.Create<uint>(
            _graphicsDevice,
            BufferUsageFlags.Index,
            MAX_FILLED_RECTANGLE_COUNT * FILLED_RECTANGLE_INDEX_COUNT);

        var filledRectangleIndexTransferBuffer = TransferBuffer.Create<uint>(
            _graphicsDevice,
            TransferBufferUsage.Upload,
            MAX_FILLED_RECTANGLE_COUNT * FILLED_RECTANGLE_INDEX_COUNT);

        var indexSpan = filledRectangleIndexTransferBuffer.Map<uint>(false);
        for (int i = 0, j = 0; i < MAX_FILLED_RECTANGLE_COUNT * FILLED_RECTANGLE_INDEX_COUNT; 
            i += FILLED_RECTANGLE_INDEX_COUNT, j += 4)
        {
            indexSpan[i]     =  (uint)j;
            indexSpan[i + 1] =  (uint)j + 1;
            indexSpan[i + 2] =  (uint)j + 2;
            indexSpan[i + 3] =  (uint)j + 3;
            indexSpan[i + 4] =  (uint)j + 2;
            indexSpan[i + 5] =  (uint)j + 1;
        }
        filledRectangleIndexTransferBuffer.Unmap();

        var commandBuffer = _graphicsDevice.AcquireCommandBuffer();
        var copyPass = commandBuffer.BeginCopyPass();
        copyPass.UploadToBuffer(
            filledRectangleIndexTransferBuffer, 
            _filledRectangleIndexBuffer, 
            false);
        commandBuffer.EndCopyPass(copyPass);
        _graphicsDevice.Submit(commandBuffer);
    }

    public void Begin(Color clearColor, Matrix4x4 matrix)
    {
        _batchMatrix = matrix;
        _clearColor = clearColor;
    }

    public void DrawLineCircle(Circle circle, Color color)
    {
        DrawLineCircle(new Vector3(circle.Position, 0), circle.Radius, color);
    }

    public void DrawLineCircle(Vector3 position, float radius, Color color)
    {
        if (_circleCount >= MAX_WIRE_CIRCLE_COUNT) {
            End();
        }

        float angleStep = (float)(2 * Math.PI / CIRCLE_LINE_VERTEX_COUNT);

        var dataSpan = _lineCircleVertexTransferBuffer
           .Map<PositionColorVertex>(true);

        //for (int i = 0; i < CIRCLE_LINE_VERTEX_COUNT; i++)
        //{
        //    float theta = i * angleStep;
        //    float x = position.X + radius * (float)Math.Cos(theta);
        //    float y = position.Y + radius * (float)Math.Sin(theta);


        //    dataSpan[_circleCount * CIRCLE_LINE_VERTEX_COUNT + i] = new PositionColorVertex
        //    {
        //        Position = new Vector4(x, y, 0, 1),
        //        Color = color.ToVector4()
        //    };
        //}

        //int circleIndex = _circleCount * CIRCLE_LINE_VERTEX_COUNT;
        //dataSpan[circleIndex + CIRCLE_LINE_VERTEX_COUNT] = new PositionColorVertex
        //{
        //    Position = dataSpan[circleIndex].Position,
        //    Color = color.ToVector4()
        //};

        dataSpan[0] = new PositionColorVertex
        {
            Position = new Vector4(100, 100, 0, 1),
            Color = color.ToVector4()
        };

        dataSpan[1] = new PositionColorVertex
        {
            Position = new Vector4(100, 200, 0, 1),
            Color = color.ToVector4()
        };

        _lineCircleVertexTransferBuffer.Unmap();
        _circleCount++;
    }

    public void DrawFilledRectangle(Rectangle rectangle, float rotation, Color color)
    {
        DrawFilledRectangle(
            new Vector3(rectangle.Position, 0f), 
            rotation, 
            new Vector2(rectangle.Width, rectangle.Height), 
            color);
    }

    public void DrawFilledRectangle(Vector3 position, float rotation, Vector2 size, Color color)
    {
        if(_rectangleCount >= MAX_FILLED_RECTANGLE_COUNT) {
            End();
        }

        InstanceData[_rectangleCount] = new RectangleInstanceData
        {
            Position = position,
            Rotation = rotation,
            Size = size,
            Color = color.ToVector4()
        };

        var dataSpan = _filledRectangleVertexTransferBuffer
            .Map<PositionColorVertex>(true);

        var transform =
                   Matrix4x4.CreateScale(InstanceData[_rectangleCount].Size.X, InstanceData[_rectangleCount].Size.Y, 1) *
                   Matrix4x4.CreateRotationZ(InstanceData[_rectangleCount].Rotation) *
                   Matrix4x4.CreateTranslation(InstanceData[_rectangleCount].Position);

        dataSpan[_rectangleCount*FILLED_RECTANGLE_VERTEX_COUNT] = new PositionColorVertex
        {
            Position = new Vector4(Vector3.Transform(new Vector3(0, 0, 0), transform), 1),
            Color = InstanceData[_rectangleCount].Color
        };

        dataSpan[_rectangleCount*FILLED_RECTANGLE_VERTEX_COUNT + 1] = new PositionColorVertex
        {
            Position = new Vector4(Vector3.Transform(new Vector3(1, 0, 0), transform), 1),
            Color = InstanceData[_rectangleCount].Color
        };

        dataSpan[_rectangleCount*FILLED_RECTANGLE_VERTEX_COUNT + 2] = new PositionColorVertex
        {
            Position = new Vector4(Vector3.Transform(new Vector3(0, 1, 0), transform), 1),
            Color = InstanceData[_rectangleCount].Color
        };

        dataSpan[_rectangleCount*FILLED_RECTANGLE_VERTEX_COUNT + 3] = new PositionColorVertex
        {
            Position = new Vector4(Vector3.Transform(new Vector3(1, 1, 0), transform), 1),
            Color = InstanceData[_rectangleCount].Color
        };

        _filledRectangleVertexTransferBuffer.Unmap();

        _rectangleCount++;
    }
    public void End()
    {
        var commandBuffer = _graphicsDevice.AcquireCommandBuffer();
        var swapchainTexture = commandBuffer.AcquireSwapchainTexture(_window);

        if(swapchainTexture != null)
        {
            _batchMatrix *= _worldSpace;

            var copyPass = commandBuffer.BeginCopyPass();
            copyPass.UploadToBuffer(
                _filledRectangleVertexTransferBuffer,
                _filledRectangleVertexBuffer,
                true);

            copyPass.UploadToBuffer(
              _lineCircleVertexTransferBuffer,
              _lineCircleVertexBuffer,
              true);
            commandBuffer.EndCopyPass(copyPass);

            var renderPass = commandBuffer.BeginRenderPass(
            new ColorTargetInfo(swapchainTexture, _clearColor));

            renderPass.BindGraphicsPipeline(_filledRectangleRenderPipeline);
            renderPass.BindVertexBuffer(_filledRectangleVertexBuffer);
            renderPass.BindIndexBuffer(_filledRectangleIndexBuffer, IndexElementSize.ThirtyTwo);
            commandBuffer.PushVertexUniformData(_batchMatrix);
            renderPass.DrawIndexedPrimitives(
                MAX_FILLED_RECTANGLE_COUNT * FILLED_RECTANGLE_INDEX_COUNT, 1, 0, 0, 0);

            renderPass.BindGraphicsPipeline(_lineCircleRenderPipeline);
            renderPass.BindVertexBuffer(_lineCircleVertexBuffer);
            commandBuffer.PushVertexUniformData(_batchMatrix);
            renderPass.DrawPrimitives(MAX_WIRE_CIRCLE_COUNT * CIRCLE_LINE_VERTEX_COUNT, 1, 0, 0);

            commandBuffer.EndRenderPass(renderPass);

            _circleCount = 0;
            _rectangleCount = 0;
        }

        _graphicsDevice.Submit(commandBuffer);
    }
}