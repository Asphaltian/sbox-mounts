namespace GoldSrc.HalfLife;

[Hide]
public sealed class PlayerRocket : RocketProjectile
{
	internal override float IgnitionDelay => 0.4f;
	internal override float SmokeLifetime => 4;
	internal override float BurnTime => 1;

	internal override void OnImpact( IDamageable victim, Vector3 position, Vector3 normal )
		=> Blast.Detonate( GameObject, Shooter, position, normal, Damage, true );
}
