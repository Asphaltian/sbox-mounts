namespace GoldSrc.HalfLife;

[Title( "Laser Tripmine" ), Category( "Half-Life" )]
[WeaponPickup( "tripmine", "tripmines", 5, PickupModel = "v_tripmine" )]
public sealed class Tripmine : ProjectileWeapon
{
	internal override string ShotSequence => "draw";
	internal override string IdleSequence => Game.Random.Float( 0, 1 ) switch
	{
		<= 0.25f => "idle1",
		<= 0.75f => "idle2",
		_ => "fidget"
	};
	public override Ray AimRay => Owner.IsValid() ? Owner.EyeTransform.ForwardRay : base.AimRay;

	internal override void Configure()
	{
		HoldType = WeaponHolds.Throwable;
		PrimaryDelay = 0.3f;
		UsesClips = false;
		StartingAmmo = 1;
	}

	internal override void PosePickup( SkinnedModelRenderer renderer ) => PlacedCharge.PoseMine( renderer, "ground" );

	internal override bool Release( Ray ray )
	{
		var hit = Scene.Trace.Ray( ray, 128 ).IgnoreGameObjectHierarchy( Owner.GameObject ).Run();
		if ( !hit.Hit || hit.StartedSolid ) return false;
		PlacedCharge.Attach( Owner, hit );
		return true;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		if ( Networking.IsHost && IsActive && IsHeld && !HasPrimaryAmmo() ) Inventory.Remove( this );
	}
}
