using System;

namespace Source1;

/// <summary>First-person movement shared across weapon switches on the same player.</summary>
[Title( "Source Viewmodel Motion" )]
[Category( "Source 1" )]
[Alias( "Source1.SourceViewModelMotion", "SourceViewModelMotion" )]
public sealed class ViewModelMotion : Component
{
	[ConVar( "cl_wpn_sway_interp", Help = "Seconds of view angle history used for mounted weapon sway. Zero disables sway." )]
	public static float SwayDelay { get; set; } = 0.1f;

	[ConVar( "cl_wpn_sway_scale", Help = "Strength of mounted weapon sway when turning." )]
	public static float SwayScale { get; set; } = 1.5f;

	/// <summary>Bob cycle and sway delay are seconds. Bob acceleration limits the change in movement speed per second.</summary>
	public sealed record Settings( float BobCycle = 0.45f, float ForwardBob = 0.1f, float SideBob = 0.8f,
		float UpBob = 0.1f, float BobAcceleration = float.PositiveInfinity, float TurnSway = 1.5f,
		float SwayDelay = 0.1f, bool MirrorSway = true )
	{
		internal static readonly Settings Default = new();
	}

	float _speed;
	double _lastTime = double.NaN;
	double _lastBobTime = double.NaN;
	double _phase;
	readonly Queue<(double Time, Rotation Rotation)> _history = new();
	(double Time, Rotation Rotation) _previous;

	void ApplyBob( ref CameraView eye, ref Angles angles, Vector3 velocity, double now, Settings settings )
	{
		var speed = Math.Min( velocity.WithZ( 0 ).Length, 320 );
		var elapsed = now - _lastBobTime;
		_lastBobTime = now;
		if ( elapsed >= 0 && float.IsFinite( settings.BobAcceleration ) )
		{
			var step = (float)elapsed * settings.BobAcceleration;
			speed = Math.Clamp( speed, _speed - step, _speed + step );
		}
		_speed = speed;
		if ( elapsed > 0 ) _phase = (_phase + elapsed * speed / 320) % (2 * settings.BobCycle);
		float Bob( double cycle ) => speed * 0.005f * (0.3f + 0.7f * (float)Math.Sin( Math.Tau * _phase / cycle ));
		var vertical = Bob( settings.BobCycle );
		var lateral = Bob( 2 * settings.BobCycle );
		eye.Position += eye.Rotation.Forward * (vertical * settings.ForwardBob) + Vector3.Up * (vertical * settings.UpBob)
			+ eye.Rotation.Right * (lateral * settings.SideBob);
		angles += new Angles( -vertical * 0.4f, -lateral * 0.3f, vertical * 0.5f );
	}

	internal void Apply( ref CameraView eye, PlayerController owner, bool mirrored, float sway, Settings settings )
	{
		var now = Time.NowDouble;
		var angles = eye.Rotation.Angles();
		if ( settings.BobCycle > 0 && owner.Mode is not Sandbox.Movement.SitMoveMode )
			ApplyBob( ref eye, ref angles, owner.Velocity, now, settings );

		var rotation = angles.ToRotation();
		eye.Rotation = rotation;
		var delay = settings.SwayDelay * (SwayDelay / Settings.Default.SwayDelay);
		if ( delay <= 0 || !float.IsFinite( delay ) ) return;
		if ( !double.IsFinite( _lastTime ) || now < _lastTime )
		{
			_history.Clear();
			_previous = (now, rotation);
		}
		if ( now != _lastTime ) _history.Enqueue( (now, rotation) );
		_lastTime = now;
		var sampleTime = now - delay;
		while ( _history.TryPeek( out var entry ) && entry.Time <= sampleTime ) _previous = _history.Dequeue();
		var lagged = _previous.Rotation;
		if ( _history.TryPeek( out var next ) && next.Time > _previous.Time )
			lagged = Rotation.Slerp( lagged, next.Rotation, (float)((sampleTime - _previous.Time) / (next.Time - _previous.Time)) );

		var turn = angles - lagged.Angles();
		if ( mirrored && settings.MirrorSway ) turn.yaw = -turn.yaw;
		if ( float.IsFinite( SwayScale ) )
			eye.Position += rotation * ((Vector3.Forward - turn.Forward) * (settings.TurnSway * (SwayScale / Settings.Default.TurnSway) * sway));
	}
}
