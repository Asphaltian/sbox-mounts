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

	protected override void OnShootImpact( in ShotEffect shot )
	{
		if ( !shot.Hit ) return;
		if ( HitFlesh( shot ) ) PlaySample( "weapons/cbar_hitbod" );
		else
		{
			PlaySample( "weapons/cbar_hit", pitch: Game.Random.Int( 98, 101 ) / 100f );
			ImpactDecal.Place( shot.HitPosition, shot.Normal, $"shot{Game.Random.Int( 1, 5 )}", 10 );
		}
	}

	public override Ray AimRay => Holder is null ? base.AimRay : Holder.CalculateAimRay( this );
	internal override string ShotSound => "weapons/cbar_miss1.wav";
}
