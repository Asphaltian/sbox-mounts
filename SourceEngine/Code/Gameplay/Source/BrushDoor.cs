using System;
using DoorState = Sandbox.Mapping.Door.DoorState;

namespace Source1;

[Hide]
public sealed class BrushDoor : MovingDoor, Component.IPressable, Component.ICollisionListener
{
	[Property] public Vector3 Travel { get; set; }
	[Property] public bool Rotates { get; set; }
	[Property] public Angles OpenAngles { get; set; }
	[Property] public BBox BrushBounds { get; set; }
	[Property] public string ChainTarget { get; set; }
	[Property] public string MovementGroup { get; set; }
	[Property] public float BlockDamage { get; set; }
	[Property] public float Wait { get; set; }
	[Property] public bool StartOpen { get; set; }
	[Property] public bool LegacyStartOpen { get; set; }
	[Property] public bool ToggleUse { get; set; }
	[Property] public bool TouchOpens { get; set; }
	[Property] public SoundEvent OpenSound { get; set; }
	[Property] public SoundEvent CloseSound { get; set; }
	[Sync] public DoorState State { get; private set; }

	Collider[] _colliders;
	Vector3 _closedPosition;
	Vector3 _pose;
	Vector3 _destination;
	Vector3 ClosedPose => Rotates ? ClosedAngles.AsVector3() : _closedPosition;
	Vector3 OpenPose => Rotates ? OpenAngles.AsVector3() : _closedPosition + Travel;
	TimeUntil? _closeAt;
	GameObject _activator;
	SoundHandle _movement;

	protected override void OnStart()
	{
		if ( !Networking.IsHost ) return;
		_colliders = GetComponentsInChildren<PhysicsCollider>( true ).ToArray();
		_closedPosition = LocalPosition;
		State = StartOpen ? DoorState.Open : DoorState.Closed;
		SetPose( StartOpen ? OpenPose : ClosedPose );
	}

	void SetPose( Vector3 pose )
	{
		_pose = pose;
		if ( Rotates ) LocalRotation = Rotation.From( new Angles( pose.x, pose.y, pose.z ) );
		else LocalPosition = pose;
	}

	internal void Rebase( Transform frame )
	{
		if ( Rotates )
		{
			var previous = ClosedAngles;
			ClosedAngles = (frame.Rotation * previous.ToRotation()).Angles();
			var delta = ClosedAngles - previous;
			OpenAngles += delta;
			_pose += delta.AsVector3();
			_destination += delta.AsVector3();
		}
		else
		{
			Travel = frame.PointToWorld( Travel ) - frame.Position;
			_closedPosition = frame.PointToWorld( _closedPosition );
			_pose = frame.PointToWorld( _pose );
			_destination = frame.PointToWorld( _destination );
		}
	}

	bool ShouldReverseSwing()
	{
		if ( !OpenBothWays || !_activator.IsValid() ) return false;
		var user = _activator.WorldPosition;
		var nearest = BrushBounds.ClosestPoint( WorldTransform.PointToLocal( user ) );
		var toDoor = (WorldTransform.PointToWorld( nearest ) - user).WithZ( 0 );
		var toHinge = (WorldPosition - user).WithZ( 0 );
		if ( toHinge.DistanceSquared( toDoor ) < 25 && toHinge.Normal.Dot( toDoor.Normal ) > 0.99f )
			toHinge += (WorldPosition - WorldTransform.PointToWorld( BrushBounds.Center )).WithZ( 0 );
		return toHinge.Cross( toDoor ).z > 0;
	}

	public void SetToggleState( float state )
	{
		if ( !Networking.IsHost ) return;
		SetPose( (int)state == 0 ? OpenPose : ClosedPose );
	}

	public void Open( GameObject user )
	{
		if ( Networking.IsHost && !IsLocked ) BeginOpen();
	}

	void BeginOpen()
	{
		if ( State is DoorState.Open or DoorState.Opening ) return;
		_destination = ShouldReverseSwing() ? -OpenPose : OpenPose;
		State = DoorState.Opening;
		_closeAt = null;
		Play( OpenSound, true );
		Fire( OnOpen, GameObject );
	}

	public void Close()
	{
		if ( !Networking.IsHost || State is DoorState.Closed or DoorState.Closing ) return;
		_destination = ClosedPose;
		State = DoorState.Closing;
		_closeAt = null;
		Play( CloseSound, true );
		Fire( OnClose, GameObject );
	}

	public void Toggle( GameObject user )
	{
		if ( IsLocked ) return;
		if ( State == DoorState.Closed ) Open( user );
		else if ( State == DoorState.Open ) Close();
	}

