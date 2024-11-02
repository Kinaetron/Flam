using MoonWorks;
using MoonWorks.Graphics;

namespace Flam.src.Graphics;

public class SpriteBatch
{
    private Window _window;
    private GraphicsDevice _graphicsDevice;

    public SpriteBatch(Window window, GraphicsDevice graphicsDevice)
    {
        _window = window;
        _graphicsDevice = graphicsDevice;

        var vertexShader = ShaderCross.Create(
            _graphicsDevice,
            $"{SDL3.SDL.SDL_GetBasePath()}shaders/TexturedQuadColorWithMatrix.vert.hlsl",
            "main",
            new ShaderCross.ShaderCreateInfo
            {
                Format = ShaderCross.ShaderFormat.HLSL,
                Stage = ShaderStage.Vertex,
                NumUniformBuffers = 1
            });

        var fragmentShader = ShaderCross.Create(
            _graphicsDevice,
             $"{SDL3.SDL.SDL_GetBasePath()}shaders/TexturedQuadColor.frag.hlsl",
             "main",
             new ShaderCross.ShaderCreateInfo
             {
                 Format = ShaderCross.ShaderFormat.HLSL,
                 Stage = ShaderStage.Fragment,
                 NumSamplers = 1
             });

        var graphicsPipelineInfo = new GraphicsPipelineCreateInfo
        {
            TargetInfo = new GraphicsPipelineTargetInfo
            {
                ColorTargetDescriptions = [
                    new ColorTargetDescription{
                        Format = window.SwapchainFormat,
                        BlendState = ColorTargetBlendState.Opaque
                    }]
            },
            DepthStencilState = DepthStencilState.Disable,
            MultisampleState = MultisampleState.None,
            PrimitiveType = PrimitiveType.TriangleList,
            RasterizerState = RasterizerState.CCW_CullNone,
            VertexInputState = VertexInputState.Empty,
            VertexShader = vertexShader,
            FragmentShader = fragmentShader
        };
    }
}
