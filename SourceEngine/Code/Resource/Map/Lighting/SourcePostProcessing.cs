using System;
using System.Globalization;

namespace Source1;

internal sealed class SourcePostProcessing
{
	internal sealed class TonemapSettings
	{
		public float MinimumExposure { get; set; } = 0.5f;
		public float MaximumExposure { get; set; } = 2;
		public float BloomScale { get; set; } = 1;
	}

	internal sealed class ColorCorrectionSettings
	{
		internal ColorCorrectionSettings( MapEntity entity )
		{
			Entity = entity;
			Enabled = entity.Integer( "StartDisabled" ) == 0 && !IsClientSelected;
			Weight = entity.Get( "classname" ).Equals( "color_correction_volume", StringComparison.OrdinalIgnoreCase )
				? Math.Max( 0, entity.Number( "maxweight", 1 ) ) : 1;
		}

		public MapEntity Entity { get; }
		public bool Enabled { get; set; }
		public float Weight { get; set; }
		public bool IsClientSelected => Entity.Get( "classname" ).Equals( "color_correction", StringComparison.OrdinalIgnoreCase )
			&& (Entity.Integer( "spawnflags" ) & 2) != 0;
		public bool IsGlobal => Entity.Get( "classname" ).Equals( "color_correction", StringComparison.OrdinalIgnoreCase )
			&& (Entity.Number( "minfalloff" ) == -1 || Entity.Number( "maxfalloff", 1000 ) == -1
				|| Entity.Number( "minfalloff" ) == Entity.Number( "maxfalloff", 1000 ));
	}

	public Dictionary<MapEntity, TonemapSettings> Controllers { get; } = [];
	public Dictionary<MapEntity, ColorCorrectionSettings> Corrections { get; } = [];
	public MapEntity Master { get; private set; }
	public float Rate { get; private set; } = 1;
	public TonemapSettings Global => Master is null ? new() : Controllers[Master];

	internal static SourcePostProcessing Read( IReadOnlyList<MapEntity> entities )
	{
		var result = new SourcePostProcessing();
		foreach ( var entity in entities )
		{
			switch ( entity.Get( "classname", "" ).ToLowerInvariant() )
			{
				case "env_tonemap_controller":
					result.Controllers.Add( entity, new() );
					if ( result.Master is null || (entity.Integer( "spawnflags" ) & 1) != 0 )
						result.Master = entity;
					break;
				case "color_correction":
				case "color_correction_volume":
					result.Corrections.Add( entity, new( entity ) );
					break;
			}
		}

		var inputs = new List<(MapEntity Source, BspEntityConnection Connection)>();
		foreach ( var source in entities.Where( entity => string.Equals( entity.Get( "classname" ), "logic_auto", StringComparison.OrdinalIgnoreCase ) ) )
		{
			if ( !string.IsNullOrEmpty( source.Get( "globalstate" ) ) )
				continue;
			foreach ( var output in new[] { "OnNewGame", "OnMapSpawn", "OnMultiNewMap" } )
				foreach ( var (key, value) in source.KeyValues.Where( pair => pair.Key.Equals( output, StringComparison.OrdinalIgnoreCase ) ) )
					inputs.Add( (source, BspEntityConnection.Read( key, value )) );
		}

		foreach ( var (source, connection) in inputs.OrderBy( input => input.Connection.Delay ) )
		{
			foreach ( var target in BspEntityConnection.ResolveTargets( source, connection.Target, entities ) )
				result.Apply( target, connection.Input, connection.Parameter );
		}
		return result;
	}

	private void Apply( MapEntity target, string input, string parameter )
	{
		if ( Controllers.TryGetValue( target, out var controller ) )
		{
			switch ( input.ToLowerInvariant() )
			{
				case "setautoexposuremin":
					controller.MinimumExposure = Number( target, input, parameter );
					if ( controller.MinimumExposure <= 0 ) controller.MinimumExposure = 0.5f;
					return;
				case "setautoexposuremax":
					controller.MaximumExposure = Number( target, input, parameter );
					if ( controller.MaximumExposure <= 0 ) controller.MaximumExposure = 2;
					return;
				case "usedefaultautoexposure":
					controller.MinimumExposure = 0.5f;
					controller.MaximumExposure = 2;
					return;
				case "setbloomscale":
					controller.BloomScale = Math.Max( 0, Number( target, input, parameter ) );
					return;
				case "usedefaultbloomscale":
					controller.BloomScale = 1;
					return;
				case "settonemaprate":
					Rate = Math.Max( 0, Number( target, input, parameter ) );
					return;
			}
		}
		if ( Corrections.TryGetValue( target, out var correction ) && (input.Equals( "Enable", StringComparison.OrdinalIgnoreCase )
			|| input.Equals( "Disable", StringComparison.OrdinalIgnoreCase )) )
		{
			if ( correction.IsClientSelected )
				return;
			correction.Enabled = input.Equals( "Enable", StringComparison.OrdinalIgnoreCase );
			correction.Weight = correction.Enabled ? Math.Max( 0, target.Number( "maxweight", 1 ) ) : 0;
		}
	}

	private static float Number( MapEntity entity, string input, string value )
	{
		if ( float.TryParse( value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number ) && float.IsFinite( number ) )
			return number;
		throw new InvalidDataException( $"Post-processing input '{input}' on '{entity.Get( "targetname", "<unnamed>" )}' requires a finite number, not '{value}'." );
	}
}
