namespace GoldSrc.HalfLife;

[Title( "9mm Assault Rifle" ), Category( "Half-Life" )]
[WeaponPickup( "9mmar", "9mm", 250 )]
public sealed class Rifle : BulletWeapon
{
	internal const string GrenadeAmmoPath = "ammo/argrenades.ammo";
	TimeUntil _nextLaunch;

	internal override string DeploySequence => "deploy";
	int FreshRounds => Deathmatch ? 50 : 25;

	internal override Vector3 WorldMuzzleOffset( bool hd ) => hd ? new( 30, 2.7f, 5.4f ) : new( 17.8f, 1.65f, 4.9f );

	internal override void Configure()
	{
		HoldType = WeaponHolds.Smg;
		ClipMaxSize = 50;
		PrimaryDelay = 0.1f;
		SecondaryDelay = 1;
		SecondaryAutomatic = true;
		SecondaryAmmoType = GameResource.Load<BaseAmmoResource>( $"mount://hl1/{GrenadeAmmoPath}" );
		ReloadTime = 1.5f;
		AttackDamageTags = ["bullet"];
		Ballistics = new() { Damage = 5, Range = 8192, Radius = 0 };
	}

	protected override bool OnAdding( BaseInventoryComponent inventory )
	{
		if ( Clip1 < 0 ) Clip1 = FreshRounds;
		return base.OnAdding( inventory );
	}

	internal override Vector2 PelletSpread => Vector2.One * (Deathmatch ? 0.05234f : 0.02618f);

	public override void PrimaryAttack()
	{
		if ( Underwater() ) return;
		Ballistics = Ballistics with { Damage = Deathmatch ? 12 : 5 };
		base.PrimaryAttack();
	}

	bool Underwater()
	{
		if ( !Owner.IsValid() || !WaterBody.Find( Scene, Owner.EyePosition ).IsValid() ) return false;
		PlaySample( "weapons/357_cock1.wav", 0.8f );
		SetNextFire( 0.15f );
		return true;
	}

	protected override void OnShootEffects( ShotEffect shot )
	{
		base.OnShootEffects( shot );
		if ( shot.NoEvents ) return;
		EjectShells( 1, false );
		PlaySample( Game.Random.Int( 0, 1 ) == 0 ? "weapons/hks1.wav" : "weapons/hks2.wav", 1, Game.Random.Float( 0.94f, 1.09f ) );
	}

	public override void SecondaryAttack()
	{
		if ( Underwater() ) return;
		SetNextFire( SecondaryDelay );
		LaunchGrenade();
	}

	[Rpc.Host( NetFlags.OwnerOnly )]
	void LaunchGrenade()
	{
		if ( !IsActive || !Owner.IsValid() || _nextLaunch > 0 || WaterBody.Find( Scene, Owner.EyePosition ).IsValid() ) return;
		if ( !TakeSecondaryAmmo() ) return;
		_nextLaunch = SecondaryDelay;
		var aim = AimRay;
		NpcProjectile.Fire<ContactGrenade>( Owner.GameObject, "mount://hl1/valve/models/grenade.mdl.vmdl", 100,
			null, aim.Position, aim.Forward, 800 );
		LaunchEffects();
	}

	[Rpc.Broadcast( NetFlags.HostOnly )]
	void LaunchEffects()
	{
		ViewModel?.GetComponent<StudioWeaponView>()?.Play( "grenade" );
		PlaySample( Game.Random.Int( 0, 1 ) == 0 ? "weapons/glauncher.wav" : "weapons/glauncher2.wav", 1, Game.Random.Float( 0.94f, 1.09f ) );
		PostWeaponEvent( WeaponEvents.Attack );
	}
}
