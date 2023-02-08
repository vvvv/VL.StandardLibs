//@author: vvvv group
//@help: Aligns the orientation of a geometry to the camera.
//@tags: billboard, view space
//@credits:

// --------------------------------------------------------------------------------------------------
// PARAMETERS:
// --------------------------------------------------------------------------------------------------

//transforms
float4x4 tW: WORLD;        //the models world matrix
float4x4 tV: VIEW;         //view matrix
float4x4 tWV: WORLDVIEW;
float4x4 tWVP: WORLDVIEWPROJECTION;
float4x4 tP: PROJECTION;   //projection matrix
float4x4 itP: PROJECTIONINVERSE; //inverted projection matrix

float4x4 tA <string uiname="Transform in Viewspace";>;

//material properties
float4 cAmb <bool color=true; String uiname="Color";> = { 1.0f,1.0f,1.0f,1.0f };
float Alpha <float uimin=0.0; float uimax=1.0;> = 1;

//texture
Texture2D Tex <string uiname="Texture";>;

//fixed size
bool fixedSize <string uiname = "Fixed Size"; > = false;
float2 Size = float2 (0.2, 0.2);

SamplerState g_samLinear <string uiname="Sampler State";>
{
	Filter = MIN_MAG_MIP_LINEAR;
	AddressU = Wrap;
	AddressV = Wrap;
};

float4x4 tTex <string uiname="Texture Transform"; bool uvspace=true;>;
float4x4 tColor <string uiname="Color Transform";>;


struct vs2ps
{
    float4 PosWVP: SV_POSITION;
    float4 TexCd : TEXCOORD0;
    float4 NormV: TEXCOORD1;
};

// --------------------------------------------------------------------------------------------------
// VERTEXSHADERS
// --------------------------------------------------------------------------------------------------
vs2ps VS(
    float4 PosO: POSITION,
    float4 NormO: NORMAL,
    float4 TexCd : TEXCOORD0)
{
    //inititalize all fields of output struct with 0
    vs2ps Out = (vs2ps)0;
    
    //normal in view space
    Out.NormV = normalize(mul(NormO, tA));

    //WorldView position
    float4 pos = mul(float4(0, 0, 0, 1), tWV);
	
    //position (projected)
	if (fixedSize)
	{   
		// Apply Projection to the world's view position
		pos = mul (pos, tP);
		
		// Make a perspective division
		pos.xyz /= pos.w;
				
		float aX = tP[0][0];
		float aY = tP[1][1];
		float3 aspectRatio = float3 (aX, aY, 1);

		// Add the Object's position multiplied by the viewspace transform
		// to the WorldViewProjected position multiplied by the Aspect Ratio
		Out.PosWVP = float4(pos.xyz + mul(PosO * float4(Size,1,1), tA).xyz * aspectRatio, 1);
		
	}
	else
	{
		// Add the Object's position multiplied by the viewspace transform
		// to the WorldView position and then apply Projection	
		Out.PosWVP  = mul(pos + mul(PosO, tA), tP);
	}

    Out.TexCd = mul(TexCd, tTex);
    return Out;
}

// --------------------------------------------------------------------------------------------------
// PIXELSHADERS:
// --------------------------------------------------------------------------------------------------

float4 PS(vs2ps In): SV_Target
{

   	float4 col = Tex.SampleLevel( g_samLinear, In.TexCd.xy,1) * cAmb;
   	col = mul(col, tColor);
	col.a *= Alpha;
    return col;
}


// --------------------------------------------------------------------------------------------------
// TECHNIQUES:
// --------------------------------------------------------------------------------------------------

technique10 SelfAlign
{
		pass P0
		{
			SetVertexShader( CompileShader( vs_4_0, VS() ) );
			SetPixelShader( CompileShader( ps_4_0, PS() ) );
		}
}
