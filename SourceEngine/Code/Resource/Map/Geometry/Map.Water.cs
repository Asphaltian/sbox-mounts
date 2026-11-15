using System;
using Sandbox.Rendering;
using Sandbox.Volumes;

namespace Source1;

partial class MapLoader
{
	private readonly List<(Model Model, Material Material, float Height, bool Above, bool Skybox)> _waterModels = [];
	private void BuildWater( BspFile bsp, GameObject geometry, Fog atmosphere = null, bool skybox = false )
	{
		foreach ( var water in _waterModels )
		{
			if ( water.Skybox != skybox )
				continue;
			var child = new GameObject( false, water.Above ? "water" : "water underside" )
			{
				Parent = geometry,
				IsStatic = true
			};
			child.Tags.Add( "world" );
			child.Tags.Add( "source1_water" );
			var renderer = child.AddComponent<ModelRenderer>();
			renderer.Model = water.Model;
			renderer.RenderType = ModelRenderer.ShadowRenderType.Off;
			var surface = child.AddComponent<SourceWater>();
			surface.Renderer = renderer;
			surface.Material = water.Material;
			surface.Height = water.Height;
			surface.AboveWater = water.Above;
			surface.Atmosphere = atmosphere;
			var parameters = WaterParameters( water.Material );
			var cheap = parameters.Boolean( "$forcecheap" );
			surface.Reflect = !cheap && parameters.Boolean( "$forceexpensive", true )
				&& !string.IsNullOrWhiteSpace( parameters.String( "$reflecttexture", "_rt_WaterReflection" ) );
			surface.Refract = !cheap && !string.IsNullOrWhiteSpace( parameters.String( "$refracttexture", "_rt_WaterRefraction" ) );
			surface.ReflectEntities = true;
			surface.ReflectSky = parameters.Boolean( "$reflect2dskybox", true );
			surface.ReflectSkybox3D = parameters.Boolean( "$reflect3dskybox" );
			child.Enabled = true;
		}

		BuildWaterVolumes( bsp, geometry, skybox );
	}

	private void BuildWaterModels( Dictionary<(int Texture, float Height), MeshGroup> groups, bool skybox )
	{
		foreach ( var (key, group) in groups )
		{
			var builder = Model.Builder;
			var (bounds, _) = BuildWorldMesh( builder, group.Material, [group] );
			var model = CreateWorldModel( builder.WithViewBounds( bounds ), $"{(skybox ? "skybox_" : "")}water_{key.Texture}_{_waterModels.Count}.vmdl" );
			var above = WaterParameters( group.Material ).Boolean( "$abovewater", true );
			_waterModels.Add( (model, group.Material, key.Height, above, skybox) );
		}
	}

	VmtParameters WaterParameters( Material material )
	{
		var source = Host.GetMaterialSource( material )
			?? throw new InvalidDataException( $"{FullPath}: water material '{material.Name}' has no Source parameters." );
		return new VmtParameters( VmtFile.SelectParameters( source.Source ), FullPath );
	}

	Material WaterBottomMaterial( Material material )
	{
		var path = WaterParameters( material ).String( "$bottommaterial" );
		if ( string.IsNullOrWhiteSpace( path ) ) return material;
		var root = Host.GetMaterialSource( material ).ContentRoot;
		path = VmtFile.NormalizePath( path, ".vmt" );
		if ( Host.GetContentPath( path, root ) is null )
		{
			Log.Warning( $"{FullPath}: water bottom material '{path}' is missing; retaining the surface material." );
			return material;
		}
		return Host.LoadContentMaterial( path, root );
	}

	void BuildWaterVolumes( BspFile bsp, GameObject geometry, bool skybox )
	{
		var (nodes, root) = bsp.BuildWaterTree( skybox );
		if ( root == -1 ) return;
		var map = geometry.AddComponent<SourceWaterMap>();
		map.Nodes = nodes;
		map.Root = root;
		if ( !skybox && Host.GetSurfaceSound( "Player.AmbientUnderWater" ) is { } ambient )
		{
			var audio = geometry.AddComponent<SourceWaterAudio>();
			audio.Map = map;
			audio.Ambient = ambient;
		}
		var indices = nodes.SelectMany( node => new[] { node.Front, node.Back } ).Append( root )
			.Where( index => index < -1 ).Select( index => -index - 2 ).Distinct();
		foreach ( var index in indices )
		{
			var data = bsp.Water[index];
			var material = LoadWorldMaterial( bsp, bsp.TexInfos[data.TexInfo].TexData, -1, Material.Load( "materials/error.vmat" ) );
			if ( Host.GetMaterialSource( material ) is null )
			{
				Log.Warning( $"{FullPath}: water volume {index} has no valid Source material; its fog was not imported." );
				continue;
			}
			material = WaterBottomMaterial( material );
			var child = new GameObject( false, "water fog" ) { Parent = geometry, IsStatic = true };
			var volume = child.AddComponent<SourceWaterVolume>();
			volume.Map = map;
			volume.Water = index;
			volume.Priority = 10;
			volume.BlendDistance = 0;
			volume.SceneVolume = new SceneVolume { Type = SceneVolume.VolumeTypes.Infinite };
			var fog = child.AddComponent<Fog>();
			fog.FogEnabled = WaterParameters( material ).Boolean( "$fogenable" );
			fog.Mode = FogMode.Distance;
			fog.DistanceMetric = FogDistanceMetric.ViewDepth;
			var start = material.GetVector4( "g_flSourceFogStart" ).x;
			var end = material.GetVector4( "g_flSourceFogEnd" ).x;
			fog.DistanceRange = new Vector2( start, start == end ? start + 1 : end );
			fog.FalloffExponent = 2;
			fog.Color = material.GetColor( "g_vSourceFogColor" ).WithAlpha( 1 );
			fog.SunLightingEnabled = false;
			fog.SkyAffect = 1;
			var overlayPath = WaterParameters( material ).String( "$underwateroverlay" );
			if ( !string.IsNullOrWhiteSpace( overlayPath ) )
			{
				try
				{
					var overlay = Host.LoadContentMaterial( overlayPath, Host.GetMaterialSource( material ).ContentRoot );
					var source = Host.GetMaterialSource( overlay )
						?? throw new InvalidDataException( $"Underwater overlay '{overlayPath}' has no Source material." );
					child.AddComponent<SourceUnderwater>().Overlay = source.CreateMaterial(
						$"{overlay.Name}_underwater.vmat", screenOverlay: true );
				}
				catch ( Exception e ) when ( e is FileNotFoundException or InvalidDataException or NotSupportedException )
				{
					Log.Warning( $"{FullPath}: underwater overlay '{overlayPath}' was not imported: {e.Message}" );
				}
			}
			child.Enabled = true;
		}
	}
}
