using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Source1;

/// <summary>Reads numeric skill defaults as data. Config commands are never executed.</summary>
internal sealed class SkillConfig
{
	private readonly Dictionary<string, float> values = new( StringComparer.OrdinalIgnoreCase );
	internal IReadOnlyDictionary<string, float> Values => values;

	internal static SkillConfig Parse( string text )
	{
		var result = new SkillConfig();
		foreach ( var commandLine in text.Split( ["\r\n", "\n", "\r"], StringSplitOptions.None ) )
		{
			var line = commandLine;
			var comment = line.IndexOf( "//", StringComparison.Ordinal );
			if ( comment >= 0 ) line = line[..comment];
			foreach ( var command in line.Split( ';' ) )
			{
				var match = Regex.Match( command, "^\\s*\"?(sk_[a-zA-Z0-9_]+)\"?\\s+\"?([-+0-9.eE]+)\"?\\s*$", RegexOptions.CultureInvariant );
				if ( match.Success && float.TryParse( match.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value ) && float.IsFinite( value ) )
					result.values[match.Groups[1].Value] = value;
			}
		}
		return result;
	}

	ILookup<string, KeyValuePair<string, float>> _byName;

	IEnumerable<KeyValuePair<string, float>> Matching( string name )
	{
		_byName ??= values.ToLookup( p => p.Key.Replace( "_", "" ), StringComparer.OrdinalIgnoreCase );
		return _byName[name.Replace( "_", "" )];
	}

	internal int? AmmoCapacity( string ammo )
	{
		if ( string.IsNullOrWhiteSpace( ammo ) ) return null;
		foreach ( var (name, value) in Matching( $"sk_max_{AmmoName( ammo )}" ) )
		{
			if ( name.StartsWith( "sk_max_", StringComparison.OrdinalIgnoreCase ) && value > 0 && value < int.MaxValue && value == MathF.Truncate( value ) )
				return (int)value;
		}
		return null;
	}

	static string AmmoName( string ammo ) => string.Equals( ammo, "XBowBolt", StringComparison.OrdinalIgnoreCase ) ? "crossbow" : ammo;

	internal float? WeaponDamage( string ammo, string weaponClass, bool npc = false )
		=> WeaponValue( ammo, weaponClass, npc ? "sk_npc_dmg_" : "sk_plr_dmg_", "" );

	internal float? WeaponRadius( string ammo, string weaponClass ) => WeaponValue( ammo, weaponClass, "sk_", "_radius" );

	private float? WeaponValue( string ammo, string weaponClass, string prefix, string suffix )
	{
		var weapon = weaponClass?.StartsWith( "weapon_", StringComparison.OrdinalIgnoreCase ) == true ? weaponClass[7..] : weaponClass;
		// These Source identifiers are aliases of the config's authored names.
		if ( weapon?.Equals( "frag", StringComparison.OrdinalIgnoreCase ) == true ) weapon = "fraggrenade";
		if ( weapon?.Equals( "stunstick_player", StringComparison.OrdinalIgnoreCase ) == true ) weapon = "stunstick";
		return Find( weapon ) ?? Find( AmmoName( ammo ) );

		float? Find( string name )
		{
			if ( string.IsNullOrWhiteSpace( name ) ) return null;
			foreach ( var entry in Matching( prefix + name + suffix ) )
				if ( entry.Value >= 0 ) return entry.Value;
			return null;
		}
	}
}
