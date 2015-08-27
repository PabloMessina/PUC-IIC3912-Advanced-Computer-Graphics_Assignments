float4x4 projectionMatrix;
float4x4 viewMatrix;
float4x4 modelMatrix;

static const int maxNumberOfLights = 10;
float activeNumberOfPointLights;
float activeNumberOfDirectionalLights;
float3 pointLightPositions[maxNumberOfLights];
float3 pointLightColors[maxNumberOfLights];
float3 directionalLightDirections[maxNumberOfLights];
float3 directionalLightColors[maxNumberOfLights];

float3 cameraPosition;

float3 ambientLight;
float3 diffuseColor;

float _n;
float _R;
static const float PI = 3.14159265358979323846;


struct vertexAttributes {
  float3 inPosition : POSITION;
  float3 inNormal : NORMAL;
  float3 inTextureCoords: TEXCOORD0;
};

struct fragmentAttributes {
  float4 position : SV_POSITION;
  float3 fragPosition : POSITION;
  float3 fragNormal : NORMAL;
  float3 fragTextureCoords : TEXCOORD0;
};

float lump(float3 h, float R, float n)
{
	return (n + 1) / (PI*R*R) * pow(1 - dot(h, h) / (R*R), n);
}

float3 BRDF(float3 L, float3 V, float3 N)
{
	float NdotV = dot(N, V);
	float NdotL = dot(N, L);

	if (NdotL < 0 || NdotV < 0) return float3(0,0,0);

	float3 H = normalize(L + V);
	float NdotH = dot(N, H);
	float LdotH = dot(L, H);

	// scaling projection
	float3 uH = L + V; // unnormalized H
	float3 h = NdotV / dot(N, uH) * uH;
	float3 huv = h - NdotV * N;

	// specular term (D and G)
	float p = lump(huv, _R, _n);
	float aux = p * pow(NdotV, 2) / (4 * NdotL * LdotH * pow(NdotH, 3));
	return float3(aux,aux,aux);
}

float3 shade(float3 p, float3 n, float3 diffuse)
{
	n = normalize(n);
	float3 v = cameraPosition - p;
	v = normalize(v);

	float3 color = ambientLight * diffuse;
	for (int pointLightIndex = 0; pointLightIndex < activeNumberOfPointLights; pointLightIndex++)
	{
		float3 l = pointLightPositions[pointLightIndex] - p;
		l = normalize(l);
		color += pointLightColors[pointLightIndex] * BRDF(l, v, n);
	}
	for (int directionalLightIndex = 0; directionalLightIndex < activeNumberOfDirectionalLights; directionalLightIndex++)
	{
		float3 l = -directionalLightDirections[directionalLightIndex];
		l = normalize(l);
		color += directionalLightColors[directionalLightIndex] * BRDF(l, v, n);
	}
	return color;
}

fragmentAttributes VShader(vertexAttributes input)
{
	float4 worldPosition = mul(modelMatrix, float4(input.inPosition, 1.0));
	float4 worldNormal = mul(modelMatrix, float4(input.inNormal, 0.0));
	fragmentAttributes output = (fragmentAttributes)0;
	output.fragPosition = worldPosition.xyz;
	output.fragNormal = worldNormal.xyz;
	output.fragTextureCoords = input.inTextureCoords;
	//DEBUGGING HERE
	output.position = mul(mul(worldPosition, viewMatrix), projectionMatrix);
	return output;
}

float4 FShader(fragmentAttributes input) : SV_Target
{
	return float4(shade(input.fragPosition,input.fragNormal,diffuseColor), 1.0);
}

technique10 Render
{

  pass P0
  {
    SetGeometryShader(0);
    SetVertexShader(CompileShader(vs_4_0, VShader()));
    SetPixelShader(CompileShader(ps_4_0, FShader()));
  }
}