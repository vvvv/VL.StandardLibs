//@author: vvvv group
//@help: Draws an object in Projection Space in pixel units using the Constant Shader.
//@tags: hlsl, pixel, projection space
//@credits: 

Texture2D inputTexture <string uiname="Texture";>;

struct vsInput
{
    float4 posObject : POSITION;
};

struct psInput
{
    float4 posScreen : SV_Position;
};

struct vsInputTextured
{
    float4 posObject : POSITION;
	float4 uv: TEXCOORD0;
};

struct psInputTextured
{
    float4 posScreen : SV_Position;
    float4 uv: TEXCOORD0;
};


SamplerState linearSampler <string uiname="Sampler State";>
{
    Filter = MIN_MAG_MIP_LINEAR;
    AddressU = Clamp;
    AddressV = Clamp;
};

cbuffer cbPerObj : register( b1 )
{
	float4x4 tW : WORLD;
	float4x4 tA <string uiname="Transform in Projection Space (px)";>;
	float4 cAmb <bool color=true;String uiname="Color";> = { 1.0f,1.0f,1.0f,1.0f };
	float Alpha <float uimin=0.0; float uimax=1.0;> = 1; 
	float4x4 tColor <string uiname="Color Transform";>;
};

cbuffer cbTextureData : register(b2)
{
	float4x4 tTex <string uiname="Texture Transform"; bool uvspace=true; >;
};


cbuffer cbPerDraw : register(b0)
{
	float4x4 tP : PROJECTION;
	int VPCount: VIEWPORTCOUNT;
	int VPIndex: VIEWPORTINDEX;
	float2 targetSize: TARGETSIZE;
	float2 invTargetSize: INVTARGETSIZE;
	
	int ActiveVPIndex = -1;
};

psInput VS(vsInput input)
{
	psInput output;
	
	// Aspect Ratio
	float3 aspectRatio;
	float coeff = targetSize.y  / targetSize.x;
	
	if (coeff >= 1)
	{
		aspectRatio = float3 (coeff, 1, 1);
	}
	else
	{
		aspectRatio = float3 (1, 1/coeff, 1);
	}
				
	//World position
	float4 pos = mul(float4(0, 0, 0, 1), tW);
	
	//Corrected World Position
	float3 worldPosition = pos.xyz * aspectRatio;
	
	//Apply Additional Transform in View Space:
	float4 PosInProjection = mul(input.posObject, tA);
				
	//Calculate Pixel Size coeff from Renderer Size (in px)
	float3 pixelSizeCoeff = float3(2 * invTargetSize, 1);
	
	//Adjust the position according to the Pixel Size Coeff
	float3 vertexPos = PosInProjection.xyz * pixelSizeCoeff;
	
	//Final Vertex Position
	output.posScreen = float4(worldPosition + vertexPos, 1);
	
	return output;
}

psInputTextured VS_Textured(vsInputTextured input)
{
	psInputTextured output;
	
	// Aspect Ratio
	float3 aspectRatio;
	float coeff = targetSize.y  / targetSize.x;
	
	if (coeff >= 1)
	{
		aspectRatio = float3 (coeff, 1, 1);
	}
	else
	{
		aspectRatio = float3 (1, 1/coeff, 1);
	}
				
	//World position
	float4 pos = mul(float4(0, 0, 0, 1), tW);
	
	//Corrected World Position
	float3 worldPosition = pos.xyz * aspectRatio;
	
	//Apply Additional Transform in View Space:
	float4 PosInProjection = mul(input.posObject, tA);
				
	//Calculate Pixel Size coeff from Renderer Size (in px)
	float3 pixelSizeCoeff = float3(2 * invTargetSize, 1);
	
	//Adjust the position according to the Pixel Size Coeff
	float3 vertexPos = PosInProjection.xyz * pixelSizeCoeff;
	
	//Final Vertex Position
	output.posScreen = float4(worldPosition + vertexPos, 1);
	
	//Texture Coordinates
	output.uv = mul(input.uv, tTex);
	
	return output;
}


float4 PS(psInput input): SV_Target
{
    float4 col = cAmb;
	col = mul(col, tColor);
	col.a *= Alpha;
	
	if ((ActiveVPIndex >= 0) && (ActiveVPIndex != VPIndex))
		col.a = 0;
    return col;
}


float4 PS_Textured(psInputTextured input): SV_Target
{
    float4 col = inputTexture.Sample(linearSampler,input.uv.xy) * cAmb;
	col = mul(col, tColor);
	col.a *= Alpha;
	
	if ((ActiveVPIndex >= 0) && (ActiveVPIndex != VPIndex))
		col.a = 0;
    return col;
}

technique11 Constant <string noTexCdFallback="ConstantNoTexture"; >
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS_Textured() ) );
		SetPixelShader( CompileShader( ps_4_0, PS_Textured() ) );
	}
}

technique11 ConstantNoTexture
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}





