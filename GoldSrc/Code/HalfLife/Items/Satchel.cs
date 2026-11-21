namespace GoldSrc.HalfLife;

[Title( "Satchel Charge" ), Category( "Half-Life" )]
[WeaponPickup( "satchel", "satchels", 5 )]
public sealed class Satchel : ProjectileWeapon
{
	internal override string ShotSequence => "drop";
	[Sync( SyncFlags.FromHost ), Change( nameof( OnRadioChanged ) )] bool RadioDeployed { get; set; }
	internal override string HeldModelName => RadioDeployed ? "satchel_radio" : base.HeldModelName;

	void OnRadioChanged( bool before, bool after )
	{
		if ( WorldModel.IsValid() ) CreateWorldModel();
	}

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
		RadioDeployed = true;
		return true;
	}

	public override void SecondaryAttack() => DetonateCharges();

	[Rpc.Host( NetFlags.OwnerOnly )]
	void DetonateCharges()
	{
		if ( !IsActive || !Owner.IsValid() ) return;
		foreach ( var charge in Scene.GetAllComponents<PlacedCharge>().Where( x => !x.IsMine && x.Thrower == Owner.GameObject ).ToArray() )
			charge.Detonate();
		RadioDeployed = false;
	}
}
