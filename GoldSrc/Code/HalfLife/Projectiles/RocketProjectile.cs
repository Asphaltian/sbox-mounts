namespace GoldSrc.HalfLife;

[Hide]
public abstract class RocketProjectile : NpcProjectile
{
	internal abstract float IgnitionDelay { get; }
	internal abstract float SmokeLifetime { get; }
	internal virtual float BurnTime => float.PositiveInfinity;
	bool _ignited;
	SoundHandle _motor;
	PointLight _lamp;

	protected override void OnUpdate()
	{
		if ( !_ignited && Age >= IgnitionDelay && Sandbox.Mounting.Directory.Get( "hl1" ) is HalfLifeMount mount )
		{
			_ignited = true;
			var exhaust = new GameObject( GameObject, false, "Rocket exhaust" );
			exhaust.NetworkMode = NetworkMode.Never;
			var trail = exhaust.AddComponent<TrailRenderer>();
			trail.LifeTime = SmokeLifetime;
			trail.Width = 5;
			trail.Color = new Color( 0.88f, 0.88f, 1 );
			trail.Opaque = false;
			trail.Texturing = TrailTextureConfig.Default with { Material = mount.GetTrailMaterial( "smoke" ) };
			_lamp = exhaust.AddComponent<PointLight>();
			_lamp.Radius = 96;
			_lamp.Shadows = false;
			exhaust.Enabled = true;
			if ( mount.NpcAudio.Find( "weapons/rocket1.wav" ) is { } path && SoundFile.Load( path ) is { } sound )
				_motor = GoldSrcSoundEvents.Play( sound, WorldPosition, attenuation: 0.5f );
		}
		if ( Age >= IgnitionDelay + BurnTime )
		{
			_motor?.Stop();
			_motor = null;
			if ( _lamp.IsValid() ) _lamp.Enabled = false;
		}
		else if ( _motor.IsValid() ) _motor.Position = WorldPosition;
	}

	protected override void OnDisabled() => _motor?.Stop();
}
