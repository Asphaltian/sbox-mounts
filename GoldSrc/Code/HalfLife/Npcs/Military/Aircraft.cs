namespace GoldSrc.HalfLife;

[Hide]
public abstract class Aircraft : Npc
{
	internal abstract string RotorSample { get; }
	internal abstract string DebrisModel { get; }
	SoundHandle _rotor;
	SoundFile _rotorSample;

	protected override void OnUpdate()
	{
		base.OnUpdate();
		if ( _rotorSample is null && Sandbox.Mounting.Directory.Get( "hl1" ) is HalfLifeMount mount && mount.NpcAudio.Find( RotorSample ) is { } path )
			_rotorSample = SoundFile.Load( path );
		if ( _rotorSample is null ) return;
		if ( !_rotor.IsValid() || !_rotor.IsPlaying ) _rotor = Sound.PlayFile( _rotorSample );
		if ( !_rotor.IsValid() ) return;
		_rotor.Position = WorldPosition;
		_rotor.Distance = 2048;
	}

	protected override GameObject CreateCorpse() => AircraftWreck.Spawn( this );

	protected override void OnDisabled()
	{
		_rotor?.Stop();
		base.OnDisabled();
	}
}
