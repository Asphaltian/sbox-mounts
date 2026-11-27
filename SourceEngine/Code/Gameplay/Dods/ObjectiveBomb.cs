namespace Source1.Dods;

public sealed class ObjectiveBomb : Weapon
{
	public override bool HasPrimaryAmmo() => true;
	public override bool HasSecondaryAmmo() => true;
	public override bool CanPrimaryAttack() => base.CanPrimaryAttack() && PlantTarget().IsValid();
	public override bool CanSecondaryAttack() => base.CanSecondaryAttack() && PlantTarget().IsValid();
	public override void SecondaryAttack() => PrimaryAttack();
	protected override bool OnDrop() => false;

	public override void PrimaryAttack() => PlantTarget()?.Use( Owner.GameObject );

	BombTarget PlantTarget()
	{
		var owner = Owner;
		if ( !owner.IsValid() ) return null;
		var center = (owner.WorldPosition + owner.EyeTransform.Position) * 0.5f;
		return Scene.GetAll<BombTarget>().Where( x => x.CanPlant( owner ) )
			.MinBy( x => x.WorldPosition.DistanceSquared( center ) );
	}
}
