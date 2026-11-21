namespace GoldSrc.HalfLife;

[Title( "Rocket Launcher" ), Category( "Half-Life" )]
[WeaponPickup( "rpg", "rockets", 5 )]
public sealed partial class RocketLauncher : ProjectileWeapon
{
	internal override string DeploySequence => Clip1 > 0 ? "draw1" : "draw2";
	internal override string ShotSequence => "fire";
	internal override string IdleSequence => Clip1 > 0 ? "idle" : "idle2";
	internal override void PlayLaunchSound()
	{
		PlaySample( "weapons/rocketfire1.wav", 0.9f );
		PlaySample( "weapons/glauncher.wav", 0.7f );
	}
	internal override Vector3 WorldMuzzleOffset( bool hd ) => hd ? new( 23.4f, 0.3f, 5.4f ) : new( 22.5f, 1.4f, 4.5f );

	internal override void Configure()
	{
		HoldType = WeaponHolds.Shoulder;
		PrimaryDelay = 1.5f;
		SecondaryDelay = 0.2f;
		CanCancelReload = false;
		ClipMaxSize = 1;
		StartingAmmo = 1;
	}

	internal override bool Release( Ray ray )
	{
		var rocket = NpcProjectile.Fire<PlayerRocket>( Owner.GameObject, "mount://hl1/valve/models/rpgrocket.mdl.vmdl", 100, null, ray.Position, ray.Forward, 250 );
		rocket.Launcher = this;
		ActiveRockets++;
		return true;
	}
}
