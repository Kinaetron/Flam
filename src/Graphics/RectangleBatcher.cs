using MoonWorks;
using MoonWorks.Graphics;
using System.Numerics;
using System.Runtime.InteropServices;

using Buffer = MoonWorks.Graphics.Buffer;

namespace Flam.Graphics;

public class RectangleBatcher
{
    [StructLayout(LayoutKind.Explicit, Size =48)]
    struct ComputeQuadData
    {
        [FieldOffset(0)]
        public Vector3 Position;

        [FieldOffset(12)]
        public float Rotation;

        [FieldOffset(16)]
        public Vector2 Size;

        [FieldOffset(32)]
        public Vector4 Color;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PositionColorVertex : IVertexType
    {
        public Vector3 Position;
        public Color Color;

        public PositionColorVertex(Vector3 position, Color color)
        {
            Position = position;
            Color = color;
        }

        public static VertexElementFormat[] Formats { get; } =
        [
            VertexElementFormat.Float3,
        VertexElementFormat.Ubyte4Norm
        ];

        public static uint[] Offsets { get; } =
        [0, 12];

        public override string ToString()
        {
            return Position + " | " + Color;
        }
    }

    private int _rectangleCount = 0;
    const int MAX_QUAD_COUNT = 8192;

    private readonly Window _window;
    private readonly GraphicsPipeline _renderPipeline;
    private readonly  ComputePipeline _computePipeline;
    private readonly GraphicsDevice _graphicsDevice;

    private Color _clearColor;
    private readonly Matrix4x4 _worldSpace;
    private Matrix4x4 _batchMatrix = Matrix4x4.Identity;

    private readonly Buffer _quadComputeBuffer;
    private readonly Buffer _quadVertexBuffer;
    private readonly Buffer _quadIndexBuffer;
    private readonly TransferBuffer _quadComputeTransferBuffer;


    public RectangleBatcher(Window window, GraphicsDevice graphicsDevice)
    {
        _window  = window;
        _graphicsDevice = graphicsDevice;

        var vertexShader = ShaderCross.Create(
            _graphicsDevice,
            $"{SDL3.SDL.SDL_GetBasePath()}/shaders/Quad.vert.hlsl",
            "main",
            new ShaderCross.ShaderCreateInfo
            {
                Format = ShaderCross.ShaderFormat.HLSL,
                Stage = ShaderStage.Vertex,
                NumUniformBuffers = 1
            });

        var fragmentShader = ShaderCross.Create(
            _graphicsDevice,
            $"{SDL3.SDL.SDL_GetBasePath()}/shaders/Quad.frag.hlsl",
            "main",
            new ShaderCross.ShaderCreateInfo
            {
                Format = ShaderCross.ShaderFormat.HLSL,
                Stage = ShaderStage.Fragment,
            });

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
            VertexInputState = VertexInputState.Empty,
            VertexShader = vertexShader,
            FragmentShader = fragmentShader
        };

        _renderPipeline = GraphicsPipeline.Create(_graphicsDevice, renderPipelineCreateInfo);

        _computePipeline = ShaderCross.Create(
           _graphicsDevice,
           $"{SDL3.SDL.SDL_GetBasePath()}/shaders/QuadBatch.comp.hlsl",
           "main",
           new ShaderCross.ComputePipelineCreateInfo
           {
               Format = ShaderCross.ShaderFormat.HLSL,
               NumReadonlyStorageBuffers = 1,
               NumReadWriteStorageBuffers = 1,
               ThreadCountX = 64,
               ThreadCountY = 1,
               ThreadCountZ = 1
           });

        _quadComputeTransferBuffer = TransferBuffer.Create<ComputeQuadData>(
            _graphicsDevice,
            TransferBufferUsage.Upload,
            MAX_QUAD_COUNT);

        _quadComputeBuffer = Buffer.Create<ComputeQuadData>(
            _graphicsDevice,
            BufferUsageFlags.ComputeStorageRead,
            MAX_QUAD_COUNT);

