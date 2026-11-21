namespace GoldSrc.HalfLife;

[Title( "9mm Handgun" ), Category( "Half-Life" )]
[WeaponPickup( "9mmhandgun", "9mm", 250 )]
public sealed class Pistol : StudioWeapon
{
	bool _rapidFire;

	internal override string ShotSequence => Clip1 == 0 ? "shoot_empty" : "shoot";
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
		AttackDamageTags = ["bullet", "nevergib"];
		Ballistics = new() { Damage = 8, Range = 8192, Radius = 0 };
	}

	public override Vector2 CurrentSpread => Vector2.One * System.MathF.Atan( _rapidFire ? 0.1f : 0.01f ).RadianToDegree();
	public override bool HasSecondaryAmmo() => HasPrimaryAmmo();
	public override void PrimaryAttack() => FireRound( false );
	public override void SecondaryAttack() => FireRound( true );

	void FireRound( bool rapid )
	{
		_rapidFire = rapid;
		base.PrimaryAttack();
		SetNextFire( rapid ? SecondaryDelay : PrimaryDelay );
	}

	protected override void OnShootEffects( ShotEffect shot )
	{
		base.OnShootEffects( shot );
		if ( shot.NoEvents ) return;
		EjectShells( 1, false );
		PlaySample( "weapons/pl_gun3.wav", Game.Random.Float( 0.92f, 1 ), Game.Random.Float( 0.98f, 1.01f ) );
	}
}
