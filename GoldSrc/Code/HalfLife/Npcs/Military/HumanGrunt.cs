namespace GoldSrc.HalfLife;

[Title( "Human Grunt" ), Category( "Half-Life" )]
[Alias( "GoldSrc.HalfLifeHumanGrunt" )]
[NpcDefinition( "monster_human_grunt", "hgrunt", MonsterClass.HumanMilitary, 50, 16, 72, 160, ArmorGroup = 11, Attack = NpcAttack.Bullet, Damage = 5, Range = 2048, ProjectileModel = "grenade", StrikeEvents = new[] { 4, 5, 6 }, Gibs = GibSet.Human, Blood = BloodColor.Red )]
public sealed partial class HumanGrunt : Npc
{
	internal override float ViewDot => 0.2f;

	protected override Sandbox.Npcs.Relationship GetFallbackRelationship( GameObject other ) =>
		other.GetComponent<Npc>() is AlienGrunt or Gargantua ? new( Sandbox.Npcs.Disposition.Hostile, 2 ) : base.GetFallbackRelationship( other );

	internal override NpcSounds Sounds { get; } = new( Pain: "hgrunt/gr_pain", Death: "hgrunt/gr_die", Attack: "hgrunt/gr_mgun" );

	static readonly string[] WeaponActions = ["standing_mp5", "crouching_mp5", "standing_shotgun", "crouching_shotgun", "reload_mp5", "reload_shotgun", "repel_repel", "repel_shoot", "repel_land", "throwgrenade", "launchgrenade", "drop_grenade", "frontkick"];
	internal override string[] ExtraActions => WeaponActions;

	internal override void BeginAttack( GameObject target )
	{
		var offset = target.WorldPosition - WorldPosition;
		if ( offset.Length <= 64 && Vector3.Dot( WorldRotation.Forward, offset.Normal ) >= 0.7f
			&& target.GetComponent<Snark>() is null && Find( "frontkick" ) is { } kick )
		{
			PlayAction( kick );
			return;
		}
		if ( Renderer.GetBodyGroup( "weapons" ) == 2 ) return;
		if ( HasLauncher && TryThrowGrenade( target ) ) return;
		if ( SquadBusyFiring( target ) )
		{
			TryThrowGrenade( target );
			return;
		}
		if ( FriendlyInLine( target ) ) return;
		if ( Ammo == 0 )
		{
			if ( !TakeCover( TargetEyePosition( target ), target, reload: true ) ) ReloadWeapon();
			return;
		}
		var stance = Game.Random.Int( 0, 1 ) == 0 ? "standing" : "crouching";
		AttackWith( target, Find( $"{stance}_{(HasShotgun ? "shotgun" : "mp5")}" ) );
	}

	internal override void HandleActionEvent( int id )
	{
		if ( id == 11 )
		{
			if ( !GunDropped ) GunDropped = DropWeapon( Renderer, HasLauncher );
			return;
		}
		if ( id == 3 )
		{
			var victim = TraceMelee( 70, 10, "club", out _ );
			PushVictim( victim, WorldRotation.Forward * 100 + Vector3.Up * 50 );
			return;
		}
		if ( id == 7 )
		{
			TimedGrenade.Throw( this, ThrowOrigin, _toss, 3.5f );
			_nextGrenade = 6;
			return;
		}
		if ( id == 8 )
		{
			EmitSound( "weapons/glauncher.wav", ThrowOrigin, volume: 0.8f );
			NpcProjectile.Fire<ContactGrenade>( this, null, ThrowOrigin, _toss.Normal, _toss.Length );
			_nextGrenade = 6;
			return;
		}
		if ( id == 9 )
		{
			var start = WorldTransform.PointToWorld( new Vector3( 17, 27, 6 ) );
			TimedGrenade.Throw( this, start, Vector3.Zero, 3 );
			return;
		}
		if ( id != 2 ) return;
		Ammo = Capacity;
		EmitSound( "hgrunt/gr_reload1.wav", WorldPosition );
	}

	internal override void ModifyDamage( Component.IDamageable.DamageEvent damage )
	{
		var groups = HitGroups( damage.DamageInfo );
		if ( groups.Has( "hitgroup_11" ) )
		{
			var tags = damage.DamageInfo.Tags;
			if ( Renderer.GetBodyGroup( "heads" ) == 0 && (tags.Has( "bullet" ) || tags.Has( "slash" ) || tags.Has( "club" ) || tags.Has( "blast" ) || tags.Has( "explosion" )) )
				AbsorbHelmetHit( damage );
		}
		else if ( !groups.Has( "hitgroup_1" ) ) return;
		damage.AcceptedDamage *= 3;
	}

	internal override void FireBullet( GameObject target, Vector3 origin, Vector3 end )
	{
		if ( GunDropped || Ammo <= 0 || FriendlyInLine( target ) ) return;
		var shotgun = HasShotgun;
		if ( IsFirstStrike )
			PlayGunshot( shotgun ? "weapons/sbarrel1.wav" : $"hgrunt/gr_mgun{Game.Random.Int( 1, 2 )}.wav", origin );

		Ammo--;
		EjectShell( origin - (end - origin).Normal * 24, WorldRotation, shotgun );
		TraceBullets( origin, end, shotgun ? 5 : 1, shotgun ? 5 : 4, shotgun ? 0.13053f : 0.08716f, 2048, tracerFrequency: shotgun ? 0 : 4 );
	}
}
