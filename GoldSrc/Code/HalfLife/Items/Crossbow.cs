namespace GoldSrc.HalfLife;

[Title( "Crossbow" ), Category( "Half-Life" )]
[WeaponPickup( "crossbow", "bolts", 50 )]
public sealed class Crossbow : ProjectileWeapon
{
	internal override float ZoomFieldOfView => 20;

	internal override string DeploySequence => Clip1 == 0 ? "draw2" : "draw1";
	internal override string IdleSequence => Clip1 == 0 ? "idle2" : "idle1";
	internal override string ShotSequence => Clip1 == 0 ? "fire3" : "fire1";

	internal override Vector3 WorldMuzzleOffset( bool hd ) => hd ? new( 22.4f, 1.5f, 2.9f ) : new( 19.3f, 0.8f, 2.6f );

	internal override void Configure()
	{
		HoldType = WeaponHolds.Rifle;
		PrimaryDelay = 0.75f;
		ClipMaxSize = 5;
		SecondaryDelay = 1;
		ReloadTime = 4.5f;
	}

	protected override void OnReloadStarted()
	{
		base.OnReloadStarted();
		PlayBoltSound();
	}

	internal override void PlayLaunchSound()
	{
		PlaySample( "weapons/xbow_fire1.wav", pitch: Game.Random.Int( 93, 108 ) / 100f );
		PlayBoltSound();
	}

	void PlayBoltSound() =>
		PlaySample( "weapons/xbow_reload1.wav", Game.Random.Float( 0.95f, 1 ), Game.Random.Int( 93, 108 ) / 100f );

	public override void PrimaryAttack()
	{
		if ( Zoomed ) FireScoped( AimRay );
		else
		{
			SetNextSecondaryFire( PrimaryDelay );
			base.PrimaryAttack();
		}
	}

	[Rpc.Host( NetFlags.OwnerOnly )]
	void FireScoped( Ray ray ) => TryLaunch( ray, ReleaseScoped );

	bool ReleaseScoped( Ray ray )
	{
		var hit = BulletTrace( ray, 8192, 0 ).Run();
		if ( !hit.Hit ) return true;
		var victim = hit.GameObject?.GetComponentInParent<IDamageable>();
		if ( victim is not null ) ShootBullet( hit, 120, 0, new TagSet { "bullet", "nevergib" } );
		var stick = victim is null && hit.Body?.BodyType != PhysicsBodyType.Dynamic && !hit.HasTag( "glass" );
		CrossbowBolt.ImpactEffects( hit.EndPosition, ray.Forward, victim is not null, stick );
		return true;
	}

	internal override bool Release( Ray ray )
	{
		GoldSrc.HalfLife.Npc.PunchPlayer( Owner, Vector3.Zero, new Angles( -2, 0, 0 ) );
		NpcProjectile.Fire<CrossbowBolt>( Owner.GameObject, CrossbowBolt.ModelAsset, 50, null, ray.Position, ray.Forward,
			WaterBody.Find( Scene, Owner.EyePosition ).IsValid() ? 1000 : 2000 );
		return true;
	}
}
