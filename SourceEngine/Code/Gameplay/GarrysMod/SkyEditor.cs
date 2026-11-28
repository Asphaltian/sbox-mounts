using System;

namespace Source1.GarrysMod;

[Title( "Sky Editor" )]
public sealed class SkyEditor : SkyPaint
{
	[Property, Hide] public bool Initialized { get; set; }

	protected override void OnStart()
	{
		if ( Networking.IsHost && !Initialized )
		{
			var map = Scene.GetAllComponents<SkyPaint>().FirstOrDefault( p => p is not SkyEditor );
			if ( map.IsValid() )
			{
				Paint = map.Paint;
				StarTexture = map.StarTexture;
				ContentRoot = map.ContentRoot;
			}
			Initialized = true;
		}
		base.OnStart();
	}

	static bool CanEdit => Networking.IsHost && (Rpc.Caller is null || Rpc.Caller.HasPermission( "admin" ));

	void Edit( Settings value )
	{
		if ( CanEdit ) Paint = value;
	}

	[Property, ClientEditable, Group( "Sky" )]
	public Color TopColor
	{
		get => Paint.TopColor;
		set => Edit( Paint with { TopColor = new Vector3( value.r, value.g, value.b ) } );
	}

	[Property, ClientEditable, Group( "Sky" )]
	public Color BottomColor
	{
		get => Paint.BottomColor;
		set => Edit( Paint with { BottomColor = new Vector3( value.r, value.g, value.b ) } );
	}

	[Property, ClientEditable, Group( "Sky" ), Range( 0, 1 )]
	public float FadeBias
	{
		get => Paint.FadeBias;
		set => Edit( Paint with { FadeBias = Math.Clamp( value, 0, 1 ) } );
	}

	[Property, ClientEditable, Group( "Sky" ), Range( 0, 1 )]
	public float HDRScale
	{
		get => Paint.HDRScale;
		set => Edit( Paint with { HDRScale = Math.Clamp( value, 0, 1 ) } );
	}

	[Property, ClientEditable, Group( "Sun" )]
	public Color SunColor
	{
		get => Paint.SunColor;
		set => Edit( Paint with { SunColor = new Vector3( value.r, value.g, value.b ) } );
	}

	[Property, ClientEditable, Group( "Sun" ), Range( 0, 10 )]
	public float SunSize
	{
		get => Paint.SunSize;
		set => Edit( Paint with { SunSize = Math.Clamp( value, 0, 10 ) } );
	}

	[Property, ClientEditable, Group( "Dusk" )]
	public Color DuskColor
	{
		get => Paint.DuskColor;
		set => Edit( Paint with { DuskColor = new Vector3( value.r, value.g, value.b ) } );
	}

	[Property, ClientEditable, Group( "Dusk" ), Range( 0, 1 )]
	public float DuskScale
	{
		get => Paint.DuskScale;
		set => Edit( Paint with { DuskScale = Math.Clamp( value, 0, 1 ) } );
	}

	[Property, ClientEditable, Group( "Dusk" ), Range( 0, 10 )]
	public float DuskIntensity
	{
		get => Paint.DuskIntensity;
		set => Edit( Paint with { DuskIntensity = Math.Clamp( value, 0, 10 ) } );
	}

	[Property, ClientEditable, Group( "Stars" )]
	public bool DrawStars
	{
		get => Paint.DrawStars;
		set => Edit( Paint with { DrawStars = value } );
	}

	[Property, ClientEditable, Group( "Stars" ), Range( 1, 3 )]
	public int StarLayers
	{
		get => Paint.StarLayers;
		set => Edit( Paint with { StarLayers = Math.Clamp( value, 1, 3 ) } );
	}

	[Property, ClientEditable, Group( "Stars" ), Range( 0, 5 )]
	public float StarScale
	{
		get => Paint.StarScale;
		set => Edit( Paint with { StarScale = Math.Clamp( value, 0, 5 ) } );
	}

	[Property, ClientEditable, Group( "Stars" ), Range( 0, 5 )]
	public float StarFade
	{
		get => Paint.StarFade;
		set => Edit( Paint with { StarFade = Math.Clamp( value, 0, 5 ) } );
	}

	[Property, ClientEditable, Group( "Stars" ), Range( 0, 2 )]
	public float StarSpeed
	{
		get => Paint.StarSpeed;
		set => Edit( Paint with { StarSpeed = Math.Clamp( value, 0, 2 ) } );
	}

	[Property, ClientEditable, Group( "Stars" ), Title( "Star Texture" )]
	public string TextureName
	{
		get => StarTexture;
		set { if ( CanEdit ) StarTexture = value; }
	}
}
