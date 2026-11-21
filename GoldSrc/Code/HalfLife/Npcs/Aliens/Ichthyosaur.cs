namespace GoldSrc.HalfLife;

[Title( "Ichthyosaur" ), Category( "Half-Life" )]
[Alias( "GoldSrc.HalfLifeIchthyosaur" )]
[NpcDefinition( "monster_ichthyosaur", "icky", MonsterClass.AlienMonster, 200, 32, 64, 150, Attack = NpcAttack.Melee, Damage = 35, Range = 80, Movement = NpcMovement.Swimming, IdleSequence = "idle", Gibs = GibSet.Alien, Blood = BloodColor.Yellow )]
public sealed class Ichthyosaur : SwimmingNpc
{
	internal override float ViewDot => -0.7f;

	internal override NpcSounds Sounds { get; } = new( "ichy/ichy_idle", "ichy/ichy_alert", "ichy/ichy_pain", "ichy/ichy_die", "ichy/ichy_bite" );
	static readonly string[] Clips = ["swim", "thrust", "bite_r", "bite_l", "srattack1"];
	internal override string[] ExtraActions => Clips;
	[Property, Sync( SyncFlags.FromHost )] public bool Aggressive { get; set; }
	float _distance = 384;
	float _speed = 150;
	TimeUntil _nextAlert, _blink, _contactExpires;
	GameObject _lastContact;
	bool _chasing;
	int _eye = -1;

	void Call( string sample ) => EmitSound( VoiceSample( sample ), WorldPosition, pitch: VoicePitch, speaker: this, attenuation: 0.6f );
	internal override void SwimAlert()
	{
		_chasing = false;
		Call( Sounds.Alert );
	}
	internal override float VoicePitch => Game.Random.Int( 95, 105 ) / 100f;
	internal override void PainSound() => Call( Sounds.Pain );
	internal override void DeathSound() => Call( Sounds.Death );
	internal override void IdleSound() => Call( Sounds.Idle );

	internal override string VoiceSample( string sample ) => sample switch
	{
		"ichy/ichy_alert" => $"ichy/ichy_alert{Game.Random.Int( 2, 3 )}.wav",
		"ichy/ichy_pain" => $"ichy/ichy_pain{Game.Random.Int( 0, 2 ) switch { 0 => 2, 1 => 3, _ => 5 }}.wav",
		"ichy/ichy_die" => Game.Random.Int( 0, 1 ) == 0 ? "ichy/ichy_die2.wav" : "ichy/ichy_die4.wav",
		_ => sample
	};

	internal override Vector3 StrandedJump()
	{
		WorldRotation = Rotation.FromYaw( WorldRotation.Yaw() + Game.Random.Float( -45, 45 ) );
		return WorldRotation.Forward * 200 + Vector3.Up * 200;
	}

	internal override void OnModelLoaded( ModelLoader loader )
	{
		base.OnModelLoaded( loader );
		EyeHeight = 16;
		_blink = Game.Random.Float( 3, 4 );
	}

	internal override StudioSequence MovementSequence( float speed ) => speed < 8 ? RestSequence : Find( speed >= 180 ? "thrust" : "swim" );

	internal override bool Hunt( GameObject target )
	{
		var center = TargetHullCenter( target );
		var delta = center - WorldPosition;
		var toward = delta.Normal;
		var nearest = target.GetComponent<Rigidbody>()?.FindClosestPoint( WorldPosition )
			?? target.GetComponentInChildren<Collider>()?.FindClosestPoint( WorldPosition ) ?? center;
		var contact = WorldPosition.DistanceSquared( nearest ) <= 34 * 34;
		if ( contact )
		{
			Aggressive = true;
			_lastContact = target;
			_contactExpires = 0.2f;
		}
		if ( Aggressive || (delta.Length <= 192 && _distance <= 192) )
		{
			_speed = System.MathF.Min( 400, _speed + 40 );
			SwimTo( center, _speed, target );
			if ( contact && Vector3.Dot( WorldRotation.Forward, toward ) >= 0.7f )
			{
				_chasing = false;
				Movement.Stop();
				PlayAction( Find( Clips[Game.Random.Int( 2, 4 )] ) );
			}
			else if ( !_chasing )
			{
				_chasing = true;
				Call( "ichy/ichy_attack" );
			}
			return true;
		}

		_chasing = false;
		var watched = Vector3.Dot( target.WorldRotation.Forward, -toward ) > 0.5f;
		_distance = (_distance + (watched ? 4 : -4)).Clamp( 128, 384 );
		_speed = (_speed + (watched ? -2 : 4)).Clamp( 80, 300 );
		var lateral = Vector3.Cross( toward, Vector3.Up ).Normal;
		if ( Vector3.Dot( Animation.MoveVelocity, lateral ) < 0 ) lateral = -lateral;
		SwimTo( center - toward * _distance + lateral * 32, _speed, target );
		if ( _nextAlert <= 0 )
		{
			SwimAlert();
			_nextAlert = Game.Random.Float( 3, 5 );
		}
		return true;
	}

	internal override void HandleActionEvent( int id )
	{
		if ( id is not (1 or 2) ) return;
		if ( IsLiveTarget( Prey ) && CanSeeTargetEyes( Prey, WaterBody.Find( Scene, EyePosition ).IsValid() ) )
		{
			var end = TargetBodyPosition( Prey );
			var delta = end - WorldPosition;
			if ( delta.Length > 80 && (_lastContact != Prey || _contactExpires <= 0) ) return;
			if ( WorldRotation.Forward.Dot( delta.Normal ) > 0.707f )
			{
				Aggressive = true;
				var push = -WorldRotation.Right * 300;
				if ( Prey.GetComponent<PlayerController>() is { } player )
					PunchPlayer( player, push, new Angles( 5, 0, -18 ), new Angles( Game.Random.Float( -35, 35 ), Game.Random.Float( -90, 90 ), 0 ) );
				else
					PushVictim( Prey.GetComponent<Component.IDamageable>() as Component, push );
				DealDamage( Prey, WorldPosition, end, "slash" );
			}
		}
		EmitSound( Sounds.Attack, WorldPosition, pitch: VoicePitch, attenuation: 0.6f );
		Bubbles( WorldPosition + WorldRotation.Forward * 32 );
	}

	[Rpc.Broadcast( NetFlags.HostOnly )]
	static void Bubbles( Vector3 mouth ) => Blast.Bubbles( mouth, 8, 16, "Ichthyosaur bubbles" );

	protected override void OnUpdate()
	{
		base.OnUpdate();
		if ( !Renderer.IsValid() || !Renderer.Animation.IsValid ) return;
		if ( _blink < -0.2f ) _blink = Game.Random.Float( 3, 4 );
		int eye = _blink <= 0 ? 2 : Aggressive ? 0 : 1;
		if ( eye != _eye ) Renderer.MaterialGroup = $"Skin {eye}";
		_eye = eye;
	}

	protected override void OnDamaged( Component.IDamageable.DamageEvent damage )
	{
		Aggressive = true;
		base.OnDamaged( damage );
	}

	internal override NpcCorpse AddCorpseComponent( GameObject go ) => go.AddComponent<DeadIchthyosaur>();
	protected override GameObject CreateCorpse() => NpcCorpse.Spawn( this, Find( "die1" ) );
}
