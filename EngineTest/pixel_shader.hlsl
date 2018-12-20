
struct vs_out
{
	float4 position : SV_POSITION;
	float4 color : COLOR;
	float2 uv : TEXCOORD0;
};

Texture2D tex;
SamplerState texSampler;

float4 main(vs_out input) : SV_TARGET
{
	float4 texCol = tex.Sample(texSampler, input.uv);
	return input.color * texCol;
}
