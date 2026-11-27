using System;

namespace Source1.Dods;

public abstract class ThrownGrenade : Projectile, Component.ICollisionListener
{
	protected abstract string BounceSound { get; }
	protected override float? Physical => 4;
	protected override Vector3 Spin => new Vector3( 600, Game.Random.Float( -1200, 1200 ), 0 ) * (MathF.PI / 180);

	protected override Collider CreateCollider( float halfSize )
	{
		var model = Model.Load( $"mount://{MountName}/{ModelPath}.vmdl" );
		if ( model?.Physics?.Parts.Count is not > 0 ) return base.CreateCollider( halfSize );

		var collider = GameObject.AddComponent<ModelCollider>();
		collider.Model = model;
		return collider;
	}

	protected override void OnStart()
	{
		base.OnStart();
		var gravity = -Scene.PhysicsWorld.Gravity.z;
		if ( gravity > 0 ) GetComponent<Rigidbody>().GravityScale = 420 / gravity;
	}

	public void OnCollisionStart( Collision collision )
	{
		if ( Application.IsDedicatedServer || collision.Contact.Speed.Length < 50 ) return;
		if ( Sounds.Sound( BounceSound, MountName ) is { } sound ) Sound.Play( sound, WorldPosition );
	}
}
