struct vs_out
{
	float4 position : SV_POSITION;
	float4 color : COLOR;
};

float4x4 worldViewProj;
float4 instanceColor;

vs_out main(float4 position : POSITION, float4 color : COLOR)
{
	vs_out res;
	res.position = mul(position, worldViewProj);
	res.color = color * instanceColor;
	return res;
}