namespace GoldSrc.HalfLife;

[Hide]
public sealed partial class PlacedCharge : Component, Component.IDamageable, Component.ICollisionListener
{
	[Property] public GameObject Thrower { get; set; }
	[Property] public bool IsMine { get; set; }
	bool _spent;

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

	void ICollisionListener.OnCollisionStart( Collision collision )
	{
		if ( !Networking.IsHost || IsMine || collision.Other.IsTrigger || collision.Other.GameObject == Thrower ) return;
		var body = GetComponent<Rigidbody>();
		body.GravityScale = 1;
		if ( collision.Contact.Speed.WithZ( 0 ).Length <= 10 ) return;
		if ( Sandbox.Mounting.Directory.Get( "hl1" ) is HalfLifeMount mount )
			mount.NpcAudio.Emit( "weapons/g_bounce", collision.Contact.Point );
	}

	void Component.IDamageable.OnDamage( in DamageInfo damage )
	{
		if ( !Networking.IsHost || _spent || _detonateAt.HasValue || damage.Damage <= 0 ) return;
		if ( !IsMine ) { Detonate(); return; }
		if ( !Armed && damage.Damage < 1 ) { GameObject.Destroy(); return; }
		if ( damage.Attacker?.GetComponentInParent<PlayerController>() is { } attacker ) Thrower = attacker.GameObject;
		TriggerMine();
	}

	void TriggerMine() => _detonateAt = Game.Random.Float( 0.1f, 0.3f );

	internal void Detonate()
	{
		if ( _spent ) return;
		_spent = true;
		var normal = WorldRotation.Forward;
		var contact = IsMine ? Scene.Trace.Ray( WorldPosition + normal * 8, WorldPosition - normal * 64 )
			.IgnoreGameObjectHierarchy( GameObject ).Run() : default;
		Blast.Detonate( GameObject, Thrower, contact.Hit ? contact.EndPosition : WorldPosition, contact.Hit ? contact.Normal : normal, 150, contact.Hit );
		GameObject.Destroy();
	}
}
