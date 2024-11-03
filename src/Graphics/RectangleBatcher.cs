using MoonWorks;
using MoonWorks.Graphics;
using System.Numerics;
using System.Runtime.InteropServices;

using Buffer = MoonWorks.Graphics.Buffer;

namespace Flam.Graphics;

public class RectangleBatcher
{
    private int _quadCount = 0;
    const int MAX_QUAD_COUNT = 8192;

    struct SpriteInstanceData
    {
        public Vector3 Position;
        public float Rotation;
        public Vector2 Size;
        public Vector4 Color;
    }

    SpriteInstanceData[] InstanceData = new SpriteInstanceData[MAX_QUAD_COUNT];

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

    private readonly Window _window;
    private readonly GraphicsPipeline _renderPipeline;
    private readonly GraphicsDevice _graphicsDevice;
    private readonly TransferBuffer _quadVertexTransferBuffer;

    private Color _clearColor;
    private readonly Matrix4x4 _worldSpace;
    private Matrix4x4 _batchMatrix = Matrix4x4.Identity;

    private readonly Buffer _quadVertexBuffer;
    private readonly Buffer _quadIndexBuffer;


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
            VertexInputState = VertexInputState.CreateSingleBinding<PositionColorVertex>(),
            VertexShader = vertexShader,
            FragmentShader = fragmentShader
        };

        _renderPipeline = GraphicsPipeline.Create(_graphicsDevice, renderPipelineCreateInfo);

        _quadVertexBuffer = Buffer.Create<PositionColorVertex>(
            _graphicsDevice,
            BufferUsageFlags.Vertex,
            MAX_QUAD_COUNT * 4);

        _quadVertexTransferBuffer = TransferBuffer.Create<PositionColorVertex>(
            _graphicsDevice,
            TransferBufferUsage.Upload,
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
        InstanceData[_quadCount] = new SpriteInstanceData
        {
            Position = position,
            Rotation = rotation,
            Size = size,
            Color = color.ToVector4()
        };

        var dataSpan = _quadVertexTransferBuffer.Map<PositionColorVertex>(true);

        var transform =
                   Matrix4x4.CreateScale(InstanceData[_quadCount].Size.X, InstanceData[_quadCount].Size.Y, 1) *
                   Matrix4x4.CreateRotationZ(InstanceData[_quadCount].Rotation) *
                   Matrix4x4.CreateTranslation(InstanceData[_quadCount].Position);

        dataSpan[_quadCount*4] = new PositionColorVertex
        {
            Position = new Vector4(Vector3.Transform(new Vector3(0, 0, 0), transform), 1),
            Color = InstanceData[_quadCount].Color
        };

        dataSpan[_quadCount*4 + 1] = new PositionColorVertex
        {
            Position = new Vector4(Vector3.Transform(new Vector3(1, 0, 0), transform), 1),
            Color = InstanceData[_quadCount].Color
        };

        dataSpan[_quadCount*4 + 2] = new PositionColorVertex
        {
            Position = new Vector4(Vector3.Transform(new Vector3(0, 1, 0), transform), 1),
            Color = InstanceData[_quadCount].Color
        };

        dataSpan[_quadCount*4 + 3] = new PositionColorVertex
        {
            Position = new Vector4(Vector3.Transform(new Vector3(1, 1, 0), transform), 1),
            Color = InstanceData[_quadCount].Color
        };

        _quadVertexTransferBuffer.Unmap();

        _quadCount++;
    }
    public void End()
    {
        var commandBuffer = _graphicsDevice.AcquireCommandBuffer();
        var swapchainTexture = commandBuffer.AcquireSwapchainTexture(_window);

        if(swapchainTexture != null)
        {
            var copyPass = commandBuffer.BeginCopyPass();
            copyPass.UploadToBuffer(_quadVertexTransferBuffer, _quadVertexBuffer, true);
            commandBuffer.EndCopyPass(copyPass);

            var renderPass = commandBuffer.BeginRenderPass(
            new ColorTargetInfo(swapchainTexture, _clearColor));

            renderPass.BindGraphicsPipeline(_renderPipeline);
            renderPass.BindVertexBuffer(_quadVertexBuffer);
            renderPass.BindIndexBuffer(_quadIndexBuffer, IndexElementSize.ThirtyTwo);
            commandBuffer.PushVertexUniformData(_worldSpace);
            renderPass.DrawIndexedPrimitives(MAX_QUAD_COUNT * 6, 1, 0, 0, 0);

            commandBuffer.EndRenderPass(renderPass);
            _quadCount = 0;
        }

        _graphicsDevice.Submit(commandBuffer);
    }
}