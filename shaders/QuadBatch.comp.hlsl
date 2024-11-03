struct QuadComputeData
{
    float3 position;
    float rotation;
    float2 scale;
    float4 color;
};

struct QuadVertex
{
    float4 position;
    float4 color;
};

StructuredBuffer<QuadComputeData> ComputeBuffer : register(t0, space0);
RWStructuredBuffer<QuadVertex> VertexBuffer : register(u0, space1);

[numthreads(64, 1, 1)]
void main(uint3 GlobalInvocationID : SV_DispatchThreadID)
{
    uint n = GlobalInvocationID.x;

    QuadComputeData currentQuadData = ComputeBuffer[n];

    float4x4 Scale = float4x4(
        float4(currentQuadData.scale.x, 0.0f, 0.0f, 0.0f),
        float4(0.0f, currentQuadData.scale.y, 0.0f, 0.0f),
        float4(0.0f, 0.0f, 1.0f, 0.0f),
        float4(0.0f, 0.0f, 0.0f, 1.0f)
    );

    float c = cos(currentQuadData.rotation);
    float s = sin(currentQuadData.rotation);

    float4x4 Rotation = float4x4(
        float4(   c,    s, 0.0f, 0.0f),
        float4(  -s,    c, 0.0f, 0.0f),
        float4(0.0f, 0.0f, 1.0f, 0.0f),
        float4(0.0f, 0.0f, 0.0f, 1.0f)
    );

    float4x4 Translation = float4x4(
        float4(1.0f, 0.0f, 0.0f, 0.0f),
        float4(0.0f, 1.0f, 0.0f, 0.0f),
        float4(0.0f, 0.0f, 1.0f, 0.0f),
        float4(currentQuadData.position.x, currentQuadData.position.y, currentQuadData.position.z, 1.0f)
    );

    float4x4 Model = mul(Scale, mul(Rotation, Translation));

    float4 topLeft = float4(0.0f, 0.0f, 0.0f, 1.0f);
    float4 topRight = float4(1.0f, 0.0f, 0.0f, 1.0f);
    float4 bottomLeft = float4(0.0f, 1.0f, 0.0f, 1.0f);
    float4 bottomRight = float4(1.0f, 1.0f, 0.0f, 1.0f);

    VertexBuffer[n * 4u]    .position = mul(topLeft, Model);
    VertexBuffer[n * 4u + 1].position = mul(topRight, Model);
    VertexBuffer[n * 4u + 2].position = mul(bottomLeft, Model);
    VertexBuffer[n * 4u + 3].position = mul(bottomRight, Model);

    VertexBuffer[n * 4u]    .color = currentQuadData.color;
    VertexBuffer[n * 4u + 1].color = currentQuadData.color;
    VertexBuffer[n * 4u + 2].color = currentQuadData.color;
    VertexBuffer[n * 4u + 3].color = currentQuadData.color;
}
