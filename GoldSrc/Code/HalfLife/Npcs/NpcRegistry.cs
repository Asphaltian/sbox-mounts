namespace GoldSrc.HalfLife;

internal sealed class NpcRegistry
{
	readonly (TypeDescription Component, NpcDefinitionAttribute Definition)[] _entries;
	readonly Dictionary<string, NpcDefinitionAttribute> _models;

	internal NpcRegistry()
	{
		_entries = Game.TypeLibrary.GetTypes<Component>()
			.Select( type => (Component: type, Definition: type.GetAttribute<NpcDefinitionAttribute>()) )
			.Where( entry => entry.Definition is not null )
			.ToArray();
		_models = new( System.StringComparer.OrdinalIgnoreCase );
		// Corpses and deployment helpers share models with live NPCs. Use the live NPC's hitbox materials.
		foreach ( var entry in _entries.OrderByDescending( x => x.Component.TargetType.IsAssignableTo( typeof( Npc ) ) ) )
			_models.TryAdd( $"models/{entry.Definition.Model}.mdl", entry.Definition );
	}

	internal NpcDefinitionAttribute FindModel( string path ) => _models.GetValueOrDefault( path );

	internal void Mount( MountContext context, IReadOnlyDictionary<string, string> models )
	{
		foreach ( var (component, definition) in _entries )
		{
			if ( !models.TryGetValue( $"models/{definition.Model}.mdl", out var model ) )
				continue;

			models.TryGetValue( $"models/{definition.ProjectileModel}.mdl", out var projectile );
			var loader = new NpcPrefabLoader( component, definition, model, projectile );
			loader.Tags.Add( "npc" );
			context.Add( ResourceType.PrefabFile, $"entities/npcs/{definition.ClassName}.prefab", loader );
			foreach ( var alias in definition.Aliases )
				context.Add( ResourceType.PrefabFile, $"entities/npcs/{alias}.prefab", new NpcPrefabLoader( component, definition, model, projectile ) );
		}
	}
}
