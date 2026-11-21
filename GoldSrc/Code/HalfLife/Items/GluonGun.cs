namespace GoldSrc.HalfLife;

[Title( "Gluon Gun" ), Category( "Half-Life" )]
[WeaponPickup( "egon", "uranium", 100 )]
public sealed class GluonGun : StudioWeapon
{
	internal override string ShotSequence => "fire1";
	internal override string ShotSound => "weapons/egon_run3.wav";
	internal override Vector3 WorldMuzzleOffset( bool hd ) => new( 25, 2, 4 );

	internal override void DrawShotTrail( Vector3 start, Vector3 end ) =>
		EnergyBeam.Show( start, end, new Color( 0.4f, 0.55f, 1 ), "xbeam1", "Gluon beam", 3, PrimaryDelay );

	internal override void Configure()
	{
		HoldType = WeaponHolds.Rifle;
		PrimaryDelay = 0.1f;
		UsesClips = false;
		StartingAmmo = 20;
		Ballistics = new() { Damage = 14, Range = 8192 };
		AttackDamageTags = ["energybeam"];
	}
}
