namespace Source1.Dods;

public sealed class HandGrenade : Weapon
{
	[Property] public string ThrownModel { get; set; }
	[Property] public Projectile.Kinds Kind { get; set; }
	[Property] public float FuseTime { get; set; } = 5;
	[Property] public string LivePrefab { get; set; }
	[Property] public bool TumbleEndOverEnd { get; set; }
	[Property] public bool Live { get; set; }
	[Sync( SyncFlags.FromHost )] internal float DetonateAt { get; set; }
	TimeUntil? _dropAt;

	public override bool CanPrimaryAttack() => _dropAt is null && base.CanPrimaryAttack();
	public override bool CanSecondaryAttack() => false;
	internal override bool UseEmptyAttack => Live;
	internal override bool UseEmptyIdle => Live || _dropAt.HasValue;

	protected override void OnEquipped()
	{
		base.OnEquipped();
		if ( Networking.IsHost && Live && DetonateAt == 0 ) DetonateAt = Time.Now + FuseTime;
		_ = WeaponSetup.ConfigureWeapon( this );
	}

	public override void PrimaryAttack()
	{
		if ( BlockSubmergedShot( true ) ) return;
		_dropAt = Live ? DetonateAt - Time.Now : FuseTime;
		PullPin();
	}

	[Rpc.Broadcast( NetFlags.OwnerOnly )]
	void PullPin() => WeaponModel?.OnAttack();

	protected override void Think()
	{
		base.Think();
		if ( _dropAt is not { } timer ) return;
		if ( timer > 0 && (Input.Down( "attack1" ) || Input.Down( "attack2" )) ) return;
		_dropAt = null;
		SetNextFire( DeployTime );
		Throw( AimRay, timer );
	}

	protected override bool OnHolstering( BaseInventoryItem next ) => !Live && _dropAt is null && base.OnHolstering( next );

	protected override void OnHolstered()
	{
		_dropAt = null;
		if ( Networking.IsHost && Live && DetonateAt > 0 )
		{
			Launch( AimRay, DetonateAt - Time.Now, true );
			DetonateAt = 0;
			TakePrimaryAmmo();
			_ = RemoveAfterHolster();
		}
		base.OnHolstered();
	}

	[Rpc.Host( NetFlags.OwnerOnly )]
	void Throw( Ray aim, float remaining ) => Release( aim, remaining );

	void Release( Ray aim, float remaining )
	{
		if ( !IsActive || !TakePrimaryAmmo() ) return;
		if ( Live )
		{
			remaining = DetonateAt - Time.Now;
			DetonateAt = 0;
		}
		Launch( aim, remaining, remaining <= 0 );
		ReleaseEffects();
		if ( !HasPrimaryAmmo() && Inventory.IsValid() ) Inventory.Remove( this );
	}

	void Launch( Ray aim, float remaining, bool drop )
	{
		var angles = Rotation.LookAt( aim.Forward ).Angles();
		var pitch = angles.pitch;
		angles.pitch = pitch - 10 + System.MathF.Abs( pitch ) / 9;
		var forward = angles.Forward;
		var facing = angles.WithPitch( 0 ).Forward;
		var inherited = Owner.IsValid() ? facing * Owner.Velocity.Dot( facing ) : Vector3.Zero;
		var velocity = drop ? forward : inherited + forward * ((90 - angles.pitch) * 4.2f).Clamp( 500, 1400 );
		var trace = Scene.Trace.Ray( aim.Position, aim.Position + forward * 16 ).IgnoreGameObjectHierarchy( AimIgnoreRoot ).Run();
		var rotation = TumbleEndOverEnd ? new Angles( 45, angles.yaw, 0 ).ToRotation() : Rotation.Identity;
		Vector3? spin = TumbleEndOverEnd ? rotation.Left * 600f.DegreeToRadian() : null;
		var grenade = Projectile.Launch( this, Attacker, Kind, trace.EndPosition, velocity, 0, 0, ThrownModel, rotation, spin );
		if ( grenade is FragGrenade frag )
		{
			frag.Fuse = remaining.Clamp( 0, FuseTime );
			frag.LivePrefab = LivePrefab;
		}
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		if ( !Networking.IsHost || !Live || DetonateAt <= 0 ) return;
		if ( IsActive && DetonateAt <= Time.Now ) Release( AimRay, 0 );
	}

	async Task RemoveAfterHolster()
	{
		// Let the inventory finish its drop or switch before removing the spent item.
		await GameTask.Yield();
		if ( !IsValid ) return;
		if ( Inventory.IsValid() ) Inventory.Remove( this );
		else GameObject.Destroy();
	}

	[Rpc.Broadcast( NetFlags.HostOnly )]
	void ReleaseEffects()
	{
		if ( Sounds.Sound( "Weapon_Grenade.Throw", MountContent.OfWeapon( this ) ) is { } sound )
			GameObject.PlaySound( sound );
		PostWeaponEvent( WeaponEvents.Attack );
		WeaponModel?.OnDeploy();
	}
}
