namespace GoldSrc.HalfLife;

[Title( "Gluon Gun" ), Category( "Half-Life" )]
[WeaponPickup( "egon", "uranium", 100 )]
public sealed partial class GluonGun : StudioWeapon
{
	internal override string ShotSequence => "fire1";
	SoundHandle _beamSound;
	bool _firing, _running;
	TimeUntil _nextPulseDeadline, _windupEnds;
	RealTimeUntil _shutdownEnds;
	internal override Vector3 WorldMuzzleOffset( bool hd ) => hd ? new( 29, -5.2f, 0.5f ) : new( 28.4f, -5.1f, 0 );

	protected override void Think()
	{
		base.Think();
		if ( _firing && Input.Released( "attack1" ) ) EndAttack( 0.5f );
	}

	public override void PrimaryAttack()
	{
		if ( Owner.IsValid() && WaterBody.Find( Scene, Owner.EyePosition ).IsValid() )
		{
			if ( _firing ) EndAttack( 0.5f );
			else DryFire();
			return;
		}

		base.PrimaryAttack();
		if ( IsHeld && !HasPrimaryAmmo() ) EndAttack( 1 );
	}

	public override void DryFire()
	{
		PlaySample( "weapons/357_cock1.wav", 0.8f );
		SetNextFire( 0.25f );
	}

	void EndAttack( float delay )
	{
		SetNextFire( delay );
		EndBeamEffects();
	}

	[Rpc.Broadcast( NetFlags.OwnerOnly )]
	void EndBeamEffects() => StopBeam();

	protected override void OnShootEffects( ShotEffect shot )
	{
		base.OnShootEffects( shot );
		if ( shot.NoEvents || Application.IsDedicatedServer ) return;
		_nextPulseDeadline = PrimaryDelay + 0.15f;
		if ( _firing ) return;
		_firing = true;
		_windupEnds = 2;
		_beamSound?.Stop();
		_beamSound = PlaySample( "weapons/egon_windup2.wav", 0.98f, 1.25f );
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		if ( !_firing )
		{
			if ( _beamSound is not null && _shutdownEnds <= 0 )
			{
				_beamSound.Stop();
				_beamSound = null;
			}
			return;
		}
		if ( _nextPulseDeadline <= 0 ) { StopBeam(); return; }
		if ( !_running && _windupEnds <= 0 )
		{
			_beamSound?.Stop();
			_running = true;
			_beamSound = PlaySample( "weapons/egon_run3.wav", 0.98f, 1.25f );
		}
		if ( _beamSound.IsValid() ) _beamSound.Position = GetMuzzleTransform().Position;
	}

	protected override void OnHolstered()
	{
		StopBeam();
		base.OnHolstered();
	}

	protected override void OnDisabled()
	{
		_firing = _running = false;
		_beamSound?.Stop();
		_beamSound = null;
		base.OnDisabled();
	}

	void StopBeam()
	{
		if ( !_firing ) return;
		_firing = _running = false;
		_beamSound?.Stop();
		_beamSound = PlaySample( "weapons/egon_off1.wav", 0.98f );
		// The shutdown WAV lasts 3.284 seconds. Retire the channel even if audio playback stalls.
		_shutdownEnds = 3.3f;
	}

	internal override void DrawShotTrail( Vector3 start, Vector3 end ) =>
		EnergyBeam.Show( start, end, new Color( 0.4f, 0.55f, 1 ), "xbeam1", "Gluon beam", 3, PrimaryDelay );

	protected override void OnShootImpact( in ShotEffect shot )
	{
		if ( shot.Hit && shot.HitObject.IsValid() && shot.HitObject.GetComponentInParent<Component.IDamageable>() is not null )
			Blast.SpriteEffect( "Gluon impact", "xspark1", shot.HitPosition, 1, 16 );
	}

	internal override void Configure()
	{
		HoldType = WeaponHolds.Rifle;
		PrimaryDelay = 0.1f;
		UsesClips = false;
		StartingAmmo = 20;
		Ballistics = new() { Damage = 14, Range = 2048 };
		AttackDamageTags = ["energybeam"];
	}
}
