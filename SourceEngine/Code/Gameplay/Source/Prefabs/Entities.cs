using System;

namespace Source1;

public enum EntityKind
{
	Weapon,
	ViewModel,
	WorldModel,
	Npc,
	Pickup,
	Charger,
	Effect
}

/// <summary>
/// One entity the mounts build as a prefab: its game, kind and name, the spawn menu title and icon pose of the ones
/// players can spawn.
/// </summary>
public sealed record EntityDefinition( string Game, EntityKind Kind, string Name, string Title, string PreviewModel, string PreviewSequence )
{
	/// <summary>Shown under the title, such as the episode an NPC is from.</summary>
	public string Description { get; init; }
	internal Action<EntityPrefab> Build { get; init; }
	internal string ModelPath { get; init; } = PreviewModel;
	internal string FallbackModel { get; init; }
	internal string[] RequiredPrefabs { get; init; } = [];

	/// <summary>The prefab's path in its mount, such as entities/npcs/combine_soldier.prefab.</summary>
	public string RelativePath => $"entities/{Entities.Folder( Kind )}/{Name}.prefab";

	public string Path => $"mount://{Game}/{RelativePath}";

	/// <summary>The resource tag that lists it for spawning: <see cref="Entities.NpcTag"/>, <see cref="Entities.WeaponTag"/> or <see cref="Entities.ItemTag"/>. Null for parts and effects.</summary>
	public string Tag => Title is null ? null : Kind switch
	{
		EntityKind.Weapon => Entities.WeaponTag,
		EntityKind.Npc => Entities.NpcTag,
		EntityKind.Pickup or EntityKind.Charger => Entities.ItemTag,
		_ => null
	};
}

/// <summary>Discovers the mounted games' prefab definitions without loading their models or building any prefabs.</summary>
public static class Entities
{
	public const string NpcTag = "npc";
	public const string WeaponTag = "weapon";
	public const string ItemTag = "item";

	public static string Folder( EntityKind kind ) => kind switch
	{
		EntityKind.Weapon => "weapons",
		EntityKind.Npc => "npcs",
		EntityKind.Pickup or EntityKind.Charger => "items",
		EntityKind.ViewModel or EntityKind.WorldModel => "parts",
		_ => "effects"
	};

	[AttributeUsage( AttributeTargets.Method )]
	public sealed class DefinitionsAttribute : Attribute { }

	static object _providers;
	static IReadOnlyList<EntityDefinition> _all;

	public static IReadOnlyList<EntityDefinition> All
	{
		get
		{
			var providers = Game.TypeLibrary.GetMethodsWithAttribute<DefinitionsAttribute>();
			if ( providers != _providers )
			{
				_all = providers.SelectMany( p => (IEnumerable<EntityDefinition>)p.Method.Invoke( null ) ).ToArray();
				_providers = providers;
			}
			return _all;
		}
	}

	/// <summary>Find a named entity within a game, such as the headcrab a zombie drops.</summary>
	public static EntityDefinition Find( string game, string name ) =>
		All.FirstOrDefault( x => string.Equals( x.Game, game, StringComparison.OrdinalIgnoreCase ) && string.Equals( x.Name, name, StringComparison.OrdinalIgnoreCase ) );

	/// <summary>The built prefab of that name in a mount, null when the mount doesn't have it.</summary>
	public static PrefabFile Prefab( string game, string name )
	{
		if ( Find( game, name ) is not { } entity || Sandbox.Mounting.Directory.Get( game )?.GetByPath( entity.Path ) is null )
			return null;
		return PrefabFile.Load( entity.Path );
	}

	internal static List<EntityDefinition> Available( string game, Func<string, bool> hasModel )
	{
		var available = All.Where( e => e.Game.Equals( game, StringComparison.OrdinalIgnoreCase )
			&& (e.ModelPath is null || hasModel( e.ModelPath ) || (e.FallbackModel is not null && hasModel( e.FallbackModel )))
			&& (e.PreviewModel is null || hasModel( e.PreviewModel )) ).ToList();
		while ( true )
		{
			var paths = available.Select( e => e.RelativePath ).ToHashSet( StringComparer.OrdinalIgnoreCase );
			if ( available.RemoveAll( e => e.RequiredPrefabs.Any( path => !paths.Contains( path ) ) ) == 0 )
				return available;
		}
	}
}
