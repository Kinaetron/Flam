cbuffer UniformBlock : register(b0, space1)
{
    float4x4 MatrixTransform : packoffset(c0);
};

struct Input
{
    float4 Position : TEXCOORD0;
    float4 Color : TEXCOORD1;
};

struct Output
{
    float4 Position : SV_Position;
    float4 Color : TEXCOORD0;
};

Output main(Input input)
{
    Output output;
    output.Position = mul(MatrixTransform, input.Position);
    output.Color = input.Color;
    return output;
}