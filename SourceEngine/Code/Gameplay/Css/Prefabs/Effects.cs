namespace Source1.Css.Prefabs;

[Library]
public static class Effects
{
	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> All()
	{
		yield return Explosion( "explosion", "BaseGrenade.Explode", 350 );
		yield return Explosion( "c4_explosion", "C4.Explode", 1750 );
	}

	static EntityDefinition Explosion( string name, string sound, float radius ) =>
		new( "css", EntityKind.Effect, name, null, null, null )
		{
			Build = p =>
			{
				var radiusDamage = p.Root.AddComponent<RadiusDamage>();
				radiusDamage.Radius = radius;
				radiusDamage.PhysicsForceScale = 1.5f;
				p.Effect<ExplosionEffect>( 3 ).Sound = sound;
			}
		};
}
