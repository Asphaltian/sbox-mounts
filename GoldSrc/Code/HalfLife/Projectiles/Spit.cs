namespace GoldSrc.HalfLife;

[Hide]
public sealed class Spit : NpcProjectile
{
	internal override float LaunchSpeed => 900;
	// GoldSrc applies generic impact damage, which can hurt acid-immune headcrabs.
	internal override string HitTag => "generic";

	protected override void OnStart()
	{
		if ( Sandbox.Mounting.Directory.Get( "hl1" ) is HalfLifeMount mount && mount.GetIndexedSprite( "bigspit" ) is { } spit )
		{
			var renderer = GameObject.AddComponent<SpriteRenderer>();
			renderer.Sprite = spit;
			renderer.Size = 16;
		}
	}

	internal override void OnImpact( IDamageable victim, Vector3 position, Vector3 normal )
	{
		base.OnImpact( victim, position, normal );
		if ( Sandbox.Mounting.Directory.Get( "hl1" ) is not HalfLifeMount mount ) return;
		var pitch = Game.Random.Float( 0.9f, 1.1f );
		mount.NpcAudio.Emit( "bullchicken/bc_acid1.wav", position, pitch: pitch );
		mount.NpcAudio.Emit( $"bullchicken/bc_spithit{Game.Random.Int( 1, 2 )}.wav", position, pitch: pitch );
		if ( victim is null )
		{
			Spray( position, normal, 5, 30, 0.8f );
			PlaceMark( position, normal, Game.Random.Int( 1, 2 ) );
		}
	}

	[Rpc.Broadcast( NetFlags.HostOnly )]
	internal static void Spray( Vector3 position, Vector3 normal, int count, float speed, float noise )
	{
		if ( Sandbox.Mounting.Directory.Get( "hl1" ) is HalfLifeMount mount && mount.GetIndexedSprite( "tinyspit" ) is { } fleck )
		{
			var spray = new GameObject( false, "Spit spray" );
			spray.NetworkMode = NetworkMode.Never;
			spray.WorldPosition = position;
			var effect = spray.AddComponent<ParticleEffect>();
			effect.Lifetime = 0.45f;
			effect.InitialScale = 4;
			spray.AddComponent<ParticleSpriteRenderer>().Sprite = fleck;
			spray.Enabled = true;
			for ( var i = 0; i < count; ++i )
			{
				var particle = effect.Emit( position, 0f );
				if ( particle is not null )
					particle.Velocity = (normal + Vector3.Random * noise).Normal * speed;
			}
			spray.AddComponent<TemporaryEffect>().DestroyAfterSeconds = 0.6f;
		}
	}

	[Rpc.Broadcast( NetFlags.HostOnly )]
	static void PlaceMark( Vector3 position, Vector3 normal, int variant ) => ImpactDecal.Place( position, normal, $"spit{variant}", 32 );
}
