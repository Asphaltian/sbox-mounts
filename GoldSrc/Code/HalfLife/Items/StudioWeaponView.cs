namespace GoldSrc.HalfLife;

[Hide]
public sealed class StudioWeaponView : BaseWeaponModel
{
	IReadOnlyList<StudioSequence> _clips;
	StudioSequence _current;
	TimeSince _elapsed;
	readonly StudioSequenceEvents _sequenceEvents = new();
	NpcAudio _audio;

	internal void Initialize( StudioWeapon weapon, ModelLoader loader, NpcAudio audio )
	{
		_clips = loader.Sequences;
		_audio = audio;
		_weapon = weapon;
		Renderer.UseAnimGraph = true;
		Renderer.Animation.Graph = loader.AnimationGraphs.GetGraph( _clips.ToArray() );
		OnDeploy();
	}

	internal void Play( string name )
	{
		_current = _clips?.FirstOrDefault( clip => string.Equals( clip.Name, name, System.StringComparison.OrdinalIgnoreCase ) );
		_elapsed = 0;
		_sequenceEvents.Reset( _current, 0, true );
	}

	StudioWeapon _weapon;

	public override void OnDeploy() => Play( _weapon.DeploySequence );
	public override void OnAttack( Vector3? hitPoint = null, Vector3? origin = null ) =>
		Play( _weapon.ShotSequence );
	public override void OnReloadStart() =>
		Play( _weapon.ReloadSequence );

	protected override void OnUpdate()
	{
		if ( Scene.Camera.IsValid() ) WorldTransform = Scene.Camera.WorldTransform;
		if ( _current is null || !Renderer.Animation.IsValid ) return;
		_sequenceEvents.Update( _audio, Renderer, _elapsed, false );
		if ( _elapsed >= _current.Duration ) Play( _weapon.IdleSequence );
		if ( _current is null ) return;
		Renderer.Animation.Parameters.Set( StudioAnimationGraphs.StateParameter, _current.Name );
		Renderer.Animation.Parameters.Set( _current.TimeParameter, (float)_elapsed / _current.Duration );
	}
}
