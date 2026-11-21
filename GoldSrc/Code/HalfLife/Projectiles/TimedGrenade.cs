namespace GoldSrc.HalfLife;

[Hide]
public sealed class TimedGrenade : Component, Component.ICollisionListener
{
	[Property] public GameObject Thrower { get; set; }
	TimeUntil _fuse, _nextBounce, _nextWarning;
	bool _settledWarning;
	internal const string DangerStimulus = "grenade";

	internal static bool TryAim( Npc owner, Vector3 origin, GameObject target, Vector3 end, out Vector3 velocity, float flightTime = 1, bool requireFloor = true )
	{
		var scene = owner.Scene;
		var gravity = -scene.PhysicsWorld.Gravity.z * 0.5f;
		// Use the same flight time and gravity for the launch velocity and the clearance trace.
		velocity = (end - origin) / flightTime + Vector3.Up * (gravity * 0.5f * flightTime);
		var ground = scene.Trace.Ray( target.WorldPosition + Vector3.Up * 8, target.WorldPosition + Vector3.Down * 12 )
			.IgnoreGameObjectHierarchy( target ).Run();
		if ( requireFloor && !ground.Hit ) return false;
		var previous = origin;
		for ( int i = 1; i <= 8; i++ )
		{
			var t = i * flightTime / 8f;
			var next = origin + velocity * t + Vector3.Down * (gravity * 0.5f * t * t);
			if ( scene.Trace.Sphere( 2, previous, next ).IgnoreGameObjectHierarchy( owner.GameObject ).IgnoreGameObjectHierarchy( target ).Run().Hit )
				return false;
			previous = next;
		}
		return true;
	}

	internal static void Throw( Npc owner, Vector3 origin, Vector3 velocity, float fuse ) => Throw( owner.GameObject, origin, velocity, fuse );

	internal static void Throw( GameObject owner, Vector3 origin, Vector3 velocity, float fuse )
	{
		var go = new GameObject( false, "Hand grenade" );
		go.WorldPosition = origin;
		var renderer = go.AddComponent<ModelRenderer>();
		renderer.Model = Model.Load( "mount://hl1/valve/models/w_grenade.mdl.vmdl" );
		var collider = go.AddComponent<SphereCollider>();
		collider.Radius = 2;
		collider.Elasticity = 0.45f;
		collider.Friction = 0.8f;
		var body = go.AddComponent<Rigidbody>();
		body.GravityScale = 0.5f;
		var grenade = go.AddComponent<TimedGrenade>();
		grenade.Thrower = owner;
		grenade._fuse = fuse;
		go.Enabled = true;
		body.Velocity = velocity;
		body.AngularVelocity = Vector3.Random * 6;
		go.NetworkSpawn( true, null );
	}

	void ICollisionListener.OnCollisionStart( Collision collision )
	{
		if ( !Networking.IsHost ) return;
		var velocity = GetComponent<Rigidbody>().Velocity;
		if ( !_settledWarning && velocity.WithZ( velocity.z * 0.45f ).Length <= 60 )
		{
			_settledWarning = true;
			Scene.GetSystem<Sandbox.Npcs.NpcSystem>().EmitStimulus( DangerStimulus, WorldPosition, GameObject, 250, 0.3f );
		}
		if ( _nextBounce > 0 || System.MathF.Abs( collision.Contact.NormalSpeed ) < 10 ) return;
		_nextBounce = 0.12f;
		if ( Sandbox.Mounting.Directory.Get( "hl1" ) is HalfLifeMount mount )
			mount.NpcAudio.Emit( "weapons/grenade_hit", WorldPosition, volume: 0.25f );
	}

	protected override void OnFixedUpdate()
	{
		if ( !Networking.IsHost ) return;
		if ( _fuse > 0 )
		{
			if ( _fuse < 1 && _nextWarning <= 0 )
			{
				_nextWarning = 0.1f;
				var landing = WorldPosition + GetComponent<Rigidbody>().Velocity * (float)_fuse;
				Scene.GetSystem<Sandbox.Npcs.NpcSystem>().EmitStimulus( DangerStimulus, landing, GameObject, 400, 0.1f );
			}
			return;
		}
		var floor = Scene.Trace.Ray( WorldPosition + Vector3.Up * 8, WorldPosition + Vector3.Down * 40 )
			.IgnoreGameObjectHierarchy( GameObject ).Run();
		Blast.Detonate( GameObject, Thrower, floor.Hit ? floor.EndPosition : WorldPosition, floor.Normal, 100, floor.Hit );
		GameObject.Destroy();
	}
}
