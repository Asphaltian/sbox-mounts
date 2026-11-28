using System;

namespace Source1;

public sealed partial class PropDoor
{
	public enum DoorState { Closed, Ajar, Opening, Open, Closing }

	[Property, Sync] public DoorState State { get; set; }
	[Property] public Angles InitialAngles { get; set; }
	[Property] public bool OpensBackward { get; set; }
	[Property] public float TargetAngle { get; set; }
	[Property] public float ReturnDelay { get; set; }
	[Property] public PropDoor Master { get; set; }
	[Property] public List<PropDoor> LinkedDoors { get; set; } = [];
	[Property] public SoundEvent MoveSound { get; set; }
	[Property] public Doo RotationDone { get; set; }

	Angles _angles;
	Angles? _goal;
	GameObject _openedBy;
	TimeUntil? _closeAt;
	SoundHandle _moving;

	Rotation ClosedRotation => ClosedAngles.ToRotation();

	protected override void OnStart() => _angles = InitialAngles;

	internal void SetRotationDistance( float distance, bool move )
	{
		TargetAngle = (OpensBackward ? 1 : -1) * Math.Abs( distance == 0 ? 90 : distance );
		if ( move ) _goal = OpenAngles( TargetAngle );
	}

	public void Open( GameObject user )
	{
		if ( !IsLocked ) BeginOpen( null, user );
	}

	public void Toggle( GameObject user )
	{
		if ( State == DoorState.Closed ) Open( user );
		else if ( State == DoorState.Open ) Close();
	}

	void BeginOpen( GameObject awayFrom, GameObject user, bool playerUse = false )
	{
		if ( !Networking.IsHost || State is DoorState.Open or DoorState.Opening ) return;
		_openedBy = user;
		var angle = TargetAngle;
		if ( OpenBothWays && awayFrom.IsValid() )
		{
			var leaf = WorldTransform.PointToWorld( Renderer.Model.PhysicsBounds.Center ) - WorldPosition;
			var side = Vector3.Cross( leaf, awayFrom.WorldPosition - WorldPosition ).z;
			angle = side < 0 ? Math.Abs( angle ) : -Math.Abs( angle );
			if ( playerUse && !IsClear( BoundsAt( ClosedRotation ).AddBBox( BoundsAt( OpenRotation( angle ) ) ), user ) ) angle = -angle;
		}
		_goal = OpenAngles( angle );
		State = DoorState.Opening;
		StartMoving();
		RunDoo( OnOpen, c => c.SetArgument( "user", user ) );
		foreach ( var leaf in LinkedDoors )
			if ( leaf.IsValid() ) leaf.BeginOpen( awayFrom, user, playerUse );
	}

	public void Close()
	{
		if ( !Networking.IsHost || State is DoorState.Closed or DoorState.Closing ) return;
		_goal = ClosedAngles;
		State = DoorState.Closing;
		StartMoving();
		RunDoo( OnClose, c => c.SetArgument( "user", GameObject ) );
		foreach ( var leaf in LinkedDoors )
			if ( leaf.IsValid() ) leaf.Close();
	}

	void StartMoving()
	{
		_latch = null;
		_user = null;
		_closeAt = null;
		if ( MoveSound is not null ) PlayMovement( true );
	}

	protected override void OnFixedUpdate()
	{
		if ( !Networking.IsHost ) return;
		if ( _closeAt is { } due && due <= 0 )
		{
			if ( CanClose( _openedBy, bothSides: true ) ) Close();
			else _closeAt = ReturnDelay + 0.1f;
		}
		if ( _goal is not { } goal ) return;
		var delta = goal - _angles;
		var remaining = delta.AsVector3().Length;
		var step = Speed * Time.Delta;
		var next = remaining > step ? _angles + delta * (step / remaining) : goal;
		var rotation = next.ToRotation();
		if ( MotionBlocked( rotation ) ) return;
		_angles = next;
		LocalRotation = rotation;
		if ( remaining > step ) return;

		_goal = null;
		if ( State is DoorState.Opening or DoorState.Closing ) FinishMovement();
		RunDoo( RotationDone, c => c.SetArgument( "user", GameObject ) );
	}

	void FinishMovement()
	{
		bool opened = State == DoorState.Opening;
		State = opened ? DoorState.Open : DoorState.Closed;
		if ( opened && ReturnDelay >= 0 ) _closeAt = ReturnDelay + 0.1f;
		if ( MoveSound is not null ) PlayMovement( false );
		if ( (opened ? OpenFinishedSound : CloseFinishedSound) is { } sound ) PlaySound( sound );
		RunDoo( opened ? FullyOpen : FullyClosed, c => c.SetArgument( "user", GameObject ) );
	}

	[Rpc.Broadcast( NetFlags.HostOnly )]
	void PlayMovement( bool start )
	{
		_moving?.Stop();
		if ( start && !Application.IsDedicatedServer && MoveSound is not null ) _moving = GameObject.PlaySound( MoveSound );
	}

	[Rpc.Broadcast( NetFlags.HostOnly )]
	void PlaySound( SoundEvent sound )
	{
		if ( !Application.IsDedicatedServer && sound is not null ) GameObject.PlaySound( sound );
	}

	protected override void OnDisabled() => _moving?.Stop();
}
