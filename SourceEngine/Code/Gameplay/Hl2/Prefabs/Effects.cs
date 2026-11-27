namespace Source1.Hl2.Prefabs;

[Library]
public static class Effects
{
	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> All()
	{
		yield return new( "hl2", EntityKind.Effect, "acid_impact", null, null, null )
		{
			Build = p =>
			{
				var mountedEffect = p.Effect<MountedEffect>( 2 );
				mountedEffect.Particle = "antlion_spit";
				mountedEffect.Sound = "GrenadeSpit.Hit";
			}
		};

		yield return new( "hl2", EntityKind.Effect, "explosion", null, null, null )
		{
			Build = p =>
			{
				var mountedEffect = p.Effect<MountedEffect>( 3 );
				mountedEffect.Sound = "BaseGrenade.Explode";
				var explosionEffect = p.Root.AddComponent<ExplosionEffect>();
				explosionEffect.Sound = "";
				explosionEffect.CreateScorch = false;
				explosionEffect.MountName = "hl2";
				var radiusDamage = p.Root.AddComponent<RadiusDamage>();
				radiusDamage.Radius = 250;
				radiusDamage.PhysicsForceScale = 1.5f;
			}
		};

		yield return new( "hl2", EntityKind.Effect, "flechette_explosion", null, null, null )
		{
			Build = p =>
			{
				var mountedEffect = p.Effect<MountedEffect>( 2 );
				mountedEffect.Particle = "hunter_projectile_explosion_1";
				mountedEffect.Sound = "NPC_Hunter.FlechetteExplode";
			}
		};
	}
}
