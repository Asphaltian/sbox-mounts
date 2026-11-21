namespace GoldSrc.HalfLife;

internal static class StudioSparks
{
	internal static void Emit( Vector3 position, int count )
	{
		if ( count <= 0 || Sandbox.Mounting.Directory.Get( "hl1" ) is not HalfLifeMount mount ||
			mount.GetIndexedSprite( "glow01" ) is not { } sprite ) return;

		var go = new GameObject( false, "Studio sparks" );
		go.NetworkMode = NetworkMode.Never;
		go.WorldPosition = position;
		var sparks = go.AddComponent<ParticleEffect>();
		sparks.Lifetime = 0.3f;
		sparks.InitialScale = 1;
		sparks.ApplyColor = true;
		sparks.Tint = new Color( 1, 0.7f, 0.25f );
		sparks.Force = true;
		sparks.ForceDirection = Vector3.Down;
		sparks.ForceScale = 400;
		var renderer = go.AddComponent<ParticleSpriteRenderer>();
		renderer.Sprite = sprite;
		renderer.VelocityStretch = true;
		renderer.StretchTime = 0.035f;
		renderer.Additive = true;
		go.AddComponent<TemporaryEffect>().DestroyAfterSeconds = 0.35f;
		go.Enabled = true;
		count = System.Math.Min( count, sparks.MaxParticles );
		for ( int i = 0; i < count; i++ )
		{
			if ( sparks.Emit( position, 0 ) is { } spark )
				spark.Velocity = new Vector3( Game.Random.Float( -100, 100 ), Game.Random.Float( -100, 100 ), Game.Random.Float( -100, 100 ) );
		}
	}
}
