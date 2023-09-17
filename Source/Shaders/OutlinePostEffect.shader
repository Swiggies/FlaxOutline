#include "./Flax/Common.hlsl"

META_CB_BEGIN(0, Data)
float4 Color;
float2 TexelSize;
float Size;
float ColorSensitivity;
float NormalSensitivity;
float DepthSensitivity;
float DepthAccuracy;
META_CB_END

Texture2D SceneColor : register(t0);
Texture2D SceneDepth : register(t1);
Texture2D SceneNormals : register(t2);

void Outline(float2 uv, out float4 outline)
{
    float halfScaleFloor = floor(Size);
    float halfScaleCeil = ceil(Size);
    
    float depthSamples[4];
    float2 uvSamples[4];
    float3 colorSamples[4], normalSamples[4];
    
    uvSamples[0] = uv - float2(TexelSize) * halfScaleFloor;
    uvSamples[1] = uv + float2(TexelSize) * halfScaleCeil;
    uvSamples[2] = uv + float2(TexelSize.x * halfScaleCeil, -TexelSize.y * halfScaleFloor);
    uvSamples[3] = uv + float2(-TexelSize.x * halfScaleFloor, TexelSize.y * halfScaleCeil);
    
    for (int i = 0; i < 4; i++)
    {
        colorSamples[i] = SceneColor.SampleLevel(SamplerLinearClamp, uvSamples[i], 0).rgb;
        normalSamples[i] = SceneNormals.SampleLevel(SamplerLinearClamp, uvSamples[i], 0).rgb;
        depthSamples[i] = pow(SceneDepth.SampleLevel(SamplerLinearClamp, uvSamples[i], 0).r, DepthSensitivity);
    }
    
    // Depth
    float3 edgeFiniteDifference0 = depthSamples[1] - depthSamples[0];
    float3 edgeFiniteDifference1 = depthSamples[3] - depthSamples[2];
    float edgeDepth = sqrt(dot(edgeFiniteDifference0, edgeFiniteDifference0) + dot(edgeFiniteDifference1, edgeFiniteDifference1));
    edgeDepth = edgeDepth > (1 / DepthSensitivity) ? 1 : 0;
    
    // Normals
    float3 normalFiniteDifference0 = normalSamples[1] - normalSamples[0];
    float3 normalFiniteDifference1 = normalSamples[3] - normalSamples[2];
    float4 edgeNormal = sqrt(dot(normalFiniteDifference0, normalFiniteDifference0) + dot(normalFiniteDifference1, normalFiniteDifference1));
    edgeNormal = edgeNormal > (1 / NormalSensitivity) ? 1 : 0;
    
    // Color
    float col0 = colorSamples[1] - colorSamples[0];
    float col1 = colorSamples[3] - colorSamples[2];
    float4 edgeColor = sqrt(dot(col0, col1) + dot(col0, col1));
    edgeColor = edgeColor > (1 / ColorSensitivity) ? 1 : 0;
    
    float edge = max(edgeDepth, max(edgeNormal, edgeColor));
    float4 scene = SceneColor.SampleLevel(SamplerPointClamp, uv, 0);
    float4 color = edge * Color;
    outline = lerp(scene, color, edge);
}

META_PS(true, FEATURE_LEVEL_ES2)
float4 PS_Fullscreen(Quad_VS2PS input) : SV_Target
{
	// Solid color fill from the constant buffer passed from code
	float4 outline;
	Outline(input.TexCoord, outline);
	return outline;
}