        _quadVertexBuffer = Buffer.Create<PositionColorVertex>(
            _graphicsDevice,
            BufferUsageFlags.ComputeStorageWrite | BufferUsageFlags.Vertex,
            MAX_QUAD_COUNT * 4);

        _quadIndexBuffer = Buffer.Create<uint>(
            _graphicsDevice,
            BufferUsageFlags.Index,
            MAX_QUAD_COUNT * 6);

        var quadIndexTransferBuffer = TransferBuffer.Create<uint>(
            _graphicsDevice,
            TransferBufferUsage.Upload,
            MAX_QUAD_COUNT * 6);

        var indexSpan = quadIndexTransferBuffer.Map<uint>(false);
        for (int i = 0, j = 0; i < MAX_QUAD_COUNT * 6; i += 6, j += 4)
        {
            indexSpan[i]     =  (uint)j;
            indexSpan[i + 1] =  (uint)j + 1;
            indexSpan[i + 2] =  (uint)j + 2;
            indexSpan[i + 3] =  (uint)j + 3;
            indexSpan[i + 4] =  (uint)j + 2;
            indexSpan[i + 5] =  (uint)j + 1;
        }
        quadIndexTransferBuffer.Unmap();

        var commandBuffer = _graphicsDevice.AcquireCommandBuffer();
        var copyPass = commandBuffer.BeginCopyPass();
        copyPass.UploadToBuffer(quadIndexTransferBuffer, _quadIndexBuffer, false);
        commandBuffer.EndCopyPass(copyPass);
        _graphicsDevice.Submit(commandBuffer);

        _worldSpace = Matrix4x4.CreateOrthographicOffCenter(
            0,
            _window.Width,
            _window.Height,
            0,
            0,
            -1f);
    }

    public void Begin(Color clearColor, Matrix4x4 matrix)
    {
        _batchMatrix = matrix;
        _clearColor = clearColor;
    }

    public void Draw(Vector3 position, float rotation, Vector2 size, Color color)
    {
        var data = _quadComputeTransferBuffer.Map<ComputeQuadData>(true);

        data[_rectangleCount].Position = position;
        data[_rectangleCount].Rotation = rotation;
        data[_rectangleCount].Size = size;
        data[_rectangleCount].Color = color.ToVector4();
        _quadComputeTransferBuffer.Unmap();

        _rectangleCount++;
    }
    public void End()
    {
        _rectangleCount = 0;
        var commandBuffer = _graphicsDevice.AcquireCommandBuffer();
        var swapchainTexture = commandBuffer.AcquireSwapchainTexture(_window);

        if(swapchainTexture != null)
        {
            var copyPass = commandBuffer.BeginCopyPass();
            copyPass.UploadToBuffer(_quadComputeTransferBuffer, _quadComputeBuffer, true);
            commandBuffer.EndCopyPass(copyPass);

            var computePass = commandBuffer.BeginComputePass(
                new StorageBufferReadWriteBinding(_quadVertexBuffer, true)
            );

            computePass.BindComputePipeline(_computePipeline);
            computePass.BindStorageBuffer(_quadComputeBuffer);
            computePass.Dispatch(MAX_QUAD_COUNT / 64, 1, 1);

            commandBuffer.EndComputePass(computePass);

            var renderPass = commandBuffer.BeginRenderPass(
                new ColorTargetInfo(swapchainTexture, _clearColor)
            );

            commandBuffer.PushVertexUniformData(_worldSpace);

            renderPass.BindGraphicsPipeline(_renderPipeline);
            renderPass.BindVertexBuffer(_quadVertexBuffer);
            renderPass.BindIndexBuffer(_quadIndexBuffer, IndexElementSize.ThirtyTwo);
            renderPass.DrawIndexedPrimitives(MAX_QUAD_COUNT * 6, 1, 0, 0, 0);

            commandBuffer.EndRenderPass(renderPass);
        }

        _graphicsDevice.Submit(commandBuffer);
    }
}