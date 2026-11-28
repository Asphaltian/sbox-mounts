namespace Source1.Css;

public sealed class ShotgunWeapon : Firearm
{
	const float ShellTime = 0.5f;
	TimeUntil? _firstInsert;
	TimeSince _reloadAge;
	bool _insertRequested;

	[Property] public int MinimumPitchPunch { get; set; } = 4;
	protected override float UnderwaterRefireDelay => 0.15f;

	protected override bool KickView( Angles? punchOverride = null )
	{
		if ( !Owner.IsValid() || Owner.IsProxy ) return false;
		var minimum = MinimumPitchPunch;
		AddPitchPunch( Owner.IsOnGround ? Game.Random.Int( minimum, minimum + 2 ) : Game.Random.Int( minimum + 4, minimum + 7 ) );
		return true;
	}

	public override bool CanReload() => NextPrimaryFire <= 0 && base.CanReload();

	protected override bool WantsReload() => base.WantsReload()
		|| (AutoReload && Clip1 == 0 && NextPrimaryFire <= 0 && !Input.Down( "attack2" ));

	protected override bool WantsPrimaryAttack() => (!IsReloading || NextPrimaryFire <= 0) && base.WantsPrimaryAttack();

	protected override void Think()
	{
		base.Think();
		if ( !IsReloading || Clip1 != 0 || _insertRequested || NextPrimaryFire > 0 || !Input.Down( "attack1" ) ) return;
		if ( _reloadAge <= ShellTime + Time.Delta )
		{
			PlayEmptySound();
			SetNextPrimaryFire( 0.2f );
			return;
		}
		_insertRequested = true;
		FinishEmptyInsert();
	}

	[Rpc.Host( NetFlags.OwnerOnly )]
	void FinishEmptyInsert()
	{
		if ( !IsActive || !IsReloading || Clip1 != 0 || _reloadAge < ShellTime || Ammo1 <= 0 ) return;
		CancelReload();
		Clip1 += TakeReserveAmmo( PrimaryAmmoType, 1 );
	}

	protected override void OnReloadStarted()
	{
		if ( !NpcControlled )
		{
			_reloadAge = 0;
			_insertRequested = false;
			ReloadTime = ShellTime;
			ReloadStartTime = ShellTime * 2;
			SetNextFire( ShellTime );
			_firstInsert = (TimeUntil)ShellTime;
		}
		base.OnReloadStarted();
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();
		if ( _firstInsert is not { } delay || delay > 0 ) return;
		_firstInsert = null;
		if ( IsReloading ) WeaponModel?.OnIncrementalReload();
	}

	protected override void OnReloadInserted()
	{
		if ( Clip1 < PrimaryClipSize && Ammo1 > 0 ) base.OnReloadInserted();
	}
}
