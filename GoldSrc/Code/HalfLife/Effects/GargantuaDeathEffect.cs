namespace GoldSrc.HalfLife;

[Hide]
public sealed class GargantuaDeathEffect : Component
{
	TimeSince _age;
	int _explosions, _streaks;
	bool _burst;
	ParticleEffect _sparks;
	HalfLifeMount _mount;

	[Rpc.Broadcast( NetFlags.HostOnly )]
	internal static void Begin( Transform transform )
	{
		var go = new GameObject( false, "Gargantua death effects" );
		go.WorldTransform = transform;
		go.NetworkMode = NetworkMode.Never;
		go.AddComponent<GargantuaDeathEffect>();
		go.Enabled = true;
	}

	protected override void OnStart()
	{
		_mount = Sandbox.Mounting.Directory.Get( "hl1" ) as HalfLifeMount;
		if ( _mount is null ) { GameObject.Destroy(); return; }
		_age = 0;
		_sparks = GameObject.AddComponent<ParticleEffect>();
		_sparks.InitialScale = 1.5f;
		_sparks.Lifetime = 0.35f;
		_sparks.ApplyColor = true;
		var renderer = GameObject.AddComponent<ParticleSpriteRenderer>();
		renderer.Sprite = _mount.GetIndexedSprite( "glow01" );
		renderer.VelocityStretch = true;
		renderer.StretchTime = 0.045f;
		renderer.Additive = true;
	}

	protected override void OnUpdate()
	{
		while ( _streaks < 15 && _age >= (_streaks + 1) * 0.1f )
		{
			var fraction = _streaks++ / 15f;
			var radial = Rotation.FromYaw( fraction * 360 * 8 ).Forward;
			var point = WorldPosition + WorldRotation.Forward * 100 + radial * (125 * fraction) + Vector3.Up * (214 * 0.6f * fraction);
			Streaks( point, 20 );
		}
		while ( _explosions < 4 && _age >= _explosions * 0.6f ) Explosion( _explosions++ );
		if ( !_burst && _age >= 1.6f )
		{
			_burst = true;
			BreakModel();
		}
		if ( _age < 2.5f ) return;
		Blast.SpriteEffect( "Gargantua smoke", "steam1", WorldPosition, 4.6f, 12 );
		GameObject.Destroy();
	}

	void Streaks( Vector3 point, int count )
	{
		var tint = _mount.PaletteColor( Game.Random.Int( 8, 11 ) );
		var speed = Game.Random.Float( 50, 150 );
		for ( int i = 0; i < count; i++ )
		{
			var particle = _sparks.Emit( point, 0 );
			if ( particle is null ) continue;
			particle.Color = tint;
			particle.Velocity = Vector3.Up * speed + new Vector3( Game.Random.Float( -400, 400 ), Game.Random.Float( -400, 400 ), Game.Random.Float( -400, 400 ) );
		}
	}

	async void Explosion( int index )
	{
		var magnitude = 60 + index * 40;
		var scale = System.MathF.Max( 1, (magnitude - 50) * 0.06f );
		var point = WorldPosition + new Vector3( Game.Random.Float( -70, 70 ), Game.Random.Float( -70, 70 ), 32 + index * 15 );
		var hit = Scene.Trace.Ray( point + Vector3.Up * 8, point + Vector3.Down * 32 ).WithoutTags( "npc", "player", "debris", "ragdoll", "trigger" ).Run();
		if ( hit.Hit )
		{
			ImpactDecal.Place( hit.EndPosition, hit.Normal, $"scorch{Game.Random.Int( 1, 2 )}", 48 );
			point = hit.EndPosition + hit.Normal * ((magnitude - 24) * 0.6f);
		}
		Blast.SpriteEffect( "Gargantua explosion", "zerogxplode", point, scale, 15 );
		NpcAudio.Play( _mount.NpcAudio.Find( $"weapons/explode{Game.Random.Int( 3, 5 )}.wav" ), point );
		Streaks( point, Game.Random.Int( 0, 3 ) );
		await GameTask.DelaySeconds( 0.3f );
		if ( IsValid ) Blast.SpriteEffect( "Gargantua explosion smoke", "steam1", point, scale, 12 );
	}

	void BreakModel()
	{
		var model = Model.Load( "mount://hl1/valve/models/metalplategibs.mdl.vmdl" );
		if ( !model.IsValid() || model.IsError || model.Parts.All.FirstOrDefault() is not { } part ) return;
		for ( int i = 0; i < 10; i++ )
		{
			var velocity = new Vector3( Game.Random.Float( -1, 1 ), Game.Random.Float( -1, 1 ), Game.Random.Float( 0, 1 ) ) * Game.Random.Float( 300, 500 );
			Gib.Spawn( model, part.Name, 0, WorldPosition, velocity, Npc.BloodColor.Yellow, 1.25f );
		}
		var extent = new Vector3( 100, 100, 64 );
		var hull = new BBox( -extent, extent ) + WorldPosition;
		for ( int i = 0; i < 50; i++ )
		{
			var velocity = Vector3.Random * Game.Random.Float( 100, 200 );
			Gib.Spawn( model, part.Name, Game.Random.Int( 0, part.Choices.Count - 1 ), hull.RandomPointInside, velocity, Npc.BloodColor.None, 2f );
		}
	}
}
