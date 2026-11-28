namespace Source1;

/// <summary>Surface impacts on a mounted BSP fluid hull.</summary>
[Hide]
public sealed class WaterImpact : Component, Component.ITriggerListener
{
	[Property] public string Mount { get; set; }

	/// <summary>Play a local splash where a resolved shot enters mounted water.</summary>
	public static bool TrySplash( Scene scene, Vector3 start, Vector3 end )
	{
		if ( Application.IsDedicatedServer || TraceEntry( scene, start, end, out var position ) is not { } water ) return false;
		WeaponEffects.WaterSplash( position, water.Mount, water.Tags.Has( "slime" ) );
		return true;
	}

	internal static void ProjectileCrossing( Scene scene, Vector3 start, Vector3 end )
	{
		if ( TraceEntry( scene, start, end, out var position ) is { } water )
			BroadcastSplash( position, water.Mount, water.Tags.Has( "slime" ), false );
	}

	static WaterImpact TraceEntry( Scene scene, Vector3 start, Vector3 end, out Vector3 position )
	{
		position = default;
		if ( start.z <= end.z ) return null;
		var hit = scene.Trace.Ray( start, end ).WithTag( "water" ).HitTriggersOnly().Run();
		if ( !hit.Hit || hit.StartedSolid || hit.Normal.z < 0.99f ) return null;
		position = hit.HitPosition;
		return hit.GameObject.GetComponent<WaterImpact>();
	}

	void ITriggerListener.OnTriggerEnter( GameObject other )
	{
		if ( !Networking.IsHost || other.GetComponent<Rigidbody>() is not { } body || body.Velocity.z >= 0 ) return;
		var bounds = body.GetWorldBounds();
		var level = WorldPosition.z;
		if ( bounds.Maxs.z < level || bounds.Mins.z > level + 1 ) return;

		var extent = bounds.Size * 0.5f;
		var speed = body.Velocity.z;
		var strength = speed * speed * extent.x * extent.y * body.Mass / 250000000f;
		if ( strength <= 0.1f ) return;
		var center = bounds.Center.WithZ( level );
		if ( TraceEntry( Scene, center + Vector3.Up * 2, center + Vector3.Down * 2, out var position ) == this )
			BroadcastSplash( position, Mount, Tags.Has( "slime" ), strength <= 0.35f );
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Unreliable )]
	static void BroadcastSplash( Vector3 position, string mount, bool slime, bool rippleOnly )
	{
		if ( Application.IsDedicatedServer ) return;
		if ( rippleOnly ) FlatQuad.WaterRipple( position, Vector3.Up, mount );
		else WeaponEffects.WaterSplash( position, mount, slime );
	}
}
