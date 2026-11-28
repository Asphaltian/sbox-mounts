using System;
using Sandbox.Movement;

namespace Source1.Css;

public partial class Firearm : BulletWeapon, PlayerController.IEvents
{
	[Property] public float? AlternateShotDamage { get; set; }
	[Property] public float? AlternateRangeModifier { get; set; }
	[Property] public float ZoomedCycleTime { get; set; }
	[Property] public bool UseLastShotAnimation { get; set; } = true;

	AccuracyProfile Settings => Setup.Data is Accuracy data ? (IsZoomed || BurstMode || Silenced ? data.Alternate ?? data.Primary : data.Primary) : null;
	float _penalty;
	float _fallSpeed;
	Vector2 _shotOffset;
	double _nextShot;

	protected override bool UseSpreadDisk => Settings is not null;

	internal override string WorldMuzzleAttachment => HoldType == WeaponHolds.DualPistols
		? (Clip1 & 1) == 0 ? "muzzle_flash_l" : "muzzle_flash_r"
		: null;

	internal override string SelectAttackClip( WeaponClips clips )
	{
		if ( !BurstMode && !UseLastShotAnimation && Clip1 == 0 ) return clips.Fire;
		if ( !clips.Alternates || clips.LastShotLeft is null ) return null;
		return Clip1 switch
		{
			0 => clips.LastShot,
			1 => clips.LastShotLeft,
			_ => (Clip1 & 1) == 0 ? clips.SecondaryFire : clips.Fire
		};
	}

	float StancePenalty( AccuracyProfile settings ) => Owner.Mode is MoveModeLadder ? settings.Stand + settings.Ladder
		: Owner.IsDucking ? settings.Crouch : settings.Stand;

	float Inaccuracy( AccuracyProfile settings )
	{
		var speed = Owner.Velocity.WithZ( 0 ).Length;
		var maxSpeed = IsZoomed && ZoomedMaxSpeed > 0 ? ZoomedMaxSpeed : settings.MaxSpeed;
		var movement = Math.Clamp( (speed / Math.Max( maxSpeed, 1 ) - 0.34f) / (0.95f - 0.34f), 0, 1 );
		return Math.Max( _penalty, StancePenalty( settings ) ) + settings.Move * movement;
	}

	public override Vector2 CurrentSpread
	{
		get
		{
			if ( Settings is not { } settings || !Owner.IsValid() ) return base.CurrentSpread;
			return SpreadDegrees( Inaccuracy( settings ) + settings.Spread );
		}
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();
		UpdateKickback();
		ResumeZoom();
		if ( !IsActive || !Owner.IsValid() || Settings is not { } settings ) return;
		if ( !Owner.IsOnGround ) _fallSpeed = Math.Max( 0, -Owner.Velocity.z );
		var target = StancePenalty( settings ) + (IsReloading ? settings.Reload : 0);
		var recovery = Owner.Mode is MoveModeLadder ? settings.RecoveryStand
			: !Owner.IsOnGround ? settings.RecoveryCrouch * 3
			: Owner.IsDucking ? settings.RecoveryCrouch : settings.RecoveryStand;
		_penalty = _penalty <= target || recovery <= 0 ? target
			: target + (_penalty - target) * MathF.Pow( 0.1f, Time.Delta / recovery );
	}

	void PlayerController.IEvents.OnJumped()
	{
		if ( IsActive && Settings is { } settings ) _penalty += settings.Jump * Owner.JumpSpeed / JumpVelocity;
	}

	void PlayerController.IEvents.OnLanded( float distance, Vector3 impactVelocity )
	{
		if ( IsActive && Settings is { } settings )
		{
			var speed = impactVelocity.z < 0 ? -impactVelocity.z : _fallSpeed;
			_penalty += settings.Land * speed / JumpVelocity;
		}
		_fallSpeed = 0;
	}

	static readonly float JumpVelocity = MathF.Sqrt( 2 * 800 * 57 );

	protected override void OnEquipped()
	{
		_penalty = 0;
		_fallSpeed = 0;
		_nextShot = 0;
		base.OnEquipped();
		LoadScope();
	}

	public override void PrimaryAttack()
	{
		var clip = Clip1;
		base.PrimaryAttack();
		if ( Clip1 >= clip || !Owner.IsValid() ) return;
		if ( Settings is { } settings ) _penalty += settings.Fire;
		if ( BurstMode ) return;

		var now = Time.NowDouble;
		var late = now - _nextShot;
		if ( Setup.WeaponType == "Pistol" || late < 0 || late > ViewPunch.Tick ) _nextShot = now;
		_nextShot += IsZoomed && ZoomedCycleTime > 0 ? ZoomedCycleTime : PrimaryDelay;
		// Script intervals are floats. Round the gate down so a click on the deadline
		// isn't rejected for the rounding error in e.g. 0.15 seconds.
		SetNextFire( MathF.BitDecrement( (float)(_nextShot - now) ) );
		CycleBolt();
	}

	protected override SceneTraceResult[] TraceVolley( int count, Vector2 spread )
	{
		var alternate = Silenced || BurstMode;
		var modifier = (alternate ? AlternateRangeModifier : null) ?? Setup.Data?.RangeModifier ?? 1;
		var damage = (alternate ? AlternateShotDamage : null) ?? Ballistics.Damage;
		if ( modifier == 1 )
			return ShootBullets( count, spread, Ballistics.Range, Ballistics.Radius, damage, Ballistics.Force );

		var hits = new SceneTraceResult[count];
		var aim = AimRay;
		for ( var i = 0; i < count; i++ )
		{
			var ray = aim with { Forward = aim.Forward.WithAimCone( spread.x, spread.y ) };
			var hit = BulletTrace( ray, Ballistics.Range, Ballistics.Radius ).Run();
			hits[i] = hit;
			if ( hit.Hit )
				ShootBullet( hit, damage * MathF.Pow( modifier, hit.Distance / 500 ), Ballistics.Force );
		}
		return hits;
	}

	protected override Vector2 PrepareVolley()
	{
		_shotOffset = RandomOffset( Inaccuracy( Settings ) );
		return Vector2.One;
	}

	protected override void OnBurstFollowUp()
	{
		if ( Settings is { } settings ) _penalty += settings.Fire;
		_shots++;
	}

	protected override Vector2 SampleSpread() => _shotOffset + RandomOffset( Settings.Spread );

	static Vector2 RandomOffset( float radius )
	{
		var angle = Random.Shared.Float( 0, MathF.Tau );
		return new Vector2( MathF.Cos( angle ), MathF.Sin( angle ) ) * Random.Shared.Float( 0, radius );
	}
}
