namespace GoldSrc.HalfLife;

[Title( "Headcrab" ), Category( "Half-Life" )]
[Alias( "GoldSrc.HalfLifeHeadcrab" )]
[NpcDefinition( "monster_headcrab", "headcrab", MonsterClass.AlienPrey, 10, 12, 24, 90, Attack = NpcAttack.Leap, Damage = 10, Range = 256, Gibs = GibSet.Alien, Blood = BloodColor.Yellow )]
public class Headcrab : Npc
{
	internal override NpcSounds Sounds { get; } = new( "headcrab/hc_idle", "headcrab/hc_alert1.wav", "headcrab/hc_pain", "headcrab/hc_die",
				"headcrab/hc_attack", "headcrab/hc_headbite" );

	internal override Vector3 HullCenter => WorldPosition + Vector3.Up * 6;
	internal override Vector3 BodyTarget => HullCenter;
	internal virtual float FacingThreshold => 0.65f;
	internal virtual bool PerchedOnActor => false;
	int _pounceAction = -1;
	GameObject _prey;
	TimeUntil _nextJump, _nextCooCheck;
	bool _biting;

	internal override void OnModelLoaded( ModelLoader loader )
	{
		EyeHeight = loader.EyePosition.z;
		if ( Networking.IsHost ) GameObject.GetOrAddComponent<HeadcrabBite>().Crab = this;
	}

	internal override void IdleMovement()
	{
		if ( Movement?.MoveTarget.HasValue == true ) Movement.Stop();
	}

	internal override void IdleSound() => EmitSound( $"{Sounds.Idle}{Game.Random.Int( 1, 3 )}.wav", WorldPosition, VoiceVolume, VoicePitch );
	internal override void OnEnemySpotted( GameObject target ) => EmitSound( Sounds.Alert, WorldPosition, VoiceVolume, VoicePitch );
	internal override void PainSound() => EmitSound( Sounds.Pain, WorldPosition, VoiceVolume, VoicePitch );

	protected override void Think()
	{
		base.Think();
		if ( !InCombat || _nextCooCheck > 0 ) return;
		_nextCooCheck = 0.1f;
		if ( Game.Random.Float( 0, 5 ) < 0.1f ) IdleSound();
	}

	internal override bool InAttackRange( GameObject target, Vector3 aim ) =>
		PerchedOnActor || WorldPosition.DistanceSquared( target.WorldPosition ) <= AttackRange * AttackRange;

	internal override void BeginAttack( GameObject target )
	{
		var towards = (target.WorldPosition - WorldPosition).WithZ( 0 ).Normal;
		if ( _nextJump > 0 || !IsOnGround || (!PerchedOnActor && WorldRotation.Forward.Dot( towards ) < FacingThreshold) ) return;
		if ( WeightedActivity( Ranged ) is not { } jump ) return;
		Movement?.Stop();
		_prey = target;
		EmitSound( "headcrab/hc_attack1.wav", WorldPosition, VoiceVolume, VoicePitch );
		PlayAction( jump );
		_pounceAction = ActionSerial;
	}

	internal override void HandleActionEvent( int id )
	{
		if ( id != 2 ) return;
		WorldPosition += Vector3.Up;
		var prey = _pounceAction == ActionSerial ? _prey : FindVisibleEnemy();
		var velocity = WorldRotation.Forward.WithZ( WorldRotation.Up.z ) * 350;
		if ( prey.IsValid() )
		{
			var gravity = System.MathF.Max( -Scene.PhysicsWorld.Gravity.z, 1 );
			var offset = TargetEyePosition( prey ) - WorldPosition;
			var riseSpeed = System.MathF.Sqrt( 2 * gravity * System.MathF.Max( offset.z, 16 ) );
			velocity = (offset * (gravity / riseSpeed)).WithZ( riseSpeed );
			if ( velocity.Length > 650 ) velocity = velocity.Normal * 650;
		}
		_biting = _pounceAction == ActionSerial;
		_nextJump = 2;
		var call = Game.Random.Int( 0, 2 );
		if ( call != 0 ) EmitSound( $"headcrab/hc_attack{call + 1}.wav", WorldPosition, VoiceVolume, VoicePitch );
		Launch( velocity );
	}

	internal void Touch( Collision collision )
	{
		if ( !Networking.IsHost || !_biting ) return;
		if ( _pounceAction != ActionSerial || PhysicsCarrier.IsValid() || IsOnGround || Find( Action ) is not { } jump || ActionTime >= jump.Duration )
		{
			_biting = false;
			return;
		}
		var other = collision.Other.GameObject;
		if ( other?.GetComponentInParent<IDamageable>() is not Component victim || ClassOf( victim.GameObject ) == Classification ) return;
		_biting = false;
		var damage = new DamageInfo { Attacker = GameObject, Weapon = GameObject, Origin = WorldPosition, Position = collision.Contact.Point, Damage = Definition.Damage };
		damage.Tags.Add( "slash" );
		EmitSound( Sounds.Hit, WorldPosition, VoiceVolume, VoicePitch );
		((IDamageable)victim).OnDamage( damage );
	}

	internal override void ModifyDamage( Component.IDamageable.DamageEvent damage )
	{
		base.ModifyDamage( damage );
		if ( damage.DamageInfo.Tags.Has( "acid" ) )
			damage.Cancelled = true;
	}
}
