namespace GoldSrc.HalfLife;

internal static partial class Blast
{
	internal static void Detonate( GameObject inflictor, GameObject owner, Vector3 contact, Vector3 normal, float damage, bool hit )
	{
		var origin = hit ? contact + normal * ((damage - 24) * 0.6f) : contact;
		DamageInRadius( inflictor, owner, origin, damage, damage * 2.5f, "blast" );
		ShowExplosion( origin, contact, normal, damage, hit );
		if ( Sandbox.Mounting.Directory.Get( "hl1" ) is HalfLifeMount mount )
		{
			mount.NpcAudio.Emit( $"weapons/explode{Game.Random.Int( 3, 5 )}.wav", origin, attenuation: 0.3f );
			mount.NpcAudio.Emit( "weapons/debris", origin, volume: 0.55f );
		}
	}

	[Rpc.Broadcast( NetFlags.HostOnly )]
	static async void ShowExplosion( Vector3 origin, Vector3 contact, Vector3 normal, float damage, bool hit )
	{
		var scene = Game.ActiveScene;
		bool underwater = WaterBody.Find( scene, origin ).IsValid();
		if ( !underwater )
		{
			var count = Game.Random.Int( 0, 3 );
			for ( int i = 0; i < count; ++i ) SparkShower.Spawn( origin, hit ? normal : Vector3.Zero );
		}
		if ( hit ) ImpactDecal.Place( contact, normal, $"scorch{Game.Random.Int( 1, 2 )}", 48 );
		var flash = SpriteEffect( "Grenade explosion", underwater ? "wxplo1" : "zerogxplode", origin, (damage - 50) * 0.06f, 15 );
		if ( flash.IsValid() )
		{
			var light = flash.AddComponent<PointLight>();
			light.LightColor = new Color( 1, 0.72f, 0.36f );
			light.Radius = 200;
			light.Shadows = false;
			await GameTask.DelaySeconds( 0.1f );
			if ( light.IsValid() ) light.Destroy();
			await GameTask.DelaySeconds( 0.2f );
		}
		if ( !scene.IsValid() ) return;
		if ( underwater ) Bubbles( origin, 64, 100, "Explosion bubbles" );
		else SpriteEffect( "Explosion smoke", "steam1", origin, (damage - 50) * 0.08f, 12 );
	}

	[Rpc.Broadcast( NetFlags.HostOnly )]
	internal static void Smoke( Vector3 origin, float scale ) => SpriteEffect( "Aircraft smoke", "steam1", origin, scale, 12 );

	internal static GameObject SpriteEffect( string name, string asset, Vector3 origin, float scale, float fps )
	{
		if ( Sandbox.Mounting.Directory.Get( "hl1" ) is not HalfLifeMount mount || mount.GetIndexedSprite( asset ) is not { } sprite ) return null;
		var animation = sprite.Animations[0];
		var image = animation.Frames[0].Texture;
		var go = new GameObject( false, name );
		go.NetworkMode = NetworkMode.Never;
		go.WorldPosition = origin;
		var renderer = go.AddComponent<SpriteRenderer>();
		renderer.Sprite = sprite;
		renderer.Size = new Vector2( image.Width, image.Height ) * scale;
		renderer.PlaybackSpeed = fps / animation.FrameRate;
		renderer.Additive = true;
		go.AddComponent<TemporaryEffect>().DestroyAfterSeconds = animation.Frames.Count / fps;
		go.Enabled = true;
		return go;
	}

	internal static void Bubbles( Vector3 origin, float radius, int count, string name )
	{
		if ( Sandbox.Mounting.Directory.Get( "hl1" ) is not HalfLifeMount mount || mount.GetIndexedSprite( "bubble" ) is not { } sprite ) return;
		var go = new GameObject( false, name );
		go.NetworkMode = NetworkMode.Never;
		go.WorldPosition = origin;
		var effect = go.AddComponent<ParticleEffect>();
		effect.Lifetime = 1;
		effect.InitialScale = 2;
		go.AddComponent<ParticleSpriteRenderer>().Sprite = sprite;
		go.AddComponent<TemporaryEffect>().DestroyAfterSeconds = 1.1f;
		go.Enabled = true;
		for ( int i = 0; i < count; i++ )
		{
			var bubble = effect.Emit( origin + Vector3.Random * radius, 0 );
			if ( bubble is not null ) bubble.Velocity = Vector3.Up * Game.Random.Float( 32, 64 ) + Vector3.Random * 8;
		}
	}
}
