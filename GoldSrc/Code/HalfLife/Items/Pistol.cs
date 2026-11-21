namespace GoldSrc.HalfLife;

[Title( "9mm Handgun" ), Category( "Half-Life" )]
[WeaponPickup( "9mmhandgun", "9mm", 250 )]
public sealed class Pistol : BulletWeapon
{
	bool _rapidFire;

	internal override bool CanIdle => Clip1 > 0;
	internal override string IdleSequence => Game.Random.Float( 0, 1 ) switch
	{
		<= 0.3f => "idle3",
		<= 0.6f => "idle1",
		_ => "idle2"
	};
	internal override string ShotSequence => !_rapidFire && Clip1 == 0 ? "shoot_empty" : "shoot";
	internal override string ReloadSequence => Clip1 > 0 ? "reload_noshot" : "reload";

	internal override Vector3 WorldMuzzleOffset( bool hd ) => hd ? new( 12.25f, 1.3f, 5.1f ) : new( 9.8f, 1.1f, 3.8f );

	internal override void Configure()
	{
		HoldType = WeaponHolds.Pistol;
		Handedness = WeaponHandedness.Right;
		ClipMaxSize = 17;
		PrimaryDelay = 0.3f;
		SecondaryDelay = 0.2f;
		SecondaryAutomatic = true;
		ReloadTime = 1.5f;
		CanCancelReload = false;
		AttackDamageTags = ["bullet"];
		Ballistics = new() { Damage = 12, Range = 8192, Radius = 0 };
	}

	internal override Vector2 PelletSpread => Vector2.One * (_rapidFire ? 0.1f : 0.01f);
	public override bool HasSecondaryAmmo() => HasPrimaryAmmo();
	public override void PrimaryAttack() => FireRound( false );
	public override void SecondaryAttack() => FireRound( true );

	void FireRound( bool rapid )
	{
		_rapidFire = rapid;
		Ballistics = Ballistics with { Damage = Deathmatch ? 12 : 8 };
		base.PrimaryAttack();
		SetNextFire( rapid ? SecondaryDelay : PrimaryDelay );
	}

	public override void DryFire()
	{
		if ( AutoReload && CanReload() ) Reload();
		else
		{
			PlaySample( "weapons/357_cock1.wav", 0.8f );
			SetNextFire( 0.2f );
		}
	}

	internal override void DrawShotTrail( Vector3 start, Vector3 end ) { }

	protected override void OnShootEffects( ShotEffect shot )
	{
		base.OnShootEffects( shot );
		if ( shot.NoEvents ) return;
		if ( !IsProxy && Owner is { ThirdPerson: false } )
			MeleeViewPunch.Apply( Scene.Camera, new Angles( -2, 0, 0 ) );
		EjectShells( 1, false );
		PlaySample( "weapons/pl_gun3.wav", Game.Random.Float( 0.92f, 1 ), Game.Random.Float( 0.98f, 1.01f ) );
	}
}
