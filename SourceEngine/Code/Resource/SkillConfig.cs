using System;

namespace Source1;

/// <summary>Exposes the parsed settings to gameplay and the individual config as a mounted text resource.</summary>
internal sealed class SkillConfigLoader( SkillConfig config ) : ResourceLoader<SourceGameMount>
{
	internal IReadOnlyDictionary<string, float> Values => config.Values;
	IReadOnlyDictionary<string, float> merged;

	internal IReadOnlyDictionary<string, float> WithDefaults( IReadOnlyDictionary<string, float> defaults )
	{
		if ( merged is not null ) return merged;
		var values = new Dictionary<string, float>( defaults, StringComparer.OrdinalIgnoreCase );
		foreach ( var (name, value) in Values ) values[name] = value;
		return merged = values;
	}

	protected override object Load() => System.Text.Json.JsonSerializer.Serialize( Values );
}
