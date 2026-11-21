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
	{
		if ( Sandbox.Mounting.Directory.Get( "hl1" ) is not HalfLifeMount mount || mount.GetIndexedSprite( "mommaspout" ) is not { } sprite ) return;
		var go = new GameObject( false, "Mortar spray" );
		go.NetworkMode = NetworkMode.Never;
		go.WorldPosition = position;
		var effect = go.AddComponent<ParticleEffect>();
		effect.Lifetime = 0.4f;
		effect.InitialScale = 8;
		go.AddComponent<ParticleSpriteRenderer>().Sprite = sprite;
		go.Enabled = true;
		for ( int i = 0; i < count; ++i )
		{
			var particle = effect.Emit( position, 0 );
			if ( particle is not null ) particle.Velocity = (direction + Vector3.Random * 0.8f).Normal * 130;
		}
		go.AddComponent<TemporaryEffect>().DestroyAfterSeconds = 0.5f;
	}
}
