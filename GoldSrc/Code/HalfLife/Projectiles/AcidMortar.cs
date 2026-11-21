namespace GoldSrc.HalfLife;

[Hide]
public sealed class AcidMortar : NpcProjectile
{
	TimeUntil _nextSpray;
	internal override string HitTag => "acid";
	internal override void Guide() => Velocity += Scene.PhysicsWorld.Gravity * Time.Delta;

	protected override void OnStart()
	{
		if ( Sandbox.Mounting.Directory.Get( "hl1" ) is not HalfLifeMount mount || mount.GetIndexedSprite( "mommaspit" ) is not { } spit ) return;
		var renderer = GameObject.AddComponent<SpriteRenderer>();
		renderer.Sprite = spit;
		renderer.Size = 32;
		renderer.Additive = true;
		Spray( WorldPosition, Vector3.Up, 24 );
		_nextSpray = 0.4f;
	}

	protected override void OnUpdate()
	{
		if ( _nextSpray > 0 ) return;
		_nextSpray = 0.2f;
		Spray( WorldPosition, -Velocity.Normal, 3 );
	}

	internal override void OnImpact( IDamageable direct, Vector3 position, Vector3 normal )
	{
		if ( Sandbox.Mounting.Directory.Get( "hl1" ) is HalfLifeMount mount )
		{
			var pitch = Game.Random.Float( 0.9f, 1.1f );
			mount.NpcAudio.Emit( "bullchicken/bc_acid1.wav", position, pitch: pitch );
			mount.NpcAudio.Emit( "bullchicken/bc_spithit", position, pitch: pitch );
		}
		Impact( position, normal, direct is null );
		var center = position + normal;
		Blast.DamageInRadius( GameObject, Shooter, center, Damage, 250, "acid" );
	}

	[Rpc.Broadcast( NetFlags.HostOnly )]
	static void Impact( Vector3 position, Vector3 normal, bool mark )
	{
		Spray( position, normal, 24 );
		if ( mark ) ImpactDecal.Place( position, normal, "mommablob", 0 );
	}

	static void Spray( Vector3 position, Vector3 direction, int count )
		=> SpriteSpray.Emit( "Mortar spray", "mommaspout", position, direction, count, 130, 0.8f );
}
