namespace GoldSrc.HalfLife;

[Title( ".357 Magnum" ), Category( "Half-Life" )]
[WeaponPickup( "357", "357", 36 )]
public sealed class Revolver : StudioWeapon
{
	internal override string ShotSequence => "fire1";
	internal override void PlayShotSound() => PlaySample( $"weapons/357_shot{Game.Random.Int( 1, 2 )}.wav", Game.Random.Float( 0.8f, 0.9f ) );
	internal override Vector3 WorldMuzzleOffset( bool hd ) => hd ? new( 14.7f, 2, 4.2f ) : new( 13.8f, 1.6f, 3.3f );

	internal override void Configure()
	{
		HoldType = WeaponHolds.Pistol;
		PrimaryDelay = 0.75f;
		ClipMaxSize = 6;
		AttackDamageTags = ["bullet", "nevergib"];
		Ballistics = new() { Damage = 40, Range = 8192 };
	}
}
