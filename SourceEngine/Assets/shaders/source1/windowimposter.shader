HEADER
{
	Description = "Source 1 window imposter cubemap";
}
FEATURES
{
	#include "common/features.hlsl"
	Feature( F_TRANSLUCENT, 0..1, "Blending" );
}
MODES
{
	Forward();
}
COMMON
{
	#define CUSTOM_MATERIAL_INPUTS
	#include "common/shared.hlsl"
}
struct VertexInput
{
	#include "common/vertexinput.hlsl"
};
struct PixelInput
{
	#include "common/pixelinput.hlsl"
};
VS
{
	#include "common/vertex.hlsl"
	PixelInput MainVs( VertexInput v )
	{
		return FinalizeVertex( ProcessVertex( v ) );
	}
}
PS
{
	StaticCombo( S_TRANSLUCENT, F_TRANSLUCENT, Sys( ALL ) );
	#include "common/pixel.hlsl"
	CreateInputTextureCube( Environment, Srgb, 8, "None", "_cube", "Source 1", Default4( 0, 0, 0, 1 ) );
	TextureCube g_tEnvCube < Channel( RGBA, Box( Environment ), Srgb ); OutputFormat( RGBA8888 ); SrgbRead( true ); >;
	BoolAttribute( DoNotCastShadows, true );

	float4 MainPs( PixelInput i ) : SV_Target0
	{
		float3 world = i.vPositionWithOffsetWs + g_vHighPrecisionLightingOffsetWs.xyz;
		float3 color = g_tEnvCube.Sample( g_sAniso, world - g_vCameraPositionWs ).rgb * i.vVertexColor.rgb;
		if ( g_bFogEnabled ) color = Fog::Apply( world, i.vPositionSs.xy, color );
		return float4( color, i.vVertexColor.a );
	}
}
