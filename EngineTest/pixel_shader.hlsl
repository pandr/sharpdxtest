
struct vs_out
{
	float4 position : SV_POSITION;
	float4 color : COLOR;
	float2 uv : TEXCOORD0;
};


float4 main(vs_out input) : SV_TARGET
{
	float center = 1.0 - length(input.uv*2.0 - 1.0);
	return input.color * center;
}
