namespace GoldSrc.HalfLife;

[Title( "Satchel Charge" ), Category( "Half-Life" )]
[WeaponPickup( "satchel", "satchels", 5, AlternateModel = "satchel_radio" )]
public sealed class Satchel : ProjectileWeapon
{
	enum SatchelState { Charge, Radio, Detonating }
	[Sync( SyncFlags.FromHost ), Change( nameof( OnStateChanged ) )] SatchelState State { get; set; }
	TimeUntil _radioReady, _redraw;

	internal override string HeldModelName => State == SatchelState.Charge ? Definition.Model : Definition.AlternateModel;
	internal override string ShotSequence => "draw";
	public override bool ShouldAvoid => State == SatchelState.Charge && base.ShouldAvoid;

	void OnStateChanged( SatchelState before, SatchelState after )
	{
		if ( after == SatchelState.Detonating )
		{
			ViewModel?.GetComponent<StudioWeaponView>()?.Play( "fire" );
			return;
		}

		if ( ViewModel.IsValid() )
		{
			DestroyViewModel();
			CreateViewModel();
		}
		if ( WorldModel.IsValid() ) CreateWorldModel();
	}

	internal override void Configure()
	{
		HoldType = WeaponHolds.Throwable;
		DeployTime = PrimaryDelay = 1;
		SecondaryDelay = 0.5f;
		UsesClips = false;
		StartingAmmo = 1;
	}

	public override bool CanPrimaryAttack() => State != SatchelState.Detonating && base.CanPrimaryAttack();
	public override bool CanSecondaryAttack() => State == SatchelState.Radio && base.CanSecondaryAttack();

	internal override bool Release( Ray ray )
	{
		if ( State == SatchelState.Detonating || _redraw > 0 ) return false;
		PlacedCharge.Throw( Owner, ray );
		State = SatchelState.Radio;
		_radioReady = 0.5f;
		return true;
	}

	public override void SecondaryAttack() => DetonateCharges();

	[Rpc.Host( NetFlags.OwnerOnly )]
	void DetonateCharges()
	{
		if ( !IsActive || !Owner.IsValid() || State != SatchelState.Radio || _radioReady > 0 ) return;
		State = SatchelState.Detonating;
		_redraw = 0.5f;
		foreach ( var charge in Scene.GetAllComponents<PlacedCharge>().Where( x => !x.IsMine && x.Thrower == Owner.GameObject &&
			x.WorldPosition.DistanceSquared( Owner.WorldPosition ) <= 4096 * 4096 ).ToArray() )
			charge.Detonate();
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		if ( !Networking.IsHost || State != SatchelState.Detonating || _redraw > 0 ) return;
		State = SatchelState.Charge;
		_redraw = 0.5f;
		if ( IsHeld && !HasPrimaryAmmo() ) Inventory.Remove( this );
	}
}
