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
		ExplosionFlash( "Grenade explosion", origin, (damage - 50) * 0.06f, underwater );
		await GameTask.DelaySeconds( 0.3f );
		if ( !scene.IsValid() ) return;
		if ( underwater ) Bubbles( origin, 64, 100, "Explosion bubbles" );
		else SmokePuff( "Explosion smoke", origin, (damage - 50) * 0.08f );
	}

	internal static void ExplosionFlash( string name, Vector3 origin, float scale, bool underwater )
	{
		var flash = SpriteEffect( name, underwater ? "wxplo1" : "zerogxplode", origin + Vector3.Up * 10, scale, 15 );
		if ( flash.IsValid() ) flash.GetComponent<SpriteRenderer>().Color = Color.White.WithAlpha( 180f / 255 );
		ExplosionParticles.Spawn( origin, flash );
	}

	[Rpc.Broadcast( NetFlags.HostOnly )]
	internal static void Smoke( Vector3 origin, float scale ) => SmokePuff( "Aircraft smoke", origin, scale );

	internal static void SmokePuff( string name, Vector3 origin, float scale )
	{
		var shade = (byte)Game.Random.Int( 20, 35 );
		var puff = SpriteEffect( name, "steam1", origin + Vector3.Up * 20, scale, 12, additive: false );
		if ( !puff.IsValid() ) return;
		puff.GetComponent<SpriteRenderer>().Color = new Color32( shade, shade, shade, 255 );
		puff.AddComponent<RisingSmoke>();
	}

	internal static GameObject SpriteEffect( string name, string asset, Vector3 origin, float scale, float fps, bool additive = true )
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
		renderer.Additive = additive;
		go.AddComponent<TemporaryEffect>().DestroyAfterSeconds = animation.Frames.Count / fps;
		go.Enabled = true;
		return go;
	}

	internal static void Bubbles( Vector3 origin, float radius, int count, string name )
	{
		var effect = CreateBubbles( origin, name );
		if ( !effect.IsValid() ) return;
		for ( int i = 0; i < count; i++ )
		{
			var bubble = effect.Emit( origin + Vector3.Random * radius, 0 );
			if ( bubble is not null ) bubble.Velocity = Vector3.Up * Game.Random.Float( 32, 64 ) + Vector3.Random * 8;
		}
	}

	internal static void BulletBubbles( Scene scene, Vector3 start, Vector3 end )
	{
		var count = System.Math.Min( 255, (int)(start.Distance( end ) / 64) );
		if ( count < 1 ) return;
		var water = WaterBody.Find( scene, start );
		if ( !water.IsValid() || !water.Contains( start + Vector3.Up * 8 ) )
		{
			water = WaterBody.Find( scene, end );
			if ( !water.IsValid() || !water.Contains( end + Vector3.Up * 8 ) ) return;
		}
		var effect = CreateBubbles( start, "Bullet bubbles" );
		if ( !effect.IsValid() ) return;
		for ( int i = 0; i < count; i++ )
		{
			var point = Vector3.Lerp( start, end, (i + Game.Random.Float( 0, 1 )) / count );
			if ( water.Contains( point ) && effect.Emit( point, 0 ) is { } bubble )
				bubble.Velocity = Vector3.Up * Game.Random.Float( 8, 16 );
		}
	}

	static ParticleEffect CreateBubbles( Vector3 origin, string name )
	{
		if ( Sandbox.Mounting.Directory.Get( "hl1" ) is not HalfLifeMount mount || mount.GetIndexedSprite( "bubble" ) is not { } sprite ) return null;
		var go = new GameObject( false, name );
		go.NetworkMode = NetworkMode.Never;
		go.WorldPosition = origin;
		var effect = go.AddComponent<ParticleEffect>();
		effect.Lifetime = 1;
		effect.InitialScale = 2;
		go.AddComponent<ParticleSpriteRenderer>().Sprite = sprite;
		go.AddComponent<TemporaryEffect>().DestroyAfterSeconds = 1.1f;
		go.Enabled = true;
		return effect;
	}
}
