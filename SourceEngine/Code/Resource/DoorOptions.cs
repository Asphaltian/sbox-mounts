using System;

namespace Source1;

[Sandbox.ModelEditor.GameData( "source_door_options" ), Hide]
public sealed class DoorOptions
{
	public Dictionary<string, Dictionary<string, string>> Groups { get; set; } = new( StringComparer.OrdinalIgnoreCase );

	internal static DoorOptions Read( VmtNode options )
	{
		var result = new DoorOptions();
		foreach ( var group in options.Children.Where( x => x.IsBlock ) )
			result.Groups[group.Name] = new( group.Values, StringComparer.OrdinalIgnoreCase );
		return result;
	}

	internal string Get( string group, string role ) =>
		Groups.GetValueOrDefault( group )?.GetValueOrDefault( role ) ?? Groups.GetValueOrDefault( "defaults" )?.GetValueOrDefault( role );
}
