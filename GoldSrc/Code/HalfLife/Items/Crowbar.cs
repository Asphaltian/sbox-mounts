namespace GoldSrc.HalfLife;

[Title( "Crowbar" ), Category( "Half-Life" )]
[WeaponPickup( "crowbar", null, 0 )]
public sealed class Crowbar : StudioWeapon
{
	internal override string ShotSequence => "attack1";

	internal override void Configure()
	{
		HoldType = WeaponHolds.Melee;
		PrimaryDelay = 0.5f;
		UsesClips = false;
		UsesAmmo = false;
		AttackDamageTags = ["club"];
		Ballistics = new() { Damage = 10, Range = 32, Radius = 4 };
	}

	internal override void DrawShotTrail( Vector3 start, Vector3 end ) { }

	public override Ray AimRay => Holder is null ? base.AimRay : Holder.CalculateAimRay( this );
	internal override string ShotSound => "weapons/cbar_miss1.wav";
}
