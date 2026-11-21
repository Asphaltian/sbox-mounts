namespace GoldSrc.HalfLife;

[Title( "Hand Grenade" ), Category( "Half-Life" )]
[WeaponPickup( "grenade", "grenades", 10, ClassName = "handgrenade" )]
public sealed class HandGrenade : ProjectileWeapon
{
	internal override string IdleSequence => "idle";
	internal override string ShotSequence => "throw1";

	internal override void Configure()
	{
		HoldType = WeaponHolds.Throwable;
		PrimaryDelay = 1f;
		UsesClips = false;
		StartingAmmo = 5;
	}

	internal override bool Release( Ray ray )
	{
		TimedGrenade.Throw( Owner.GameObject, ray.Position, ray.Forward * 600 + Owner.Velocity, 3 );
		return true;
	}
}