	bool CanUse => State == DoorState.Closed || (ToggleUse && State == DoorState.Open);
	bool IPressable.CanPress( IPressable.Event e ) => IsUsable && CanUse;
	IPressable.Tooltip? IPressable.GetTooltip( IPressable.Event e ) => GetUseHint( IsUsable && CanUse, State == DoorState.Open );

	bool IPressable.Press( IPressable.Event e )
	{
		if ( !IsUsable || !CanUse ) return false;
		Use( e.Source.GameObject );
		return true;
	}

	void ICollisionListener.OnCollisionStart( Collision collision )
	{
		if ( Networking.IsHost && collision.Other.GameObject.GetComponentInParent<PlayerController>() is { } player )
			ActivateChain( player.GameObject, true );
	}

	[Rpc.Host]
	void Use( GameObject user ) => ActivateChain( user, false );

	void ActivateChain( GameObject user, bool touch )
	{
		if ( !string.IsNullOrEmpty( ChainTarget ) )
		{
			foreach ( var target in Scene.Directory.FindByName( ChainTarget ) )
				if ( target != GameObject ) target.GetComponent<BrushDoor>()?.Activate( user, touch );
		}
		Activate( user, touch );
	}

	void Activate( GameObject user, bool touch )
	{
		if ( !(touch ? TouchOpens : IsUsable) || !CanUse ) return;
		if ( IsLocked )
		{
			Fire( LockedUse, user );
			Play( LockedSound, false );
			return;
		}
		_activator = user;
		Toggle( user );
	}

	protected override void OnFixedUpdate()
	{
		if ( !Networking.IsHost ) return;
		if ( _closeAt is { } due && due <= 0 ) Close();
		if ( State is not (DoorState.Opening or DoorState.Closing) ) return;
		bool opening = State == DoorState.Opening;
		var delta = _destination - _pose;
		var step = Speed * Time.Delta;
		var arrived = delta.LengthSquared <= step * step;
		var next = arrived ? _destination : _pose + delta.Normal * step;
		if ( MotionBlocked( next, opening ) ) return;
		SetPose( next );
		if ( !arrived ) return;
		State = opening ? DoorState.Open : DoorState.Closed;
		Play( opening ? OpenFinishedSound : CloseFinishedSound, false );
		if ( opening && !ToggleUse && Wait >= 0 ) _closeAt = Wait;
		Fire( opening != LegacyStartOpen ? FullyOpen : FullyClosed, opening ? GameObject : _activator );
	}

	bool MotionBlocked( Vector3 next, bool opening )
	{
		var local = Rotates ? LocalTransform.WithRotation( Rotation.From( new Angles( next.x, next.y, next.z ) ) ) : LocalTransform.WithPosition( next );
		var world = GameObject.Parent?.WorldTransform.ToWorld( local ) ?? local;
		foreach ( var collider in _colliders )
		{
			var blocker = FindBlocker( collider, world );
			if ( !blocker.IsValid() ) continue;
			var firstContact = UpdateBlocked( blocker, !opening );
			var prop = blocker.IsValid() ? blocker.GetComponent<Prop>() : null;
			if ( BlockDamage > 0 && (prop is null || prop.Health > 0) )
				Crush( blocker, BlockDamage, collider.FindClosestPoint( blocker.WorldPosition ) );
			if ( !ForceClosed )
			{
				if ( !string.IsNullOrEmpty( MovementGroup ) && firstContact )
				{
					foreach ( var other in Scene.Directory.FindByName( MovementGroup ) )
						if ( other != GameObject && other.GetComponent<BrushDoor>() is { Wait: >= 0 } leaf ) leaf.Reverse();
				}
				if ( Wait >= 0 )
				{
					Reverse();
					return true;
				}
			}
			return BlockDamage <= 0 || prop is { Health: > 0 } || Npcs.IsPlayer( blocker );
		}
		UpdateBlocked( null, !opening );
		return false;
	}

	void Reverse()
	{
		if ( State == DoorState.Closing ) BeginOpen();
		else Close();
	}

	void Fire( Doo output, GameObject user ) => RunDoo( output, c => c.SetArgument( "user", user ) );

	[Rpc.Broadcast( NetFlags.HostOnly )]
	void Play( SoundEvent sound, bool moving )
	{
		if ( Application.IsDedicatedServer ) return;
		_movement?.Stop();
		if ( sound is null ) return;
		var playing = GameObject.PlaySound( sound );
		if ( moving ) _movement = playing;
	}

	protected override void OnDisabled() => _movement?.Stop();
}
