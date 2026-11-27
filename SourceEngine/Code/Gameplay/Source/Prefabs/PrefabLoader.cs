using System;

namespace Source1;

/// <summary>Builds one <see cref="EntityDefinition"/> as a prefab: in its owning mount.</summary>
internal sealed class PrefabLoader( EntityDefinition entity ) : ResourceLoader<SourceGameMount>
{
	PrefabFile prefab;

	protected override object Load()
	{
		var builder = new PrefabBuilder().WithName( Path );
		if ( entity.Title is not null )
		{
			builder.WithMetadata( "Title", entity.Title )
				.WithMetadata( "Description", entity.Description )
				.WithMetadata( "Group", Host.Title )
				.WithMetadata( "PreviewModel", entity.PreviewModel is null ? null : $"mount://{Host.Ident}/{entity.PreviewModel}.vmdl" )
				.WithMetadata( "PreviewSequence", entity.PreviewSequence );
		}

		using ( builder.Scope() )
		{
			var root = new GameObject();
			// Keep gameplay callbacks dormant while assembling the prefab.
			root.Scene.Enabled = false;
			entity.Build( new EntityPrefab( root, Host, entity ) );
		}

		prefab = builder.Create() ?? throw new InvalidOperationException( $"{Path}: could not create the prefab." );
		return prefab;
	}

	protected override void Shutdown()
	{
		PrefabBuilder.Destroy( prefab );
		prefab = null;
		base.Shutdown();
	}
}
