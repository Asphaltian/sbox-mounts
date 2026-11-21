namespace GoldSrc.HalfLife;

[Hide]
public sealed class PlayerRocket : NpcProjectile
{
	internal override void OnImpact( IDamageable victim, Vector3 position, Vector3 normal )
		=> Blast.Detonate( GameObject, Shooter, position, normal, Damage, true );
}
