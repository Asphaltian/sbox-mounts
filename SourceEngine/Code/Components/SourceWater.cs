using System;
using Sandbox.Rendering;

namespace Source1;

[Title( "Source Water" ), Category( "Source Engine" )]
public sealed class SourceWater : Component, Component.ExecuteInEditor
{
	[Property] public ModelRenderer Renderer { get; set; }
	[Property] public Material Material { get; set; }
	[Property] public Fog Atmosphere { get; set; }
	[Property] public float Height { get; set; }
	[Property] public bool AboveWater { get; set; } = true;
	[Property] public bool Reflect { get; set; } = true;
	[Property] public bool Refract { get; set; } = true;
	[Property] public bool ReflectEntities { get; set; } = true;
	[Property] public bool ReflectSky { get; set; } = true;
	[Property] public bool ReflectSkybox3D { get; set; }

	readonly List<CameraComponent> _cameras = [];
	CommandList[] _captures;
	int _captureIndex;
	TagSet _reflectionTags;
	TagSet _reflectionExclusions;
	readonly TagSet _refractionExclusions = new( ["source1_water"] );
	int _filterKey = -1;

	protected override void OnEnabled()
	{
		if ( Application.IsDedicatedServer ) return;
		_captures = [new( "Source water captures" ), new( "Source water captures" )];
	}

	protected override void OnPreRender()
	{
		if ( _captures is null || !Renderer.IsValid() || Material is not { IsValid: true } )
			return;

		CameraComponent camera = null;
		if ( Scene.IsEditor )
		{
			_cameras.Clear();
			Scene.GetAll( _cameras );
			camera = _cameras.FirstOrDefault( x => (x.GameObject.Flags & GameObjectFlags.EditorOnly) != 0 );
			_cameras.Clear();
		}
		camera ??= Scene.Camera;
		if ( !camera.IsValid() )
		{
			Renderer.ExecuteBefore = null;
			return;
		}

		var commands = _captures[_captureIndex ^= 1];
		commands.Reset();
		commands.Attributes.Set( "SourceWaterHasReflection", false );
		commands.Attributes.Set( "SourceWaterHasRefraction", false );
		var origin = WorldTransform.PointToWorld( new Vector3( 0, 0, Height ) );
		var normal = WorldRotation.Up * (WorldScale.z < 0 ? -1 : 1);
		commands.Attributes.Set( "SourceWaterPlane", new Vector4( normal, Vector3.Dot( origin, normal ) ) );
		var atmosphere = GetAtmosphere();
		int filterKey = (ReflectEntities ? 1 : 0) | (ReflectSky ? 2 : 0) | (ReflectSkybox3D ? 4 : 0);
		if ( filterKey != _filterKey )
		{
			_filterKey = filterKey;
			_reflectionTags = new TagSet();
			if ( !ReflectEntities )
			{
				_reflectionTags.Add( "world" );
				if ( ReflectSky ) _reflectionTags.Add( "skybox" );
				if ( ReflectSkybox3D ) _reflectionTags.Add( "source1_skybox3d" );
			}
			_reflectionExclusions = new TagSet( ["source1_water"] );
			if ( !ReflectEntities ) _reflectionExclusions.Add( "source1_entity" );
			if ( !ReflectSky ) _reflectionExclusions.Add( "source1_skybox2d" );
			if ( !ReflectSkybox3D ) _reflectionExclusions.Add( "source1_skybox3d" );
		}

		if ( Reflect && AboveWater )
		{
			var target = commands.GetRenderTarget( "Source water reflection", 1, ImageFormat.RGBA16161616F, ImageFormat.None );
			var view = commands.DrawReflection( camera, new Plane( origin, normal ), target, new ReflectionSetup
			{
				ViewSetup = new ViewSetup
				{
					EnablePostprocessing = false,
					EnableUI = false,
					EnableDebugOverlays = false,
					RenderTags = _reflectionTags,
					ExcludeTags = _reflectionExclusions,
					Fog = atmosphere,
					ClearColor = Color.Black,
					ViewHash = HashCode.Combine( Id, 0 )
				}
			} );
			commands.Attributes.Set( "SourceWaterReflection", target.ColorTexture );
			commands.Attributes.Set( "SourceWaterReflectionViewport", view.Viewport );
			commands.Attributes.Set( "SourceWaterHasReflection", view.IsValid );
		}

		if ( Refract )
		{
			var target = commands.GetRenderTarget( "Source water refraction", 1, ImageFormat.RGBA16161616F, ImageFormat.Default );
			var fogColor = Material.GetColor( "g_vSourceFogColor" ).WithAlpha( 1 );
			var view = commands.DrawRefraction( camera, new Plane( origin, AboveWater ? -normal : normal ), target, new RefractionSetup
			{
				RenderBehind = true,
				ViewSetup = new ViewSetup
				{
					EnablePostprocessing = false,
					EnableUI = false,
					EnableDebugOverlays = false,
					ExcludeTags = _refractionExclusions,
					Fog = AboveWater ? new FogSetup { Enabled = false } : atmosphere,
					ClearColor = AboveWater ? fogColor : Color.Black,
					ViewHash = HashCode.Combine( Id, 1 )
				}
			} );
			commands.Attributes.Set( "SourceWaterRefraction", target.ColorTexture );
			commands.Attributes.Set( "SourceWaterRefractionDepth", target.DepthTexture );
			commands.Attributes.Set( "SourceWaterRefractionProjectionToView", view.ProjectionToView );
			commands.Attributes.Set( "SourceWaterRefractionDepthUvScaleBias", view.DepthUvScaleBias );
			commands.Attributes.Set( "SourceWaterRefractionViewport", view.Viewport );
			commands.Attributes.Set( "SourceWaterRefractionDepthRange", view.DepthRange );
			commands.Attributes.Set( "SourceWaterHasRefraction", view.IsValid );
		}

		Renderer.ExecuteBefore = commands;
	}

	FogSetup GetAtmosphere()
	{
		if ( !Atmosphere.IsValid() )
			return new FogSetup { Enabled = false };

		return new FogSetup
		{
			Enabled = Atmosphere.FogEnabled,
			DistanceEnabled = Atmosphere.DistanceEnabled,
			Mode = FogMode.Distance,
			DistanceMetric = Atmosphere.DistanceMetric,
			Color = Atmosphere.Color.WithAlpha( 1 ),
			DirectionalColorEnabled = Atmosphere.DirectionalColorEnabled,
			SecondaryColor = Atmosphere.SecondaryColor.WithAlpha( 1 ),
			ColorDirection = Atmosphere.WorldRotation * Atmosphere.ColorDirection,
			ColorScale = Atmosphere.ColorScale,
			StartDistance = Atmosphere.DistanceRange.x,
			EndDistance = Atmosphere.DistanceRange.y,
			FalloffExponent = Atmosphere.FalloffExponent,
			MaximumOpacity = Atmosphere.MaximumOpacity,
			SunLightingEnabled = false,
			SkyAffect = Atmosphere.SkyAffect,
			MaxDistance = Atmosphere.MaxDistance
		};
	}

	protected override void OnDisabled()
	{
		if ( Renderer.IsValid() ) Renderer.ExecuteBefore = null;
		_captures = null;
		_cameras.Clear();
	}
}
