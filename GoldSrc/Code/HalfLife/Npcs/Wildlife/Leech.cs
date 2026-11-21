namespace GoldSrc.HalfLife;

[Title( "Leech" ), Category( "Half-Life" )]
[Alias( "GoldSrc.HalfLifeLeech" )]
[NpcDefinition( "monster_leech", "leech", MonsterClass.Insect, 2, 1, 2, 50, Attack = NpcAttack.Melee, Damage = 2, Range = 40, Movement = NpcMovement.Swimming, IdleSequence = "hover" )]
public sealed class Leech : SwimmingNpc
{
	internal override float ViewDot => -0.5f;
	internal override float SightRange => 750;

	internal override NpcSounds Sounds { get; } = new( Alert: "leech/leech_alert", Attack: "leech/leech_bite" );
	static readonly string[] Clips = ["swim", "swim2", "attack", "hover", "leftturn", "rightturn"];
	internal override string[] ExtraActions => Clips;
	[Sync( SyncFlags.FromHost )] string SwimClip { get; set; }
	[Sync( SyncFlags.FromHost )] float SwimRate { get; set; } = 1;
	TimeUntil _switchTime, _nextSound, _nextMotionSample;
	float _smoothedSpeed;
	float _lastYaw;

	internal override void OnModelLoaded( ModelLoader loader )
	{
		base.OnModelLoaded( loader );
		EyeHeight = 0;
		_lastYaw = WorldRotation.Yaw();
		_switchTime = Game.Random.Float( 1, 5 );
	}

	internal override StudioSequence MovementSequence( float speed )
	{
		var yaw = WorldRotation.Yaw();
		var turn = new Angles( 0, yaw - _lastYaw, 0 ).Normal.yaw;
		_lastYaw = yaw;
		if ( Stranded || System.MathF.Abs( turn ) > 150 * Time.Delta )
		{
			if ( Networking.IsHost ) SwimClip = null;
			return Find( Stranded ? "hover" : turn > 0 ? "leftturn" : "rightturn" );
		}
		if ( Networking.IsHost && SwimClip is null ) SwimClip = WeightedActivity( Swim )?.Name;
		return Find( SwimClip ) ?? Find( "swim" );
	}

	internal override float MovementPlaybackRate( StudioSequence sequence, float speed ) => Stranded ? 1 : SwimRate;

	protected override void Think()
	{
		if ( _nextMotionSample <= 0 )
		{
			_nextMotionSample = 0.1f;
			var speed = Animation.MoveVelocity.Length;
			SwimRate = System.MathF.Min( 1.9f, 1 + System.MathF.Abs( speed - _smoothedSpeed ) / 10 );
			_smoothedSpeed += (speed - _smoothedSpeed) * 0.2f;
		}
		base.Think();
	}

	protected override Sandbox.Npcs.Relationship GetFallbackRelationship( GameObject other ) =>
		ClassOf( other ) == MonsterClass.Player ? new( Sandbox.Npcs.Disposition.Hostile ) : base.GetFallbackRelationship( other );

	internal override GameObject SelectPrey()
	{
		if ( _switchTime > 0 && (Prey is null || CanHunt( Prey )) ) return Prey;
		var enemy = Prey is null ? base.SelectPrey() : null;
		_switchTime = enemy.IsValid() ? Game.Random.Float( 18, 25 ) : Game.Random.Float( 3, 6 );
		return enemy;
	}

	internal override bool Hunt( GameObject target )
	{
		var eye = TargetEyePosition( target );
		var facing = Vector3.Dot( WorldRotation.Forward.WithZ( 0 ).Normal, (eye - WorldPosition).WithZ( 0 ).Normal );
		SwimTo( eye + Vector3.Down * 5, facing >= System.MathF.Cos( 67.5f.DegreeToRadian() ) ? 100 : 50, target );
		if ( WorldPosition.Distance( eye ) < Definition.Range && Find( "attack" ) is { } attack )
		{
			SwimClip = null;
			PlaySequence( attack, false, SwimRate );
		}
		return true;
	}

	internal override void HandleActionEvent( int id )
	{
		if ( id != 1 ) return;
		if ( _nextSound <= 0 )
		{
			EmitSound( Sounds.Attack, WorldPosition );
			_nextSound = 0.5f;
		}
		_switchTime = (float)_switchTime - 2;
		if ( !CanHunt( Prey ) ) return;
		var delta = (Prey.WorldPosition - WorldPosition).WithZ( 0 );
		if ( WorldRotation.Forward.WithZ( 0 ).Normal.Dot( delta.Normal ) > 0.9f )
			DealDamage( Prey, WorldPosition, Prey.WorldPosition, "slash" );
	}

	internal override NpcCorpse AddCorpseComponent( GameObject go ) => go.AddComponent<DeadLeech>();
	protected override GameObject CreateCorpse() => NpcCorpse.Spawn( this, Find( Stranded ? "death_end" : "death" ) );
}
