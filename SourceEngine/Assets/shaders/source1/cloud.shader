HEADER
{
	Description = "Source 1 masked scrolling clouds";
}
FEATURES
{
	#include "common/features.hlsl"
	Feature( F_TRANSLUCENT, 0..1, "Blending" );
	Feature( F_SOURCE_PROXIES, 0..1, "Source 1" );
}
MODES
{
	Forward();
}
COMMON
{
	#define CUSTOM_MATERIAL_INPUTS
	StaticCombo( S_RENDER_BACKFACES, F_RENDER_BACKFACES, Sys( ALL ) );
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
	#include "source1/uv.hlsl"

	float2 g_vCloudScale < Default2( 1, 1 ); >;
	float2 g_vMaskScale < Default2( 1, 1 ); >;
	float3 g_vSourceBaseTransformU < Default3( 1, 0, 0 ); >;
	float3 g_vSourceBaseTransformV < Default3( 0, 1, 0 ); >;
	float3 g_vSourceBaseScroll < Default3( 0, 0, 1 ); >;

	PixelInput MainVs( VertexInput v )
	{
		PixelInput i = ProcessVertex( v );
		i.vTextureCoords.xy = SourceUv( i.vTextureCoords.xy, g_vSourceBaseTransformU, g_vSourceBaseTransformV, g_vSourceBaseScroll, g_vSourceBaseProxyU, g_vSourceBaseProxyV ) * g_vCloudScale;
		i.vTextureCoords.zw = v.vTexCoord2 * g_vMaskScale;
		return FinalizeVertex( i );
	}
}
PS
{
	StaticCombo( S_TRANSLUCENT, F_TRANSLUCENT, Sys( ALL ) );
	StaticCombo( S_ADDITIVE_BLEND, F_ADDITIVE_BLEND, Sys( ALL ) );
	#define BLEND_MODE_ALREADY_SET 1
	#include "common/pixel.hlsl"

	RenderState( DepthWriteEnable, false );
	RenderState( BlendEnable, true );
	#if S_ADDITIVE_BLEND
		RenderState( SrcBlend, ONE );
		RenderState( DstBlend, ONE );
	#else
		RenderState( SrcBlend, SRC_ALPHA );
		RenderState( DstBlend, INV_SRC_ALPHA );
	#endif
	BoolAttribute( DoNotCastShadows, true );
	CreateInputTexture2D( Cloud, Srgb, 8, "None", "_color", "Source 1", Default4( 1, 1, 1, 1 ) );
	CreateInputTexture2D( Mask, Srgb, 8, "None", "_mask", "Source 1", Default4( 1, 1, 1, 1 ) );
	Texture2D g_tColor < Channel( RGBA, Box( Cloud ), Srgb ); OutputFormat( DXT5 ); SrgbRead( true ); >;
	Texture2D g_tMask < Channel( RGBA, Box( Mask ), Srgb ); OutputFormat( DXT5 ); SrgbRead( true ); >;
	TextureAttribute( RepresentativeTexture, g_tColor );

	float4 MainPs( PixelInput i ) : SV_Target0
	{
		float4 cloud = g_tColor.Sample( g_sAniso, i.vTextureCoords.xy ) * g_tMask.Sample( g_sAniso, i.vTextureCoords.zw );
		float3 world = i.vPositionWithOffsetWs.xyz + g_vHighPrecisionLightingOffsetWs.xyz;
		cloud.a *= 2 * smoothstep( 0.3, 0.6, Fog::Transmittance( world, i.vPositionSs.xy ) );
		return cloud;
	}
}
