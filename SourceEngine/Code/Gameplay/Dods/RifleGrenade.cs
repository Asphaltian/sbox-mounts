namespace Source1.Dods;

public sealed class RifleGrenade : Firearm
{
	[Property] public string GrenadeModel { get; set; }
	[Property] public string LivePrefab { get; set; }

	public override void PrimaryAttack()
	{
		if ( BlockSubmergedShot( true ) || !TakePrimaryAmmo() ) return;
		var ray = AimRay;
		Launch( ray );
		ShootEffects( new ShotEffect() );
		TimeSinceShoot = 0;
		KickView();
	}

	[Rpc.Host( NetFlags.OwnerOnly )]
	void Launch( Ray ray )
	{
		var velocity = ray.Forward * 2000;
		var facing = Rotation.LookAt( -ray.Forward );
		var pitchRate = velocity.z > 0 ? -facing.Pitch() / System.MathF.Sqrt( 2 * velocity.z / 420 ) : -10;
		var shell = Projectile.Spawn<FragGrenade>( Attacker, MountContent.OfWeapon( this ), ray.Position, velocity,
			0, 0, GrenadeModel, null, this, facing, facing.Left * pitchRate.DegreeToRadian() );
		shell.Fuse = 3.5f;
		shell.LivePrefab = LivePrefab;
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();
		if ( Networking.IsHost && IsActive && Inventory.IsValid() && Clip1 == 0 && Ammo1 == 0 && TimeSinceShoot > PrimaryDelay )
			Inventory.Remove( this );
	}
}
