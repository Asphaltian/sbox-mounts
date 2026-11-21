namespace GoldSrc.HalfLife;

[Title( "Snarks" ), Category( "Half-Life" )]
[WeaponPickup( "squeak", "snarks", 15, ClassName = "snark", PickupModel = "w_sqknest" )]
public sealed class SnarkWeapon : ProjectileWeapon
{
	internal override string DeploySequence => "up";
	internal override string ShotSequence => "throw";
	internal override string ShotSound => $"squeek/sqk_hunt{Game.Random.Int( 2, 3 )}.wav";

	internal override void Configure()
	{
		HoldType = WeaponHolds.Throwable;
		PrimaryDelay = 0.3f;
		UsesClips = false;
		StartingAmmo = 5;
	}

	internal override bool Release( Ray ray )
	{
		var prefab = GameObject.GetPrefab( "mount://hl1/entities/npcs/monster_snark.prefab" );
		if ( !prefab.IsValid() ) return false;
		var clearance = Scene.Trace.Box( new BBox( new Vector3( -4, -4, 0 ), new Vector3( 4, 4, 8 ) ),
			ray.Position, ray.Position + ray.Forward * 32 ).IgnoreGameObjectHierarchy( Owner.GameObject ).Run();
		if ( clearance.StartedSolid || clearance.Fraction <= 0 ) return false;
		var snark = prefab.Clone( new Transform( clearance.EndPosition, Rotation.LookAt( ray.Forward ) ) );
		snark.GetComponent<Snark>().Thrower = Owner.GameObject;
		snark.NetworkSpawn( true, null );
		if ( snark.GetComponent<Rigidbody>() is { } body ) body.Velocity = ray.Forward * 200 + Owner.Velocity;
		return true;
	}
}
