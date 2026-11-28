namespace Source1;

[Hide]
public abstract class MovingBrush : Component
{
	protected void Crush( GameObject target, float damage, Vector3 position )
	{
		if ( target.GetComponent<Prop>() is { } prop )
		{
			using var recipients = Rpc.FilterInclude( prop.Network.Owner ?? Connection.Local );
			DamageProp( prop, damage, position );
			return;
		}
		foreach ( var victim in target.GetComponents<IDamageable>() )
			victim.OnDamage( CrushDamage( damage, position ) );
	}

	[Rpc.Broadcast( NetFlags.HostOnly )]
	void DamageProp( Prop prop, float damage, Vector3 position )
	{
		if ( !prop.IsValid() || prop.IsProxy ) return;
		prop.OnDamage( CrushDamage( damage, position ) );
	}

	DamageInfo CrushDamage( float damage, Vector3 position ) => new()
	{
		Damage = damage, Attacker = GameObject, Weapon = GameObject, Origin = WorldPosition,
		Position = position, Tags = { DamageTags.Crush }
	};

	protected GameObject FindBlocker( Collider collider, Transform nextPose )
	{
		if ( !collider.IsValid() || !collider.Active ) return null;
		var bounds = collider.GetWorldBounds();
		var trace = Scene.Trace.Box( bounds.Size, bounds.Center, bounds.Center )
			.IgnoreGameObjectHierarchy( GameObject ).WithCollisionRules( Tags ).WithoutTags( "trigger" );
		foreach ( var hit in trace.RunAll() )
		{
			if ( hit.Collider?.Rigidbody is not { } body || hit.Collider.IsTrigger ) continue;
			if ( !collider.ComputePenetration( hit.Collider, out var normal, out var depth ) ) continue;
			var contact = collider.FindClosestPoint( hit.Collider.GetWorldBounds().Center );
			var motion = nextPose.PointToWorld( WorldTransform.PointToLocal( contact ) ) - contact;
			var advance = -motion.Dot( normal );
			if ( advance <= 0 ) continue;
			if ( !body.MotionEnabled ) return body.GameObject;
			Component actor = body.GetComponentInParent<PlayerController>();
			actor ??= body.GetComponentInParent<Sandbox.Npcs.BaseNpc>();
			if ( actor is null ) continue;

			var push = Scene.Trace.Body( body, body.WorldPosition - normal * (depth + advance) )
				.IgnoreGameObjectHierarchy( actor.GameObject ).IgnoreGameObjectHierarchy( GameObject )
				.WithCollisionRules( actor.Tags ).WithoutTags( "trigger" ).Run();
			if ( push.Hit || push.StartedSolid ) return actor.GameObject;
		}
		return null;
	}
}
