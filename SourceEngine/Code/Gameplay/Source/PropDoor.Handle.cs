namespace Source1;

[Hide]
public sealed partial class PropDoor : MovingDoor, Component.IPressable
{
	[Property] public SkinnedModelRenderer Renderer { get; set; }
	[Property] public bool UseCloses { get; set; }
	[Property] public SoundEvent UnlockedSound { get; set; }

	AnimationPlayer _animation;
	TimeUntil? _latch;
	GameObject _user;
	bool _open, _playing;

	public void OpenAwayFrom( string name, GameObject activator, GameObject caller )
	{
		if ( !Networking.IsHost || IsLocked || State is DoorState.Open or DoorState.Opening ) return;
		var target = name?.ToLowerInvariant() switch
		{
			null or "" => null,
			"!activator" => activator,
			"!caller" => caller,
			"!player" => Scene.GetAll<PlayerController>().FirstOrDefault()?.GameObject,
			_ => Scene.Directory.FindByName( name ).FirstOrDefault()
		};
		BeginOpen( target, activator );
	}

	bool CanUse => IsUsable && !_latch.HasValue &&
		(State is DoorState.Closed or DoorState.Ajar or DoorState.Closing || UseCloses);

	PropDoor UseTarget => Master.IsValid() ? Master : this;

	bool IPressable.CanPress( IPressable.Event e ) => UseTarget.CanUse;

	IPressable.Tooltip? IPressable.GetTooltip( IPressable.Event e )
	{
		var target = UseTarget;
		return target.GetUseHint( target.CanUse, target.State is DoorState.Open or DoorState.Opening );
	}

	bool IPressable.Press( IPressable.Event e )
	{
		var target = UseTarget;
		if ( !target.CanUse ) return false;
		target.Use( e.Source.GameObject );
		return true;
	}

	[Rpc.Host]
	void Use( GameObject user )
	{
		if ( !CanUse ) return;
		Activate( user );
	}

	internal float? OpenForNpc( GameObject npc )
	{
		var door = UseTarget;
		if ( door.IsLocked || door.Speed <= 0 ) return null;
		if ( door.State != DoorState.Closed || door._latch.HasValue ) return 0;
		door.Activate( npc );
		return System.MathF.Abs( door.TargetAngle ) / door.Speed;
	}

	void Activate( GameObject user )
	{
		if ( State is DoorState.Closing or DoorState.Ajar )
		{
			if ( !IsLocked ) BeginOpen( user, user, true );
			return;
		}
		if ( State == DoorState.Opening )
		{
			if ( !IsLocked ) Close();
			return;
		}
		var model = Renderer.Model;
		var sequences = ModelSequences.Of( model );
		var clip = sequences.FindActivity( IsLocked ? "ACT_DOOR_LOCKED" : "ACT_DOOR_OPEN" );
		PlayHandle( clip, !IsLocked );
		if ( IsLocked )
		{
			if ( LockedSound is not null ) PlaySound( LockedSound );
			RunDoo( LockedUse, c => c.SetArgument( "user", user ) );
			return;
		}
		var latch = sequences.Get( clip )?.Events.FirstOrDefault( e => e.Id == 1 || e.Name == "AE_DOOR_OPEN" );
		if ( latch is null )
		{
			if ( State == DoorState.Closed ) BeginOpen( user, user, true );
			else if ( CanClose( user, bothSides: false ) ) Close();
			return;
		}
		_open = State == DoorState.Closed;
		_user = user;
		_latch = model.AnimationClips()[clip].Duration * latch.Cycle;
	}

	[Rpc.Broadcast( NetFlags.HostOnly )]
	void PlayHandle( string clip, bool unlocked )
	{
		if ( Application.IsDedicatedServer ) return;
		if ( unlocked && UnlockedSound is not null ) GameObject.PlaySound( UnlockedSound );
		if ( clip is null ) return;
		var sequences = ModelSequences.Of( Renderer.Model );
		_animation ??= new();
		if ( _animation.Ensure( Renderer,
			[(sequences.FindActivity( "ACT_IDLE" ), true), (sequences.FindActivity( "ACT_DOOR_OPEN" ), false), (sequences.FindActivity( "ACT_DOOR_LOCKED" ), false)], clip ) )
		{
			_animation.Play( clip, restart: true );
			_playing = true;
		}
	}

	protected override void OnUpdate()
	{
		if ( _playing )
		{
			_animation.Tick();
			_playing = !_animation.Finished;
		}
		if ( _latch is not { } delay || delay > 0 ) return;
		_latch = null;
		if ( !IsLocked )
		{
			if ( _open ) BeginOpen( _user, _user, true );
			else if ( CanClose( _user, bothSides: false ) ) Close();
		}
		_user = null;
	}

	protected override void OnDestroy() => _animation?.Dispose();
}
