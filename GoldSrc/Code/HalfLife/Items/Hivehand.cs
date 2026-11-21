namespace GoldSrc.HalfLife;

[Title( "Hivehand" ), Category( "Half-Life" )]
[WeaponPickup( "hgun", "hornets", 8, ClassName = "hornetgun" )]
public sealed class Hivehand : ProjectileWeapon
{
	internal override string DeploySequence => "up";
	internal override string ShotSequence => "Shoot";
	internal override string ShotSound => "agrunt/ag_fire1.wav";
	internal override Vector3 WorldMuzzleOffset( bool hd ) => hd ? new( 19.1f, 0.7f, -2.2f ) : new( 19.3f, 0.9f, -0.6f );

	internal override void Configure()
	{
		HoldType = WeaponHolds.Rifle;
		PrimaryDelay = 0.25f;
		UsesClips = false;
		StartingAmmo = 8;
	}

	internal override bool Release( Ray ray )
	{
		NpcProjectile.Fire<Hornet>( Owner.GameObject, "mount://hl1/valve/models/hornet.mdl.vmdl", 7, null, ray.Position, ray.Forward, 300 );
		return true;
	}
	TimeUntil _regenerate;
	protected override void OnUpdate()
	{
		base.OnUpdate();
		if ( !Networking.IsHost || !Inventory.IsValid() || _regenerate > 0 ) return;
		_regenerate = 0.5f;
		Inventory.GiveAmmo( PrimaryAmmoType, 1 );
	}
}
