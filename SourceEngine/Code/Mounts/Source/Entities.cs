namespace Source1;

public abstract partial class SourceGameMount
{
	/// <summary>Registers the <see cref="Entities"/> of this game whose art is installed, each built when first loaded.</summary>
	private void MountEntities( MountContext context )
	{
		bool HasModel( string path ) => GetByPath( $"mount://{Ident}/{path}.vmdl" ) is not null;
		foreach ( var entity in Entities.Available( Ident, HasModel ) )
		{
			var loader = new PrefabLoader( entity );
			if ( entity.Tag is { } tag )
				loader.Tags.Add( tag );
			context.Add( ResourceType.PrefabFile, entity.RelativePath, loader );
		}
	}
}
