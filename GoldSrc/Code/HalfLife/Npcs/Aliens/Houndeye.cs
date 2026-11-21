namespace GoldSrc.HalfLife;

[Title( "Houndeye" ), Category( "Half-Life" )]
[Alias( "GoldSrc.HalfLifeHoundeye" )]
[NpcDefinition( "monster_houndeye", "houndeye", MonsterClass.AlienMonster, 20, 16, 36, 130, Attack = NpcAttack.Shock, Damage = 15, Range = 192, Gibs = GibSet.Alien, Blood = BloodColor.Yellow )]
public sealed partial class Houndeye : Npc
{
	internal override NpcSounds Sounds { get; } = new( "houndeye/he_idle", "houndeye/he_alert", "houndeye/he_pain", "houndeye/he_die", "houndeye/he_blast" );

	const float BlastRadius = 384;
	const int IdleAngry = 47;
	internal override IEnumerable<StudioSequence> ExtraSequences => base.ExtraSequences.Concat( Activities( IdleAngry ) );
	GameObject _sonicTarget;
	int _sonicAction = -1;

	internal override void OnModelLoaded( ModelLoader loader )
	{
		EyeHeight = loader.EyePosition.z;
		if ( Networking.IsHost ) JoinPack();
	}

	internal override bool InAttackRange( GameObject target, Vector3 aim ) =>
		WorldPosition.DistanceSquared( target.WorldPosition ) <= AttackRange * AttackRange;

	internal override void BeginAttack( GameObject target )
	{
		var facing = (target.WorldPosition - WorldPosition).WithZ( 0 ).Normal;
		if ( WorldRotation.Forward.Dot( facing ) < 0.3f ) return;
		var crowded = PackLeader.IsValid() && Scene.GetAllComponents<Houndeye>().Count( h =>
			h.PackLeader == PackLeader && h.Health > 0 && h.Action is not null && h._sonicAction == h.ActionSerial && h._sonicTarget == target ) >= 3;
		if ( WeightedActivity( crowded ? IdleAngry : Ranged ) is not { } sequence ) return;
		PlayAction( sequence );
		if ( crowded ) return;
		_sonicTarget = target;
		_sonicAction = ActionSerial;
	}

	internal override void IdleMovement()
	{
		if ( Movement?.MoveTarget.HasValue == true ) Movement.Stop();
	}

	internal override void HandleActionEvent( int id )
	{
		if ( id == 3 ) SonicAttack();
		if ( id == 6 ) Launch( WorldRotation.Backward * 200 + Vector3.Up * (-Scene.PhysicsWorld.Gravity.z * 0.3f) );
		if ( id == 7 && !Sleeping ) EyeFrame = 3;
		var sound = id switch
		{
			1 => "houndeye/he_hunt",
			2 => Game.Random.Int( 0, 1 ) == 0 ? "houndeye/he_attack1.wav" : "houndeye/he_attack3.wav",
			4 => "houndeye/he_pain3.wav",
			5 => "houndeye/he_pain1.wav",
			_ => null
		};
		if ( sound is not null ) EmitSound( sound, WorldPosition, volume: id == 2 ? 0.7f : 1 );
	}

	void SonicAttack()
	{
		var origin = WorldPosition;
		var pack = PackSize;
		EmitSound( Sounds.Attack, origin );
		ShowBlast( origin + Vector3.Up * 16, pack );

		var submerged = WaterBody.Find( Scene, EyePosition ).IsValid();
		var victims = Scene.FindInPhysics( new Sphere( origin, BlastRadius ) )
			.Select( obj => obj.GetComponentInParent<IDamageable>() ).OfType<Component>().Distinct().ToArray();
		foreach ( var victim in victims )
		{
			if ( !victim.IsValid() || victim is Houndeye ) continue;
			var center = TargetHullCenter( victim.GameObject );
			var damage = Definition.Damage * (1 + 1.1f * (pack - 1)) * (1 - origin.Distance( center ) / BlastRadius);
			if ( damage <= 0 ) continue;
			if ( !CanSeeBlastTarget( victim.GameObject, submerged ) )
			{
				if ( victim.GameObject.GetComponent<PlayerController>().IsValid() ) damage *= 0.5f;
				else if ( victim is not Prop ) continue;
			}
			var info = new DamageInfo { Damage = damage, Origin = origin, Position = center, Attacker = GameObject, Weapon = GameObject };
			info.Tags.Add( "sonic" );
			info.Tags.Add( "alwaysgib" );
			((IDamageable)victim).OnDamage( info );
		}
	}

	bool CanSeeBlastTarget( GameObject target, bool submerged )
	{
		var eye = TargetEyePosition( target );
		// FVisible permits a submerged attacker to see a wading target.
		if ( submerged ? !WaterBody.Find( Scene, target.WorldPosition + Vector3.Up ).IsValid() : WaterBody.Find( Scene, eye ).IsValid() )
			return false;

		var trace = Scene.Trace.Ray( EyePosition, eye ).IgnoreGameObjectHierarchy( GameObject )
			.WithoutTags( "npc", "player", "playercontroller", "trigger" ).Run();
		return !trace.Hit || trace.GameObject?.Root == target.Root;
	}

	[Rpc.Broadcast( NetFlags.HostOnly )]
	static void ShowBlast( Vector3 origin, int pack )
	{
		var tint = pack switch
		{
			1 => new Color32( 188, 220, 255 ),
			2 => new Color32( 101, 133, 221 ),
			3 => new Color32( 67, 85, 255 ),
			_ => new Color32( 62, 33, 211 )
		};
		SonicRing.Spawn( origin, tint, BlastRadius );
		SonicRing.Spawn( origin, tint, BlastRadius * 0.5f );
	}
}
