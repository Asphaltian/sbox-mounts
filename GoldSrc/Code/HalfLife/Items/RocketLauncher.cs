namespace GoldSrc.HalfLife;

[Title( "Rocket Launcher" ), Category( "Half-Life" )]
[WeaponPickup( "rpg", "rockets", 5 )]
public sealed class RocketLauncher : ProjectileWeapon
{
	internal override string DeploySequence => "draw1";
	internal override string ShotSequence => "fire";
	internal override string IdleSequence => "idle";
	internal override string ShotSound => "weapons/rocketfire1.wav";
	internal override Vector3 WorldMuzzleOffset( bool hd ) => hd ? new( 23.4f, 0.3f, 5.4f ) : new( 22.5f, 1.4f, 4.5f );

	internal override void Configure()
	{
		HoldType = WeaponHolds.Shoulder;
		PrimaryDelay = 1.5f;
		ClipMaxSize = 1;
		StartingAmmo = 1;
	}

	internal override bool Release( Ray ray )
	{
		NpcProjectile.Fire<PlayerRocket>( Owner.GameObject, "mount://hl1/valve/models/rpgrocket.mdl.vmdl", 100, null, ray.Position, ray.Forward, 1000 );
		return true;
	}
}
