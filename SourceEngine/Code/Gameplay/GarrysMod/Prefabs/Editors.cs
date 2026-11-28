namespace Source1.GarrysMod.Prefabs;

[Library]
public static class Editors
{
	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> All()
	{
		yield return new( "gmod", EntityKind.Pickup, "edit_sky", "Sky Editor", "models/maxofs2d/cube_tool.mdl", null )
		{
			Description = "Edit the painted sky using the Inspect menu.",
			Build = p =>
			{
				p.AddModel( p.Root );
				p.Renderer.MaterialOverride = Material.Load( "mount://gmod/materials/gmod/edit_sky.vmt.vmat" );
				p.Model.Collider = p.Root.AddComponent<ModelCollider>();
				p.Root.AddComponent<Rigidbody>();
				p.Root.AddComponent<SkyEditor>();
			}
		};
	}
}
