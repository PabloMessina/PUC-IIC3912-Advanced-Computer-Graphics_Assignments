﻿float4x4 projectionMatrix;
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
float3 specularColor;
float shininess;

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

float3 lambertBRDF(float3 normal, float3 lightDirection, float3 color) {
	return color * max(dot(normal, lightDirection), 0.0);
}

float3 blinnPhongBRDF(float3 normal, float3 halfVector, float3 color, float shininess)
{
	return color * pow(max(dot(normal, halfVector), 0.0), shininess);
}

float3 BRDF(float3 normal, float3 lightDirection, float3 viewDirection, float3 halfVector, float3 diffuse)
{
	return lambertBRDF(normal, lightDirection, diffuse) + blinnPhongBRDF(normal, halfVector, specularColor, shininess);
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
		float3 h = v + l;
		h = normalize(h);
		color += pointLightColors[pointLightIndex] * BRDF(n, l, v, h, diffuse);
	}
	for (int directionalLightIndex = 0; directionalLightIndex < activeNumberOfDirectionalLights; directionalLightIndex++)
	{
		float3 l = -directionalLightDirections[directionalLightIndex];
		l = normalize(l);
		float3 h = v + l;
		h = normalize(h);
		color += directionalLightColors[directionalLightIndex] * BRDF(n, l, v, h, diffuse);
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

//struct vertexAttributes {
//	float3 inPosition : POSITION;
//	float3 inNormal : NORMAL;
//	float3 inTextureCoords: TEXCOORD0;
//};
//
//struct fragmentAttributes {
//	float4 position : SV_POSITION;
//};
//
//
//fragmentAttributes VShader(vertexAttributes input)
//{
//	fragmentAttributes output = (fragmentAttributes)0;
//	output.position = float4(input.inPosition, 1);
//	return output;
//}
//
//
//float4 FShader(fragmentAttributes input) : SV_Target
//{
//	return float4(0, 1, 0, 1);
//}
//
//
//technique10 Render
//{
//
//	pass P0
//	{
//		SetGeometryShader(0);
//		SetVertexShader(CompileShader(vs_4_0, VShader()));
//		SetPixelShader(CompileShader(ps_4_0, FShader()));
//	}
//}
