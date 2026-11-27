namespace Source1.Css.Prefabs;

[Library]
public static class Effects
{
	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> All()
	{
		yield return new( "css", EntityKind.Effect, "explosion", null, null, null )
		{
			Build = p =>
			{
				var radiusDamage = p.Root.AddComponent<RadiusDamage>();
				radiusDamage.Radius = 350;
				radiusDamage.PhysicsForceScale = 1.5f;
				p.Effect<ExplosionEffect>( 3 );
			}
		};
	}
}
