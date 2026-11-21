namespace GoldSrc.HalfLife;

[Title( "Rocket Launcher" ), Category( "Half-Life" )]
[WeaponPickup( "rpg", "rockets", 5 )]
public sealed partial class RocketLauncher : ProjectileWeapon
{
	internal override string DeploySequence => Clip1 > 0 ? "draw1" : "draw2";
	internal override string ShotSequence => "fire";
	internal override float? IdleAfterLaunch => 1.5f;
	internal override bool CanIdle => Ammo1 > 0;
	internal override void PlayIdle( StudioWeaponView view )
	{
		if ( !LaserEnabled && Game.Random.Float( 0, 1 ) > 0.75f &&
			view.Play( Clip1 > 0 ? "fidget" : "fidget2", idleAfter: 3 ) ) return;
		// The HD model has no empty-launcher fidget.
		view.Play( Clip1 > 0 ? "idle" : "idle2", idleAfter: 6 );
	}
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
		var damage = Networking.MaxPlayers > 1 ? 120 : 100;
		var rocket = NpcProjectile.Fire<PlayerRocket>( Owner.GameObject, "mount://hl1/valve/models/rpgrocket.mdl.vmdl", damage, null, ray.Position, ray.Forward, 250 );
		rocket.Launcher = this;
		ActiveRockets++;
		return true;
	}
}
