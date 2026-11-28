namespace Source1.Css;

/// <summary>
/// Counter-Strike: Source's knife: attack1 slashes, attack2 stabs. Damage, reach and recovery are
/// CKnife's code constants; the weapon script's Damage goes unused, as in the game.
/// </summary>
[Title( "CS:S Knife" )]
[Category( "Counter-Strike: Source" )]
[Alias( "Source1.CssKnife", "CssKnife" )]
public sealed class Knife : MeleeWeapon
{
	static readonly BBox SwingHull = new( new Vector3( -16, -16, -18 ), new Vector3( 16, 16, 18 ) );

	[Property] public float SwingForce { get; set; } = 1000f;

	const float SlashDamage = 15;
	const float StabDamage = 65;
	const float SlashRange = 48;
	const float StabRange = 32;
	const float SlashHitDelay = 0.5f;
	const float SlashMissDelay = 0.4f;
	const float StabHitDelay = 1.1f;
	const float StabMissDelay = 1;

	internal override bool ShouldPlayIdle => true;

	public override bool CanSecondaryAttack() => NextSecondaryFire <= 0 && NextPrimaryFire <= 0;

	TimeUntil _fullSlash;

	protected override void OnEquipped()
	{
		base.OnEquipped();
		_fullSlash = DeployTime + 0.4f;
		if ( !Application.IsDedicatedServer ) PlaySound( "Weapon_Knife.Deploy" );
	}

	public override void PrimaryAttack() => Swing( false );

	public override void SecondaryAttack() => Swing( true );

	void Swing( bool stab )
	{
		var aim = AimRay;
		var trace = Scene.Trace.Ray( aim, stab ? StabRange : SlashRange )
			.IgnoreGameObjectHierarchy( AimIgnoreRoot )
			.WithoutTags( "trigger" );
		var tr = trace.Run();
		if ( !tr.Hit ) tr = trace.Size( SwingHull ).Run();
		var delay = stab ? (tr.Hit ? StabHitDelay : StabMissDelay) : (tr.Hit ? SlashHitDelay : SlashMissDelay);
		var damage = stab ? StabDamage : _fullSlash < 0 ? 20 : SlashDamage;
		if ( stab && tr.GameObject.IsValid() && tr.GameObject.GetComponentInParent<PlayerController>() is { } victim )
		{
			var forward = victim.EyeTransform.Rotation.Forward.WithZ( 0 ).Normal;
			var towards = (victim.WorldPosition - (Owner?.WorldPosition ?? aim.Position)).WithZ( 0 ).Normal;
			if ( towards.Dot( forward ) > 0.8f ) damage *= 3;
		}
		_fullSlash = delay + 0.4f;
		SetNextPrimaryFire( delay );
		SetNextSecondaryFire( stab ? delay : SlashHitDelay );
		SwingEffects( new ShotEffect( tr.HitPosition, tr.Hit, tr.Normal, tr.GameObject, tr.Surface, aim.Position ), stab );
		tr.Direction = aim.Forward;
		ShootBullet( tr, damage, SwingForce, hitboxTags: false );
	}

	[Rpc.Broadcast( NetFlags.OwnerOnly )]
	void SwingEffects( ShotEffect shot, bool stab )
	{
		WeaponModel?.OnAttack( shot.Hit ? shot.HitPosition : null, shot.Origin );
		PostWeaponEvent( WeaponEvents.Attack );
		if ( Application.IsDedicatedServer ) return;
		if ( shot.Hit ) _ = PlayImpact( shot, stab );
		else PlaySound( "Weapon_Knife.Slash" );
	}

	async Task PlayImpact( ShotEffect shot, bool stab )
	{
		await GameTask.DelaySeconds( stab ? 0.2f : 0.1f );
		if ( !IsValid || !shot.HitObject.IsValid() ) return;
		var player = shot.HitObject.GetComponentInParent<PlayerController>();
		PlaySound( player.IsValid() ? stab ? "Weapon_Knife.Stab" : "Weapon_Knife.Hit" : "Weapon_Knife.HitWall" );
		ImpactPrefab( shot.HitObject, shot.Surface, shot.HitPosition, shot.Normal, GetShotDirection( shot ) );
	}

	void PlaySound( string name )
	{
		if ( Sounds.Sound( name, MountContent.OfWeapon( this ) ) is { } sound ) GameObject.PlaySound( sound );
	}
}
