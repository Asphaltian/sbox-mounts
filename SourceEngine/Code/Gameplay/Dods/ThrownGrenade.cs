using System;

namespace Source1.Dods;

public abstract class ThrownGrenade : Projectile, Component.ICollisionListener
{
	protected abstract string BounceSound { get; }
	protected override float? Physical => 3;
	protected override Vector3 Spin => new Vector3( 600, Game.Random.Float( -1200, 1200 ), 0 ) * (MathF.PI / 180);

	public void OnCollisionStart( Collision collision )
	{
		if ( Application.IsDedicatedServer || collision.Contact.Speed.Length < 50 ) return;
		if ( Sounds.Sound( BounceSound, MountName ) is { } sound ) Sound.Play( sound, WorldPosition );
	}
}
