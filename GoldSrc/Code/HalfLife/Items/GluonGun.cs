namespace GoldSrc.HalfLife;

[Title( "Gluon Gun" ), Category( "Half-Life" )]
[WeaponPickup( "egon", "uranium", 100 )]
public sealed partial class GluonGun : StudioWeapon
{
	internal override string ShotSequence => $"fire{Game.Random.Int( 1, 4 )}";
	internal override string IdleSequence => Game.Random.Int( 0, 1 ) == 0 ? "idle1" : "fidget1";
	SoundHandle _beamSound;
	GluonBeam _beam;
	bool _firing, _running;
	TimeUntil _nextPulseDeadline, _windupEnds;
	TimeUntil _burstExpires, _nextCell, _nextShake;
	RealTimeUntil _shutdownEnds;
	internal override Vector3 WorldMuzzleOffset( bool hd ) => hd ? new( 29, -5.2f, 0.5f ) : new( 28.4f, -5.1f, 0 );

	protected override void Think()
	{
		base.Think();
		if ( _burstExpires > 0 && Input.Released( "attack1" ) ) EndAttack( 0.5f );
	}

	public override void PrimaryAttack()
	{
		if ( Owner.IsValid() && WaterBody.Find( Scene, Owner.EyePosition ).IsValid() )
		{
			if ( _burstExpires > 0 ) EndAttack( 0.5f );
			else DryFire();
			return;
		}

		var start = _burstExpires <= 0;
		_burstExpires = PrimaryDelay + 0.15f;
		var hit = BulletTrace( AimRay, Ballistics.Range, 0 ).Run();
		var multiplayer = Networking.MaxPlayers > 1;
		if ( start )
		{
			_nextCell = 0;
		}
		else if ( !hit.StartedSolid )
		{
			var damage = multiplayer ? 20 : 14;
			if ( hit.Hit )
			{
				ShootBullet( hit, damage, 0 );
				// Keep the blast's visibility traces outside the surface, including sloped ceilings.
				ImpactPulse( hit.EndPosition + hit.Normal * 2 );
			}
			if ( IsHeld && _nextCell <= 0 )
			{
				TakePrimaryAmmo();
				_nextCell = multiplayer ? 0.2f : 0.1f;
			}
		}
		ShootEffects( new ShotEffect( hit.EndPosition, hit.Hit, hit.Normal, hit.GameObject, hit.Surface, NoEvents: !start ) );
		if ( IsHeld && !HasPrimaryAmmo() ) EndAttack( 1 );
	}

	[Rpc.Host( NetFlags.OwnerOnly )]
	void ImpactPulse( Vector3 point )
	{
		if ( !Owner.IsValid() || !IsActive ) return;
		if ( Networking.MaxPlayers > 1 )
			Blast.DamageInRadius( GameObject, Owner.GameObject, point, 5, 128, "energybeam", "blast", "alwaysgib" );
		if ( _nextShake > 0 ) return;
		GroundShake.Emit( Scene, point, 5, 150, 0.75f, 250 );
		_nextShake = 1.5f;
	}

	public override void DryFire()
	{
		PlaySample( "weapons/357_cock1.wav", 0.8f );
		SetNextFire( 0.25f );
	}

	void EndAttack( float delay )
	{
		_burstExpires = 0;
		SetNextFire( delay );
		EndBeamEffects();
	}

	[Rpc.Broadcast( NetFlags.OwnerOnly )]
	void EndBeamEffects() => StopBeam();

	protected override void OnShootEffects( ShotEffect shot )
	{
		base.OnShootEffects( shot );
		if ( Application.IsDedicatedServer ) return;
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
			ViewModel?.GetComponent<StudioWeaponView>()?.Play( ShotSequence );
		}
		if ( _beamSound.IsValid() ) _beamSound.Position = GetMuzzleTransform().Position;
	}

	protected override void OnHolstered()
	{
		_burstExpires = 0;
		StopBeam();
		base.OnHolstered();
	}

	protected override void OnDisabled()
	{
		StopBeamVisual();
		_burstExpires = 0;
		_firing = _running = false;
		_beamSound?.Stop();
		_beamSound = null;
		base.OnDisabled();
	}

	void StopBeam()
	{
		StopBeamVisual();
		if ( !_firing ) return;
		_firing = _running = false;
		_beamSound?.Stop();
		_beamSound = PlaySample( "weapons/egon_off1.wav", 0.98f );
		// The shutdown WAV lasts 3.284 seconds. Retire the channel even if audio playback stalls.
		_shutdownEnds = 3.3f;
	}

	void StopBeamVisual()
	{
		if ( _beam.IsValid() ) _beam.Stop();
		_beam = null;
	}

	internal override void DrawShotTrail( Vector3 start, Vector3 end )
	{
		if ( !_beam.IsValid() ) _beam = GluonBeam.Start( this, end );
	}

	protected override void OnShootImpact( in ShotEffect shot )
	{
		var showFlare = Networking.MaxPlayers <= 1 || (shot.HitObject.IsValid() && shot.HitObject.GetComponentInParent<Component.IDamageable>() is not null);
		_beam?.Aim( shot.HitPosition, showFlare );
	}

	internal override void Configure()
	{
		HoldType = WeaponHolds.Rifle;
		PrimaryDelay = 0.1f;
		UsesClips = false;
		StartingAmmo = 20;
		Ballistics = new() { Damage = 14, Range = 2048 };
		AttackDamageTags = ["energybeam", "alwaysgib"];
	}
}
