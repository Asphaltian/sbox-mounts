namespace GoldSrc.HalfLife;

[Title( "Crossbow" ), Category( "Half-Life" )]
[WeaponPickup( "crossbow", "bolts", 50 )]
public sealed class Crossbow : ProjectileWeapon
{
	bool _zoomed;

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

	public override void SecondaryAttack() => _zoomed = !_zoomed;

	protected override void ModifyCamera( CameraComponent camera, ref CameraView view )
	{
		base.ModifyCamera( camera, ref view );
		if ( _zoomed ) view.FieldOfView = 20;
	}

	protected override void OnHolstered()
	{
		_zoomed = false;
		base.OnHolstered();
	}

	protected override void OnReloadStarted()
	{
		_zoomed = false;
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

	internal override bool Release( Ray ray )
	{
		NpcProjectile.Fire<CrossbowBolt>( Owner.GameObject, "mount://hl1/valve/models/crossbow_bolt.mdl.vmdl", 50, null, ray.Position, ray.Forward,
			WaterBody.Find( Scene, Owner.EyePosition ).IsValid() ? 1000 : 2000 );
		return true;
	}
}
