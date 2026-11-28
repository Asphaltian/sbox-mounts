using System;

namespace Source1.Css;

public partial class Firearm
{
	public readonly record struct Kick( float Pitch, float Yaw, float PitchStep, float YawStep, float MaxPitch, float MaxYaw, int DirectionChange );
	public sealed record KickbackSettings( Kick Standing, Kick Crouching, Kick Moving, Kick Airborne, bool AirborneFirst = false );

	[Property] public float ShotPunch { get; set; }
	[Property] public Vector2 SniperPunch { get; set; }
	[Property] public KickbackSettings Kickback { get; set; }

	Vector2 _punch;
	protected void AddPitchPunch( float degrees ) => _punch.x -= degrees;
	bool _kickRight;
	int _shots;
	bool _firing;
	TimeUntil _recoverShots;

	protected override bool KickView( Angles? punchOverride = null )
	{
		if ( (Kickback is null && ShotPunch == 0 && SniperPunch == Vector2.Zero) || punchOverride.HasValue ) return base.KickView( punchOverride );
		if ( !Owner.IsValid() || Owner.IsProxy ) return false;

		if ( SniperPunch != Vector2.Zero )
		{
			_punch.x = _punch.x * 0.75f - Random.Shared.Float( SniperPunch.x, SniperPunch.y );
			_punch.y += Random.Shared.Float( -0.75f, 0.75f );
			return true;
		}

		if ( Kickback is null )
		{
			_punch.x -= ShotPunch;
			return true;
		}

		var kick = (Owner.IsOnGround || !Kickback.AirborneFirst) && Owner.Velocity.WithZ( 0 ).LengthSquared > 25 ? Kickback.Moving
			: !Owner.IsOnGround ? Kickback.Airborne
			: Owner.IsDucking ? Kickback.Crouching : Kickback.Standing;
		_shots++;
		_firing = true;
		var step = _shots == 1 ? 0 : _shots;
		_punch.x = Math.Max( -kick.MaxPitch, _punch.x - kick.Pitch - step * kick.PitchStep );
		var yaw = kick.Yaw + step * kick.YawStep;
		_punch.y = Math.Clamp( _punch.y + (_kickRight ? yaw : -yaw), -kick.MaxYaw, kick.MaxYaw );
		if ( Random.Shared.Next( kick.DirectionChange + 1 ) == 0 ) _kickRight = !_kickRight;
		return true;
	}

	void UpdateKickback()
	{
		var length = _punch.Length;
		if ( length > 0 )
			_punch *= Math.Max( 0, length - (10 + length * 0.5f) * Time.Delta ) / length;

		if ( _shots == 0 || !IsActive || Input.Down( "attack1" ) || Input.Down( "attack2" ) ) return;
		if ( _firing )
		{
			_firing = false;
			_shots = Clip1 == 0 ? 0 : Math.Min( _shots, 15 );
			_recoverShots = 0.4f;
		}
		else if ( _recoverShots <= 0 )
		{
			_shots--;
			_recoverShots = 0.0225f;
		}
	}

	protected override void OnHolstered()
	{
		ReleaseGrip();
		base.OnHolstered();
		_shots = 0;
		_firing = false;
	}

	protected override void OnReloadStarted()
	{
		base.OnReloadStarted();
		_shots = 0;
	}

	public override Ray AimRay
	{
		get
		{
			var ray = base.AimRay;
			if ( _punch == Vector2.Zero ) return ray;
			var angles = Rotation.LookAt( ray.Forward ).Angles();
			return ray with { Forward = (angles + new Angles( 2 * _punch.x, 2 * _punch.y, 0 )).Forward };
		}
	}

	protected override void ModifyCamera( CameraComponent camera, ref CameraView view )
	{
		if ( _punch != Vector2.Zero && !Owner.ThirdPerson )
			view.Rotation = Rotation.From( view.Rotation.Angles() + new Angles( _punch.x, _punch.y, 0 ) );
		var baseFov = view.FieldOfView;
		base.ModifyCamera( camera, ref view );
		if ( _zoomChanged < _zoomDuration ) view.FieldOfView = ZoomedFov( baseFov, ZoomFov );
	}
}
