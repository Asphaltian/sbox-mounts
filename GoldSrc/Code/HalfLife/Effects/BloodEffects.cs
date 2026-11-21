namespace GoldSrc.HalfLife;

[Library]
internal static class BloodEffects
{
	internal static void Wound( GameObject victim, Npc.BloodColor color, DamageInfo info, float damage )
	{
		if ( color == Npc.BloodColor.None || !Npc.HitStudioModel( info ) || damage < 1 ) return;

		var direction = (info.Position - info.Origin).Normal;
		Spray( color, info.Position, direction, (int)damage );
		var tags = info.Tags;
		if ( !(tags.Has( "bullet" ) || tags.Has( "slash" ) || tags.Has( "melee" ) || tags.Has( "club" )
			|| tags.Has( "crush" ) || tags.Has( "blast" ) || tags.Has( "explosion" ) || tags.Has( "mortar" )) ) return;

		var (traces, noise) = damage < 10 ? (1, 0.1f) : damage < 25 ? (2, 0.2f) : (4, 0.3f);
		for ( int i = 0; i < traces; i++ )
		{
			var scatter = new Vector3( Game.Random.Float( -noise, noise ), Game.Random.Float( -noise, noise ), Game.Random.Float( -noise, noise ) );
			var surface = victim.Scene.Trace.Ray( info.Position, info.Position + (direction + scatter) * 172 )
				.IgnoreGameObjectHierarchy( victim ).WithoutTags( "playercontroller", "npc", "player", "trigger", "ragdoll" ).Run();
			if ( surface.Hit ) Decal( color, surface.EndPosition, surface.Normal, Game.Random.Int( 1, 6 ) );
		}
	}

	[Rpc.Broadcast( NetFlags.HostOnly )]
	internal static void Decal( Npc.BloodColor color, Vector3 point, Vector3 normal, int variant )
	{
		var prefix = color == Npc.BloodColor.Red ? "blood" : "yblood";
		ImpactDecal.Place( point, normal, $"{prefix}{variant}", 0 );
	}

	[Rpc.Broadcast( NetFlags.HostOnly )]
	internal static void Spray( Npc.BloodColor color, Vector3 point, Vector3 direction, int damage )
	{
		if ( Sandbox.Mounting.Directory.Get( "hl1" ) is not HalfLifeMount mount ) return;
		var size = System.Math.Clamp( damage * (Connection.All.Count > 1 ? 2 : 1) / 10, 3, 16 );
		var tint = mount.PaletteColor( (int)color );
		if ( mount.GetIndexedSprite( "bloodspray" ) is { } mist )
		{
			var go = new GameObject( false, "Blood spray" );
			go.NetworkMode = NetworkMode.Never;
			go.WorldPosition = point;
			var sprite = go.AddComponent<SpriteRenderer>();
			sprite.Sprite = mist;
			sprite.Color = tint;
			sprite.Size = size * 6.4f;
			go.AddComponent<TemporaryEffect>().DestroyAfterSeconds = 0.5f;
			go.Enabled = true;
		}
		Droplets( mount, point, direction, tint, size * 2, size );
	}

	[Rpc.Broadcast( NetFlags.HostOnly )]
	internal static void Stream( Npc.BloodColor color, Vector3 point, Vector3 direction, float damage )
	{
		if ( Sandbox.Mounting.Directory.Get( "hl1" ) is HalfLifeMount mount )
			Droplets( mount, point, direction, mount.PaletteColor( (int)color ), System.Math.Clamp( (int)damage, 1, 100 ), 3 );
	}

	static void Droplets( HalfLifeMount mount, Vector3 point, Vector3 direction, Color tint, int count, float size )
	{
		if ( mount.GetIndexedSprite( "blood" ) is not { } drops ) return;
		var go = new GameObject( false, "Blood droplets" );
		go.NetworkMode = NetworkMode.Never;
		go.WorldPosition = point;
		var particles = go.AddComponent<ParticleEffect>();
		particles.Lifetime = 0.7f;
		particles.InitialScale = size;
		particles.ApplyColor = true;
		particles.Tint = tint;
		particles.Force = true;
		particles.ForceDirection = Vector3.Down;
		particles.ForceScale = 300;
		go.AddComponent<ParticleSpriteRenderer>().Sprite = drops;
		go.AddComponent<TemporaryEffect>().DestroyAfterSeconds = 0.8f;
		go.Enabled = true;
		for ( int i = 0; i < count; ++i )
		{
			var drop = particles.Emit( point, 0 );
			if ( drop is not null ) drop.Velocity = direction * Game.Random.Float( 40, 110 ) + Vector3.Random * 48;
		}
	}
}
