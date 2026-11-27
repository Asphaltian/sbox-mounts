namespace Source1.Dods;

public sealed class ObjectiveBomb : Weapon
{
	public override bool HasPrimaryAmmo() => true;
	public override bool HasSecondaryAmmo() => true;
	public override void SecondaryAttack() => PrimaryAttack();
	protected override bool OnDrop() => false;

	public override void PrimaryAttack()
	{
		if ( !Owner.IsValid() ) return;
		var target = Scene.GetAll<BombTarget>().Where( x => x.CanPlant( Owner ) )
			.MinBy( x => x.WorldPosition.DistanceSquared( Owner.WorldPosition ) );
		target?.Use( Owner.GameObject );
	}
}
