namespace GoldSrc.HalfLife;

[Hide]
public sealed class PlacedCharge : Component, Component.IDamageable, Component.ICollisionListener
{
	[Property] public GameObject Thrower { get; set; }
	[Property] public bool IsMine { get; set; }
	TimeUntil _arm;
	bool _armed, _spent;
	float _length;
	GameObject _surface;
	SoundHandle _chargeSound;

	internal static void Throw( PlayerController player, Ray ray )
	{
		var go = new GameObject( false, "Satchel charge" );
		go.WorldPosition = ray.Position;
		go.WorldRotation = Rotation.LookAt( ray.Forward );
		go.AddComponent<ModelRenderer>().Model = Model.Load( "mount://hl1/valve/models/w_satchel.mdl.vmdl" );
		var collider = go.AddComponent<BoxCollider>();
		collider.Scale = new Vector3( 8, 8, 8 );
		collider.Friction = 0.8f;
		var body = go.AddComponent<Rigidbody>();
		body.GravityScale = 0.5f;
		body.RigidbodyFlags |= RigidbodyFlags.DisableCollisionSounds;
		var charge = go.AddComponent<PlacedCharge>();
		charge.Thrower = player.GameObject;
		go.Enabled = true;
		body.Velocity = ray.Forward * 274 + player.Velocity;
		go.NetworkSpawn( true, null );
	}

	internal static void Attach( PlayerController player, SceneTraceResult hit )
	{
		var go = new GameObject( false, "Laser tripmine" );
		go.WorldPosition = hit.EndPosition + hit.Normal * 2;
		go.WorldRotation = Rotation.LookAt( hit.Normal );
		var renderer = go.AddComponent<SkinnedModelRenderer>();
		renderer.Model = Model.Load( "mount://hl1/valve/models/v_tripmine.mdl.vmdl" );
		PoseMine( renderer, "world" );
		go.AddComponent<BoxCollider>().Scale = new Vector3( 4, 8, 8 );
		var charge = go.AddComponent<PlacedCharge>();
		charge.Thrower = player.GameObject;
		charge.IsMine = true;
		charge._arm = 2.5f;
		if ( hit.GameObject.IsValid() ) go.SetParent( hit.GameObject, true );
		go.Enabled = true;
		go.NetworkSpawn( true, null );
		charge.PlayMineSound( false );
	}

	void ICollisionListener.OnCollisionStart( Collision collision )
	{
		if ( !Networking.IsHost || IsMine || collision.Other.IsTrigger || collision.Other.GameObject == Thrower ) return;
		var body = GetComponent<Rigidbody>();
		body.GravityScale = 1;
		if ( collision.Contact.Speed.WithZ( 0 ).Length <= 10 ) return;
		if ( Sandbox.Mounting.Directory.Get( "hl1" ) is HalfLifeMount mount )
			mount.NpcAudio.Emit( "weapons/g_bounce", collision.Contact.Point );
	}

	[Rpc.Broadcast( NetFlags.HostOnly )]
	void PlayMineSound( bool armed )
	{
		if ( Application.IsDedicatedServer || Sandbox.Mounting.Directory.Get( "hl1" ) is not HalfLifeMount mount ) return;
		_chargeSound?.Stop();
		var audio = mount.NpcAudio;
		if ( armed ) NpcAudio.Play( audio.Find( "weapons/mine_activate.wav" ), WorldPosition, 0.5f, 0.75f );
		else
		{
			NpcAudio.Play( audio.Find( "weapons/mine_deploy.wav" ), WorldPosition );
			_chargeSound = NpcAudio.Play( audio.Find( "weapons/mine_charge.wav" ), WorldPosition, 0.2f );
		}
	}

	protected override void OnDisabled() => _chargeSound?.Stop();

	internal static void PoseMine( SkinnedModelRenderer renderer, string pose )
	{
		var body = 3;
		foreach ( var part in renderer.Model.Parts.All )
		{
			if ( part.Choices.Count == 0 ) continue;
			renderer.SetBodyGroup( part.Name, body % part.Choices.Count );
			body /= part.Choices.Count;
		}
		renderer.UseAnimGraph = false;
		renderer.Sequence.Name = pose;
	}

	protected override void OnFixedUpdate()
	{
		if ( !Networking.IsHost || !IsMine || _arm > 0 ) return;
		var start = WorldPosition + WorldRotation.Forward * 3;
		var hit = Scene.Trace.Ray( start, start + WorldRotation.Forward * 2048 ).IgnoreGameObjectHierarchy( GameObject ).Run();
		if ( _armed )
		{
			if ( hit.GameObject != _surface || System.MathF.Abs( _length - hit.Distance ) > 2 ) Detonate();
			return;
		}

		_armed = true;
		PlayMineSound( true );
		_length = hit.Distance;
		_surface = hit.GameObject;
		var beam = GameObject.AddComponent<LineRenderer>();
		beam.UseVectorPoints = true;
		beam.VectorPoints = [start, hit.EndPosition];
		beam.Width = 1;
		beam.Color = new Color( 0, 214f / 255, 198f / 255, 64f / 255 );
		if ( Sandbox.Mounting.Directory.Get( "hl1" ) is HalfLifeMount mount && mount.GetIndexedSprite( "laserbeam" ) is { } sprite )
			beam.Texturing = beam.Texturing with { Texture = sprite.Animations[0].Frames[0].Texture };
		beam.Additive = true;
		beam.CastShadows = false;
	}

	void Component.IDamageable.OnDamage( in DamageInfo damage )
	{
		if ( Networking.IsHost ) Detonate();
	}

	internal void Detonate()
	{
		if ( _spent ) return;
		_spent = true;
		Blast.Detonate( GameObject, Thrower, WorldPosition, WorldRotation.Forward, 150, false );
		GameObject.Destroy();
	}
}
