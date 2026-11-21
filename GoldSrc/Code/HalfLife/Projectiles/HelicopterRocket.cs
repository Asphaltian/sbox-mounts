using Sandbox.Npcs.Layers;

namespace GoldSrc.HalfLife;

[Hide]
public sealed class HelicopterRocket : NpcProjectile
{
	internal override float LaunchSpeed => 100;
	internal override float Lifetime => 12;
	Vector3 _axis;
	float _nextAcceleration = 0.2f;
	bool _ignited;
	SoundHandle _motor;

	internal override void PrepareLaunch()
	{
		_axis = WorldRotation.Forward;
		if ( Shooter.GetComponent<FlyingLayer>() is { } flight ) Velocity += flight.Velocity;
	}

	internal override void Guide()
	{
		if ( Age < _nextAcceleration ) return;
		_nextAcceleration += 0.1f;
		if ( Velocity.Length < 1800 ) Velocity += _axis * 200;
		if ( Velocity.LengthSquared > 0 ) WorldRotation = Rotation.LookAt( Velocity );
	}

	protected override void OnUpdate()
	{
		if ( !_ignited && Age >= 0.1f && Sandbox.Mounting.Directory.Get( "hl1" ) is HalfLifeMount mount )
		{
			_ignited = true;
			var exhaust = new GameObject( GameObject, false, "Rocket exhaust" );
			exhaust.NetworkMode = NetworkMode.Never;
			var trail = exhaust.AddComponent<TrailRenderer>();
			trail.LifeTime = 1.5f;
			trail.Width = 5;
			trail.Color = new Color( 0.88f, 0.88f, 1 );
			trail.Opaque = false;
			if ( mount.GetIndexedSprite( "smoke" ) is { } smoke )
			{
				var material = Material.Load( "materials/default/default_line.vmat" ).CreateCopy();
				material.Set( "g_tColor", smoke.Animations[0].Frames[0].Texture );
				trail.Texturing = TrailTextureConfig.Default with { Material = material };
			}
			var lamp = exhaust.AddComponent<PointLight>();
			lamp.Radius = 96;
			lamp.Shadows = false;
			exhaust.Enabled = true;
			if ( mount.NpcAudio.Find( "weapons/rocket1.wav" ) is { } path && SoundFile.Load( path ) is { } sound )
				_motor = Sound.PlayFile( sound );
		}
		if ( _motor.IsValid() ) _motor.Position = WorldPosition;
	}

	internal override void OnImpact( IDamageable victim, Vector3 position, Vector3 normal )
	{
		Blast.Detonate( GameObject, Shooter, position, normal, 150, true );
	}

	protected override void OnDisabled() => _motor?.Stop();
}
