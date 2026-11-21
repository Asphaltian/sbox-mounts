namespace GoldSrc.HalfLife;

[Title( "Crossbow" ), Category( "Half-Life" )]
[WeaponPickup( "crossbow", "bolts", 50 )]
public sealed class Crossbow : ProjectileWeapon
{
	internal override string DeploySequence => "draw1";
	internal override string ShotSequence => "fire1";
	internal override string ShotSound => "weapons/xbow_fire1.wav";
	internal override Vector3 WorldMuzzleOffset( bool hd ) => hd ? new( 22.4f, 1.5f, 2.9f ) : new( 19.3f, 0.8f, 2.6f );

	internal override void Configure()
	{
		HoldType = WeaponHolds.Rifle;
		PrimaryDelay = 0.75f;
		ClipMaxSize = 5;
	}

	internal override bool Release( Ray ray )
	{
		NpcProjectile.Fire<CrossbowBolt>( Owner.GameObject, "mount://hl1/valve/models/crossbow_bolt.mdl.vmdl", 50, null, ray.Position, ray.Forward, 2000 );
		return true;
	}
}
