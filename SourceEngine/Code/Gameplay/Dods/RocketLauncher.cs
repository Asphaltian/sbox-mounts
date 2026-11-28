namespace Source1.Dods;

public sealed class RocketLauncher : Firearm
{
	[Property] public string RocketModel { get; set; }
	[Sync, Change( nameof( OnShouldered ) )] public bool Shouldered { get; private set; }
	protected override float LookScale => Shouldered ? 0.6f : 1;
	internal override bool UsesDeployedClips => Shouldered;

	public override bool HasSecondaryAmmo() => true;
	public override bool CanSecondaryAttack() => !IsDeploying && !IsReloading && NextSecondaryFire <= 0;
	public override bool CanPrimaryAttack() => Shouldered && base.CanPrimaryAttack();

	public override void SecondaryAttack()
	{
		Shouldered = !Shouldered && !BlockSubmergedShot( true, false );
	}

	float Duration( string activity )
	{
		var model = Setup.ViewModel;
		return model.IsValid() ? WeaponClips.Duration( model, ModelSequences.Of( model ).FindActivity( activity ) ) : 0;
	}

	void OnShouldered( bool before, bool after )
	{
		var reload = Duration( after ? "ACT_VM_RELOAD_DEPLOYED" : "ACT_VM_RELOAD" );
		if ( reload > 0 ) ReloadTime = reload;
		SetNextFire( Duration( after ? "ACT_VM_DEPLOY" : "ACT_VM_UNDEPLOY" ) );
		(WeaponModel as ViewModel)?.OnDeployChanged( after );
	}

	public override void PrimaryAttack()
	{
		if ( !Shouldered || BlockSubmergedShot( true ) || !TakePrimaryAmmo() ) return;
		Launch( AimRay );
		ShootEffects( new ShotEffect() );
		TimeSinceShoot = 0;
		KickView();
		if ( Ammo1 == 0 ) Shouldered = false;
		SetNextPrimaryFire( Duration( "ACT_VM_PRIMARYATTACK" ) + 0.5f );
	}

	[Rpc.Host( NetFlags.OwnerOnly )]
	void Launch( Ray ray )
	{
		Projectile.Spawn<Rocket>( Attacker, MountContent.OfWeapon( this ), ray.Position, ray.Forward * 1300,
			150, 200, RocketModel, "rockettrail", this );
	}

	protected override void OnHolstered()
	{
		Shouldered = false;
		base.OnHolstered();
	}
}

public sealed class Rocket : Projectile
{
	protected override float Gravity => 80;
	protected internal override BlastMode Blast => BlastMode.Traced;

	protected override void OnStart()
	{
		base.OnStart();
		if ( !Application.IsDedicatedServer && Sounds.Sound( "Weapon_Bazooka.Shoot", MountName ) is { } sound )
			GameObject.PlaySound( sound );
	}

	protected override void Hit( SceneTraceResult tr )
	{
		if ( tr.HasTag( "source1_sky" ) )
		{
			GameObject.Destroy();
			return;
		}
		if ( tr.GameObject.GetComponentInParent<Prop>() is { Health: > 0 } prop )
		{
			BaseCombatWeapon.ShootBullet( tr, 50, 0, Attacker, GameObject, [DamageTags.Melee], hitboxTags: false );
			if ( !prop.IsValid() || prop.Health <= 0 ) return;
		}
		if ( !tr.GameObject.GetComponentInParent<PlayerController>().IsValid() )
			Scorch( tr.GameObject, WeaponEffects.DecalTransform( tr.GameObject, tr.EndPosition, tr.Normal ), MountName, large: true );
		WorldPosition += tr.Normal * 0.6f;
		Stun.Burst( Scene, WorldPosition, 150, 0.75f );
		Explode( particle: "explosioncore_floor", rotation: Rotation.LookAt( tr.Normal ) );
	}
}
