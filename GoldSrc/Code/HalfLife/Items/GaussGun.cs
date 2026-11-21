namespace GoldSrc.HalfLife;

[Title( "Tau Cannon" ), Category( "Half-Life" )]
[WeaponPickup( "gauss", "uranium", 100 )]
public sealed class GaussGun : StudioWeapon
{
	internal override string ShotSequence => "fire";
	internal override string IdleSequence => "idle";
	internal override string ShotSound => "weapons/gauss2.wav";
	internal override Vector3 WorldMuzzleOffset( bool hd ) => new( 28.4f, 2.8f, hd ? 4.6f : 4.1f );

	internal override void DrawShotTrail( Vector3 start, Vector3 end ) =>
		EnergyBeam.Show( start, end, new Color( 1, 0.5f, 0 ), "smoke", "Gauss beam", 2, 0.1f );

	internal override void Configure()
	{
		HoldType = WeaponHolds.Rifle;
		PrimaryDelay = 0.2f;
		UsesClips = false;
		StartingAmmo = 20;
		Ballistics = new() { Damage = 20, Range = 8192 };
		AttackDamageTags = ["energybeam"];
	}
}
