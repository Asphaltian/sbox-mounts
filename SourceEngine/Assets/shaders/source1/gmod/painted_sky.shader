HEADER
{
	Description = "Garry's Mod painted sky";
}
MODES
{
	Forward();
}
COMMON
{
	#include "system.fxc"
	#include "vr_common.fxc"
}
struct VS_INPUT
{
	float3 position : POSITION < Semantic( PosXyz ); >;
};
struct PixelInput
{
	float4 position : SV_Position;
	float3 direction : TEXCOORD0;
};
VS
{
	PixelInput MainVs( VS_INPUT v )
	{
		PixelInput i;
		i.direction = v.position;
		i.position = Position3WsToPs( g_vCameraPositionWs + v.position * (g_flNearPlane + g_flFarPlane) );
		return i;
	}
}
PS
{
	#include "common/classes/Fog.hlsl"

	BoolAttribute( sky, true );
	RenderState( CullMode, NONE );
	RenderState( DepthWriteEnable, false );
	RenderState( DepthEnable, true );
	RenderState( DepthFunc, GREATER_EQUAL );

	float3 TopColor < Default3( 0, 0, 1 ); >;
	float3 BottomColor < Default3( 0, 1, 1 ); >;
	float FadeBias < Default( 1 ); >;
	float HDRScale < Default( 1 ); >;
	float3 SunNormal < Default3( 0, 1, 0.5 ); >;
	float3 SunColor < Default3( 1, 1, 1 ); >;
	float SunSize < Default( 1 ); >;
	float3 DuskColor < Default3( 1, 0.4, 0 ); >;
	float DuskScale < Default( 1 ); >;
	float DuskIntensity < Default( 1 ); >;
	float StarLayers < Default( 0 ); >;
	float StarFade < Default( 1 ); >;
	float StarScale < Default( 1 ); >;
	float StarSpeed < Default( 0 ); >;
	float StarPosition < Default( 1 ); >;
	float3 Tint < Attribute( "SkyTint" ); Default3( 1, 1, 1 ); >;
	CreateInputTexture2D( StarTexture, Linear, 8, "None", "_stars", "Sky", Default4( 0, 0, 0, 1 ) );
	Texture2D g_tStars < Channel( RGB, Box( StarTexture ), Linear ); OutputFormat( RGBA8888 ); SrgbRead( false ); >;
	SamplerState StarSampler < Filter( ANISO ); AddressU( WRAP ); AddressV( WRAP ); >;

	float4 MainPs( PixelInput i ) : SV_Target0
	{
		float3 dir = normalize( i.direction );
		float height = abs( dir.z );
		float horizon = 1 - height;
		float sun = clamp( dot( SunNormal, dir ), -1, 1 );
		float3 color = lerp( BottomColor, TopColor, pow( height, FadeBias ) );
		float dusk = (sun + horizon * 8) / 9;
		if ( dusk > 0 )
			color += (DuskColor - color * 0.5) * (pow( dusk, 10 - 5 * DuskScale ) * DuskIntensity);
		if ( sun > 0 && SunSize > 0 )
			color += SunColor * pow( sun, 20 / SunSize );
		color *= HDRScale;

		if ( StarLayers > 0 && dir.z > 0 )
		{
			float offset = StarPosition + StarSpeed * g_flTime;
			float2 uv = asin( clamp( dir.xy, -1, 1 ) ) + sin( dir.xy ) * horizon;
			uv.x += offset;
			float3 stars = g_tStars.Sample( StarSampler, uv * StarScale ).rgb;
			if ( StarLayers >= 2 )
			{
				uv.x -= offset * 0.01 + 1;
				stars += g_tStars.Sample( StarSampler, uv * StarScale ).rgb * 0.5;
			}
			if ( StarLayers >= 3 )
			{
				uv.x -= offset * 0.02 + 1;
				stars += g_tStars.Sample( StarSampler, uv * StarScale ).rgb * 0.75;
			}
			color += stars * pow( height, rcp( max( StarFade, 0.000001 ) ) );
		}

		float3 world = g_vCameraPositionWs + i.direction * (g_flNearPlane + g_flFarPlane);
		return float4( Fog::ApplySky( world, i.position.xy, max( color * Tint, 0 ) ), 1 );
	}
}
