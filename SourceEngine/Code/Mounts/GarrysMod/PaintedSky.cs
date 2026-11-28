namespace Source1.GarrysMod;

public sealed partial class Mount
{
	internal override Material CreateProceduralSky( VmtFile face, string name, string contentRoot )
	{
		if ( VmtFile.NormalizeShaderName( face.ShaderName ) != "g_sky" ) return null;
		var material = Material.Create( $"{name}.vmat", SkyPaint.Shader, false );
		var values = VmtFile.SelectParameters( face );
		var parameters = new VmtParameters( values, name );
		var settings = SkyPaint.Settings.MaterialDefaults;
		foreach ( var key in values.Keys )
			if ( key.StartsWith( '$' ) ) settings = settings.Read( key[1..], parameters, key );
		settings.Bind( material );
		material.Set( "StarPosition", parameters.Float( "$starpos", 1 ) );
		material.Attributes.Set( "SkyPaintProxy", face.Root.GetBlock( "proxies" )?.GetBlock( "SkyPaint" ) is not null );
		var stars = parameters.String( "$startexture", "skybox/stars" );
		if ( settings.StarLayers > 0 ) material.Set( "g_tStars", LoadStars( stars, contentRoot ) );
		return material;
	}

	internal Texture LoadStars( string name, string contentRoot )
	{
		var path = string.IsNullOrWhiteSpace( name ) ? null : GetContentPath( VmtFile.NormalizePath( name, ".vtf" ), contentRoot );
		return path is null ? Texture.Black : Texture.Load( $"{path}.vtex", false );
	}

	internal override Component CreateMapPoint( MapEntity entity, GameObject world, BspFile bsp, string contentRoot )
	{
		if ( entity.Get( "classname" ) != "env_skypaint" ) return null;
		var go = new GameObject( world, false, entity.Get( "targetname", "env_skypaint" ) ) { NetworkMode = NetworkMode.Object };
		go.LocalPosition = entity.Vector( "origin" );
		var paint = go.AddComponent<SkyPaint>();
		var settings = SkyPaint.Settings.EntityDefaults;
		var parameters = new VmtParameters( entity.Values, go.Name );
		foreach ( var (key, _) in entity.KeyValues )
			settings = settings.Read( key.ToLowerInvariant(), parameters, key );
		if ( bsp.Entities.FirstOrDefault( e => e.Get( "classname" ) == "env_sun" ) is { } sun )
			settings = settings with { SunNormal = SunDirection( sun, bsp ) };
		paint.Paint = settings;
		paint.StarTexture = entity.Get( "startexture", "skybox/starfield" );
		paint.ContentRoot = contentRoot;
		go.Enabled = true;
		return paint;
	}

	static Vector3 SunDirection( MapEntity sun, BspFile bsp )
	{
		if ( sun.Integer( "use_angles" ) == 0 )
		{
			var name = sun.Get( "target" );
			var target = name is null ? null : bsp.Entities.FirstOrDefault( e => e.Get( "targetname" ) == name );
			return target is null ? Vector3.Up : (sun.Vector( "origin" ) - target.Vector( "origin" )).Normal;
		}
		var angles = sun.Angles;
		var pitch = sun.Number( "pitch" );
		var yaw = sun.Number( "angle" );
		var forward = new Angles( -(pitch == 0 ? angles.pitch : pitch), yaw == 0 ? angles.yaw : yaw, 0 ).Forward;
		if ( yaw is -1 or -2 ) forward = new Vector3( 0, 0, forward.z );
		return -forward;
	}
}
