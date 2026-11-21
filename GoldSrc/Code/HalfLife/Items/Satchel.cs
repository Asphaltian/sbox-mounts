namespace GoldSrc.HalfLife;

[Title( "Satchel Charge" ), Category( "Half-Life" )]
[WeaponPickup( "satchel", "satchels", 5 )]
public sealed class Satchel : ProjectileWeapon
{
	internal override string ShotSequence => "drop";

	internal override void Configure()
	{
		HoldType = WeaponHolds.Throwable;
		PrimaryDelay = 0.55f;
		UsesClips = false;
		StartingAmmo = 1;
	}

	internal override bool Release( Ray ray )
	{
		PlacedCharge.Throw( Owner, ray );
		return true;
	}

	public override void SecondaryAttack() => DetonateCharges();

	[Rpc.Host( NetFlags.OwnerOnly )]
	void DetonateCharges()
	{
		if ( !IsActive || !Owner.IsValid() ) return;
		foreach ( var charge in Scene.GetAllComponents<PlacedCharge>().Where( x => !x.IsMine && x.Thrower == Owner.GameObject ).ToArray() )
			charge.Detonate();
	}
}
