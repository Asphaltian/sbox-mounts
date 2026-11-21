namespace GoldSrc.HalfLife;

[Title( "Zombie" ), Category( "Half-Life" )]
[Alias( "GoldSrc.HalfLifeZombie" )]
[NpcDefinition( "monster_zombie", "zombie", MonsterClass.AlienMonster, 50, 16, 72, 85, Attack = NpcAttack.Melee, Damage = 20, Range = 64, Gibs = GibSet.Alien, Blood = BloodColor.Yellow )]
public sealed class Zombie : Npc
{
	internal override NpcSounds Sounds { get; } = new( "zombie/zo_idle", "zombie/zo_alert", "zombie/zo_pain", null,
				"zombie/zo_attack", "zombie/claw_strike" );

	const float BulletDamageScale = 0.3f;
	TimeUntil _nextFlinch;

	internal override IEnumerable<StudioSequence> ExtraSequences => Activities( Melee ).Concat( Activities( BigFlinch ) );
	internal override float VoicePitch => Game.Random.Int( 95, 104 ) * 0.01f;

	internal override void OnModelLoaded( ModelLoader loader )
	{
		EyeHeight = loader.EyePosition.z;
	}

	internal override void IdleMovement()
	{
		if ( Movement?.MoveTarget.HasValue == true ) Movement.Stop();
	}

	internal override void OnEnemySpotted( GameObject target ) => EmitSound( Sounds.Alert, WorldPosition, pitch: VoicePitch );
	internal override void IdleSound() => EmitSound( Sounds.Idle, WorldPosition, pitch: Game.Random.Int( 95, 105 ) * 0.01f );

	internal override bool InAttackRange( GameObject target, Vector3 aim ) => WorldPosition.DistanceSquared( target.WorldPosition ) <= 64 * 64;

	internal override void BeginAttack( GameObject target )
	{
		var offset = (target.WorldPosition - WorldPosition).WithZ( 0 ).Normal;
		if ( WorldRotation.Forward.Dot( offset ) < 0.7f ) return;
		if ( target.GetComponent<Sandbox.Npcs.BaseNpc>() is { IsOnGround: false } || target.GetComponent<PlayerController>() is { IsOnGround: false } ) return;
		if ( WeightedActivity( Melee ) is { } attack ) PlayAction( attack );
	}

	internal override StudioSequence SelectFlinch( Component.IDamageable.DamageEvent damage )
	{
		var activity = Find( Action )?.Description.Activity;
		if ( activity is not null && (activity != Melee || _nextFlinch > 0) ) return null;

		var flinch = damage.AcceptedDamage >= 20 ? WeightedActivity( BigFlinch ) : null;
		flinch ??= SmallFlinch( damage.DamageInfo );
		if ( flinch is not null ) _nextFlinch = 2;
		return flinch;
	}

	internal override void PainSound()
	{
		// Both CZombie and its base run the pain-sound roll on a hit.
		if ( Game.Random.Int( 0, 5 ) < 2 || Game.Random.Int( 0, 5 ) < 2 )
			EmitSound( Sounds.Pain, WorldPosition, pitch: VoicePitch );
	}

	internal override void HandleActionEvent( int id )
	{
		if ( id is < 1 or > 3 ) return;
		var victim = TraceClaw( 70, id == 3 ? 40 : Definition.Damage, "slash", Game.Random.Int( 95, 105 ) * 0.01f );
		var push = (id == 3 ? -WorldRotation.Forward : id == 2 ? WorldRotation.Right : -WorldRotation.Right) * 100;
		if ( victim.IsValid() && victim.GameObject.GetComponent<PlayerController>() is { } player )
			PunchPlayer( player, push, new Angles( 5, 0, id == 1 ? -18 : id == 2 ? 18 : 0 ) );
		else if ( victim.IsValid() ) PushVictim( victim, push );
		if ( Game.Random.Int( 0, 1 ) == 1 ) EmitSound( Sounds.Attack, WorldPosition, pitch: Game.Random.Int( 95, 105 ) * 0.01f );
	}

	static bool IsPlainBullet( DamageInfo info ) => info.Tags.Has( "bullet" ) && info.Tags.TryGetAll().All( tag =>
		tag is "bullet" or "head" or "studio_hitbox" || tag.StartsWith( "hitgroup_", System.StringComparison.Ordinal ) );

	internal override void ModifyDamage( Component.IDamageable.DamageEvent damage )
	{
		base.ModifyDamage( damage );
		if ( IsPlainBullet( damage.DamageInfo ) ) damage.AcceptedDamage *= BulletDamageScale;
	}

	internal override void ShowWound( Component.IDamageable.DamageEvent hit )
	{
		// TraceAttack draws blood before TakeDamage reduces bullet damage.
		var amount = IsPlainBullet( hit.DamageInfo ) ? hit.AcceptedDamage / BulletDamageScale : hit.AcceptedDamage;
		BloodEffects.Wound( GameObject, Definition.Blood, hit.DamageInfo, amount );
	}

	protected override void OnDamaged( Component.IDamageable.DamageEvent damage )
	{
		base.OnDamaged( damage );
		var info = damage.DamageInfo;
		if ( damage.AcceptedDamage <= 0 || !IsPlainBullet( info ) ) return;
		var force = System.MathF.Min( damage.AcceptedDamage / BulletDamageScale * 5, 1000 );
		var origin = info.Weapon.IsValid() ? info.Weapon.GetBounds().Center : info.Origin;
		PushVictim( this, (WorldPosition - origin).Normal * force );
	}
}
