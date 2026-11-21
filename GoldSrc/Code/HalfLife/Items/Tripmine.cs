namespace GoldSrc.HalfLife;

[Title( "Laser Tripmine" ), Category( "Half-Life" )]
[WeaponPickup( "tripmine", "tripmines", 5, PickupModel = "v_tripmine" )]
public sealed class Tripmine : ProjectileWeapon
{
	internal override string ShotSequence => "place";

	internal override void Configure()
	{
		HoldType = WeaponHolds.Throwable;
		PrimaryDelay = 0.4f;
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
}
