namespace Source1;

public sealed partial class PropDoor
{
	[Property] public ModelCollider Collider { get; set; }
	PropDoor _blockedLeaf;

	bool MotionBlocked( Rotation next )
	{
		var master = UseTarget;
		if ( master._blockedLeaf is { } stopped && stopped != this )
		{
			if ( stopped.IsValid() && stopped.Active && stopped._goal.HasValue ) return true;
			master.SetBlockedLeaf( null, null );
		}
		var nextPose = WorldTransform.WithRotation( (GameObject.Parent?.WorldRotation ?? Rotation.Identity) * next );
		var blocker = FindBlocker( Collider, nextPose );
		var blocked = blocker.IsValid();
		if ( UpdateBlocked( blocker, State == DoorState.Closing, sendUnblocked: master == this ) )
		{
			if ( MoveSound is not null ) PlayMovement( !blocked );
			if ( master != this ) master.SetBlockedLeaf( this, blocker );
			if ( blocked && blocker.GetComponentInParent<Sandbox.Npcs.BaseNpc>() is { } npc )
			{
				var moving = master.State == State ? master : this;
				if ( State == DoorState.Closing ) moving.BeginOpen( npc.GameObject, npc.GameObject );
				else moving.Close();
				return true;
			}
		}
		if ( !blocked || !ForceClosed ) return blocked;
		if ( blocker.IsValid() && blocker.GetComponent<Prop>() is { Health: > 0 } prop )
			Crush( blocker, prop.Health, Collider.FindClosestPoint( prop.WorldPosition ) );
		return false;
	}

	void SetBlockedLeaf( PropDoor leaf, GameObject blocker )
	{
		_blockedLeaf = blocker.IsValid() ? leaf : null;
		LinkedBlockChanged( blocker );
		foreach ( var other in LinkedDoors )
			if ( other.IsValid() && other != leaf ) other.LinkedBlockChanged( blocker );
	}

	void LinkedBlockChanged( GameObject blocker )
	{
		if ( !_goal.HasValue ) return;
		if ( MoveSound is not null ) PlayMovement( !blocker.IsValid() );
		if ( blocker.IsValid() || UseTarget == this ) FireBlockOutput( blocker, State == DoorState.Closing );
	}

	bool CanClose( GameObject user, bool bothSides )
	{
		var master = UseTarget;
		if ( !master.LeafCanClose( user, bothSides ) ) return false;
		foreach ( var leaf in master.LinkedDoors )
			if ( leaf.IsValid() && !leaf.LeafCanClose( user, bothSides ) ) return false;
		return true;
	}

	bool LeafCanClose( GameObject user, bool bothSides )
	{
		var bounds = BoundsAt( ClosedRotation ).AddBBox( Renderer.Model.PhysicsBounds.Transform( WorldTransform ) );
		if ( bothSides )
			bounds = bounds.AddBBox( BoundsAt( OpenRotation( TargetAngle ) ) ).AddBBox( BoundsAt( OpenRotation( -TargetAngle ) ) );
		return IsClear( bounds, user );
	}

	Angles OpenAngles( float angle ) => ClosedAngles.WithYaw( ClosedAngles.yaw + angle );
	Rotation OpenRotation( float angle ) => OpenAngles( angle ).ToRotation();

	BBox BoundsAt( Rotation localRotation )
	{
		var rotation = (GameObject.Parent?.WorldRotation ?? Rotation.Identity) * localRotation;
		return Renderer.Model.PhysicsBounds.Transform( WorldTransform.WithRotation( rotation ) );
	}

	bool IsClear( BBox bounds, GameObject user )
	{
		var trace = Scene.Trace.Box( bounds.Size, bounds.Center, bounds.Center )
			.IgnoreGameObjectHierarchy( GameObject ).WithCollisionRules( Tags ).WithoutTags( "trigger" );
		if ( user.IsValid() ) trace = trace.IgnoreGameObjectHierarchy( user );
		foreach ( var hit in trace.RunAll() )
		{
			if ( !hit.StartedSolid || !hit.GameObject.IsValid() || hit.Collider is { IsTrigger: true } ) continue;
			if ( hit.GameObject.IsStatic && !hit.GameObject.Tags.Has( "source1_entity" ) ) continue;
			if ( hit.GameObject.GetComponent<PropDoor>() is { } door && door.UseTarget == UseTarget ) continue;
			var body = hit.Collider?.Rigidbody;
			if ( body is { MotionEnabled: true } && body.PhysicsBody.IsValid() && body.PhysicsBody.Mass < 32
				&& body.GetComponentInParent<PlayerController>() is null ) continue;
			return false;
		}
		return true;
	}
}
