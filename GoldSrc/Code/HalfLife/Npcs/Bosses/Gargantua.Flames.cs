namespace GoldSrc.HalfLife;

public sealed partial class Gargantua
{
	[Sync( SyncFlags.FromHost )] bool Flaming { get; set; }
	[Sync( SyncFlags.FromHost )] Vector3 LeftEnd { get; set; }
	[Sync( SyncFlags.FromHost )] Vector3 RightEnd { get; set; }
	TimeUntil _nextFlame, _endFlame, _nextBurn, _nextScorch;
	readonly List<BeamEffect> _flames = [];
	SoundHandle _flameSound;

	internal override StudioSequence RestSequence => Flaming ? Find( "shootflames2" ) : base.RestSequence;

	void StartFlames()
	{
		Flaming = true;
		_endFlame = 4.5f;
		_nextFlame = 6;
		_nextBurn = 0;
		EmitSound( "garg/gar_flameon1.wav", WorldPosition );
		Burn();
	}

	protected override void Think()
	{
		EnemyVisible = Senses?.VisibleHostiles.Count > 0;
		if ( !EnemyVisible ) _nextStomp = 5;
		if ( !Flaming )
		{
			base.Think();
			return;
		}
		if ( _endFlame <= 0 || !_enemy.IsValid() || _enemy.WorldPosition.Distance( WorldPosition ) > 400 )
		{
			Flaming = false;
			EmitSound( "garg/gar_flameoff1.wav", WorldPosition );
			return;
		}
		if ( _nextBurn <= 0 ) Burn();
	}

	void Burn()
	{
		_nextBurn = 0.1f;
		var target = _enemy.GetBounds().Center;
		LeftEnd = BurnFrom( "attachment2", target );
		RightEnd = BurnFrom( "attachment3", target );
		if ( _nextScorch <= 0 ) _nextScorch = 0.25f;
	}

	Vector3 BurnFrom( string hand, Vector3 aimPoint )
	{
		var origin = Renderer.GetAttachment( hand )?.Position ?? WorldPosition + Vector3.Up * 64;
		var aim = (aimPoint - origin).Normal;
		var trace = Scene.Trace.Ray( origin, origin + aim * 330 ).IgnoreGameObjectHierarchy( GameObject ).UseHitboxes().Run();
		if ( trace.Hit && _nextScorch <= 0 && trace.GameObject?.GetComponentInParent<IDamageable>() is null )
			Scorch( trace.EndPosition, trace.Normal, Game.Random.Int( 1, 3 ) );

		var length = trace.EndPosition.Distance( origin );
		var center = (origin + trace.EndPosition) * 0.5f;
		var victims = Scene.FindInPhysics( new Sphere( center, length * 0.5f + 78 ) )
			.Select( obj => obj.GetComponentInParent<IDamageable>() ).OfType<Component>().Distinct().ToArray();
		foreach ( var victim in victims )
		{
			if ( !victim.IsValid() || victim.GameObject == GameObject || victim is Npc npc && npc.Classification == MonsterClass.AlienMonster ) continue;
			var target = victim.GameObject.GetBounds().Center;
			var near = origin + aim * System.Math.Clamp( Vector3.Dot( target - origin, aim ), 0, length );
			var damage = 5 - System.MathF.Max( target.Distance( near ) - 64, 0 ) * 0.4f;
			if ( damage <= 0 ) continue;
			var obstruction = Scene.Trace.Ray( near, target ).IgnoreGameObjectHierarchy( GameObject ).Run();
			if ( obstruction.Hit && obstruction.GameObject?.Root != victim.GameObject.Root ) continue;
			var info = new DamageInfo { Damage = damage, Origin = origin, Position = target, Attacker = GameObject, Weapon = GameObject };
			info.Tags.Add( "burn" );
			((IDamageable)victim).OnDamage( info );
		}
		return trace.EndPosition;
	}

	[Rpc.Broadcast( NetFlags.HostOnly )]
	static void Scorch( Vector3 position, Vector3 normal, int variant ) => ImpactDecal.Place( position, normal, $"smscorch{variant}", 24 );

	protected override void OnUpdate()
	{
		base.OnUpdate();
		UpdateEye();
		if ( !Flaming )
		{
			if ( _flames.Count > 0 ) StopFlameEffects();
			return;
		}
		if ( _flames.Count == 0 ) CreateFlameEffects();
		for ( int i = 0; i < _flames.Count; ++i )
		{
			var effect = _flames[i];
			var end = i % 2 == 0 ? LeftEnd : RightEnd;
			effect.TargetPosition = i < 2 ? end : Vector3.Lerp( effect.WorldPosition, end, 0.6f );
		}
		if ( _flameSound.IsValid() ) _flameSound.Position = WorldPosition;
	}

	void CreateFlameEffects()
	{
		if ( Sandbox.Mounting.Directory.Get( "hl1" ) is not HalfLifeMount mount ) return;
		var sprite = mount.GetIndexedSprite( "xbeam3" );
		for ( int i = 0; i < 4; ++i )
		{
			var hand = Renderer.GetAttachmentObject( i % 2 == 0 ? "attachment2" : "attachment3" );
			var go = new GameObject( hand.IsValid() ? hand : GameObject, false, "Gargantua flame" );
			go.NetworkMode = NetworkMode.Never;
			go.LocalPosition = Vector3.Zero;
			var effect = go.AddComponent<BeamEffect>();
			effect.BeamLifetime = 5;
			effect.Scale = i < 2 ? 24 : 14;
			Color color = i < 2 ? new Color32( 255, 130, 90 ) : new Color32( 0, 120, 255 );
			effect.BeamColor = color;
			effect.Brightness = 0.75f;
			effect.TextureScrollSpeed = 20;
			effect.Additive = true;
			if ( sprite is not null ) effect.Texture = sprite.Animations[0].Frames[0].Texture;
			go.Enabled = true;
			_flames.Add( effect );
		}
		if ( mount.NpcAudio.Find( "garg/gar_flamerun1.wav" ) is { } path && SoundFile.Load( path ) is { } sound )
			_flameSound = Sound.PlayFile( sound );
	}

	void StopFlameEffects()
	{
		foreach ( var flame in _flames )
			if ( flame.IsValid() ) flame.GameObject.Destroy();
		_flames.Clear();
		_flameSound?.Stop();
	}

	protected override void OnDisabled()
	{
		StopFlameEffects();
		base.OnDisabled();
	}
}
