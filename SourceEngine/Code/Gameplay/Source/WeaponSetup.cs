using System.Runtime.CompilerServices;
using System;

namespace Source1;

/// <summary>Configures a mounted weapon from its weapon script and viewmodel: ammo, damage, HUD icon, sounds, flash, brass and clip timing.</summary>
sealed class WeaponSetup
{
	// SDK default for the convar when skill.cfg leaves it unset.
	const float ShotgunPellets = 7;

	Task<bool> _configured;
	public SourceModelWeaponData Data;
	public string FlashType;
	public float FlashScale = 1;
	public (string Model, string Attachment, float? Speed)? Brass;
	public string WeaponType;
	public float? PumpDelay;
	float? PlayerDamage;
	SoundEvent FireSound;

	// Native weapons can also use mounted viewmodels. Only those need externally owned setup state.
	static readonly ConditionalWeakTable<BaseCombatWeapon, WeaponSetup> NativeWeapons = new();
	static readonly HashSet<string> Warmed = new();

	static string WorldModelPath( BaseCombatWeapon weapon ) => weapon.WorldModelPrefab?.GetComponentInChildren<MountedModel>()?.ModelPath;

	static string ViewModelPath( MountedModel definition ) => $"mount://{definition.MountName}/{definition.ResolvedModelPath}.vmdl";

	public static WeaponSetup Of( BaseCombatWeapon weapon )
	{
		if ( !weapon.IsValid() ) return null;
		if ( weapon is Weapon mounted ) return mounted.Setup;
		return NativeWeapons.TryGetValue( weapon, out var setup ) ? setup : null;
	}

	/// <summary>
	/// NPC-only tuning while an NPC holds the weapon: the script's single_shot_npc sound and NPC damage, scaled down
	/// for Counter-Strike. A player who picks the weapon up gets the player's. False until the weapon is configured.
	/// </summary>
	public bool ApplyHolder( BulletWeapon weapon, bool npc )
	{
		if ( PlayerDamage is not { } playerDamage ) return false;
		var data = Data;
		var scale = Css.Rules.Owns( weapon ) ? Css.Rules.NpcDamageScale : 1;
		var damage = npc ? (data?.NpcDamage ?? playerDamage) * scale : playerDamage;
		weapon.Ballistics = weapon.Ballistics with { Damage = damage };
		var npcSound = npc ? Sounds.WeaponSound( data, "single_shot_npc" ) : null;
		if ( (npcSound ?? Sounds.WeaponSound( data, Sounds.FireSoundRole( weapon ) ) ?? FireSound) is { } sound ) weapon.AttackSound = sound;
		return true;
	}

	/// <summary>Run before inventory pickup seeds reserves or donates duplicate weapon ammo.</summary>
	public void ConfigureAmmo( Weapon weapon )
	{
		Data ??= ReadData( weapon );
		ApplyAmmo( weapon, Data );

		if ( weapon is { SecondaryPickupAmmo: < 0, UsesSecondaryClip: false } && Data is { SecondaryClipSize: -1, SecondaryDefaultClip: > 0 } )
			weapon.SecondaryPickupAmmo = Data.SecondaryDefaultClip.Value;

		// A fresh pickup carries default_clip rounds in total. Preserve the rounds left in dropped weapons.
		if ( Data is { ClipSize: > 0, DefaultClip: >= 0 } data && weapon.UsesPrimaryClip )
		{
			weapon.StartingAmmo = 0;
			if ( weapon.Clip1 < 0 ) weapon.Clip1 = data.DefaultClip.Value;
		}
	}

	/// <summary>
	/// The weapon's mounted HUD icon, kept as its DisplayIcon. Also for a weapon nothing has configured yet:
	/// one picked up before its mount was ready, or a plain melee weapon.
	/// </summary>
	public Texture Icon( Weapon weapon )
	{
		if ( !weapon.IsValid() ) return null;
		Data ??= ReadData( weapon );
		var icon = Data?.Icon;
		if ( icon is null || Sandbox.Mounting.Directory.Get( MountContent.Of( icon ) )?.GetByPath( icon ) is null ) return null;
		weapon.DisplayIcon = Texture.Load( icon );
		return weapon.DisplayIcon;
	}

	static SourceModelWeaponData ReadData( BaseCombatWeapon weapon )
	{
		var definition = weapon.ViewModelPrefab?.GetComponentInChildren<MountedModel>();
		if ( !definition.IsValid() ) return null;
		var mount = Sandbox.Mounting.Directory.Get( definition.MountName ) as SourceGameMount;
		return mount?.FindWeaponScript( definition.ModelPath, WorldModelPath( weapon ), definition.FallbackModelPath );
	}

	internal static void ApplyInventoryData( BaseCombatWeapon weapon, SourceModelWeaponData data )
	{
		if ( data is null ) return;
		weapon.PreferredSlot = data.Bucket;
		if ( data.BucketPosition is { } position ) weapon.SlotOrder = position;
		if ( data.Weight is { } weight ) weapon.Value = weight;
		if ( data.PrimaryAmmoResource is { } primary ) weapon.PrimaryAmmoType = GameResource.Load<BaseAmmoResource>( primary );
		if ( data.SecondaryAmmoResource is { } secondary ) weapon.SecondaryAmmoType = GameResource.Load<BaseAmmoResource>( secondary );
		if ( data.ClipSize > 0 && weapon.UsesAmmo ) weapon.ClipMaxSize = data.ClipSize;
		if ( data is { ClipSize: -1, DefaultClip: >= 0 } && weapon.UsesAmmo && weapon.PrimaryAmmoType is not null )
		{
			weapon.UsesClips = false;
			weapon.StartingAmmo = data.DefaultClip.Value;
		}
	}

	static void ApplyAmmo( BaseCombatWeapon weapon, SourceModelWeaponData data )
	{
		if ( data is null ) return;
		ApplyInventoryData( weapon, data );
		if ( weapon.DisplayIcon is null && data.Icon is { } icon && Sandbox.Mounting.Directory.Get( MountContent.Of( icon ) )?.GetByPath( icon ) is not null )
			weapon.DisplayIcon = Texture.Load( icon );
		// Counter-Strike scripts carry fire rate and ballistics; HL2 keeps them in code, so its catalog supplies them.
		if ( data.CycleTime is { } cycle ) weapon.PrimaryDelay = cycle;
		if ( data.FullAuto is { } automatic ) weapon.PrimaryAutomatic = automatic;
		if ( weapon is BulletWeapon bullet )
		{
			var ballistics = bullet.Ballistics;
			if ( data.Bullets is { } bullets ) ballistics = ballistics with { Pellets = bullets };
			if ( data.Range is { } range ) ballistics = ballistics with { Range = range };
			if ( data.PlayerDamage is { } damage ) ballistics = ballistics with { Damage = damage };
			bullet.Ballistics = ballistics;
			bullet.AlternateDamage = data.SecondaryPlayerDamage;
			bullet.AlternateRadius = data.SecondaryBlastRadius;
			bullet.AlternateProjectile = Projectile.ForAmmo( data.SecondaryAmmo );
		}
		if ( weapon is MeleeWeapon melee && data.PlayerDamage is { } meleeDamage ) melee.Damage = meleeDamage;
		if ( weapon is ProjectileWeapon projectile )
		{
			// Multiplayer rockets and frags take damage from the weapon script. The prefab supplies its stock value.
			var playerDamage = projectile.Kind is Projectile.Kinds.Rocket or Projectile.Kinds.Grenade ? data.ScriptDamage : data.PlayerDamage;
			if ( playerDamage is { } damage ) projectile.ProjectileDamage = damage;
			if ( projectile.Kind != Projectile.Kinds.Grenade && data.BlastRadius is { } radius ) projectile.ExplosionRadius = radius;
			projectile.NpcDamage = data.NpcDamage;
		}
	}

	/// <summary>Reads the mounted weapon script and animation timing once per weapon instance. A failed attempt is retried by the next call.</summary>
	public static Task ConfigureWeapon( BaseCombatWeapon weapon )
	{
		var state = weapon is Weapon mounted ? mounted.Setup : NativeWeapons.GetOrCreateValue( weapon );
		return state.ConfigureOnce( weapon );
	}

	Task ConfigureOnce( BaseCombatWeapon weapon )
	{
		if ( _configured is null || _configured.IsFaulted || _configured.IsCompletedSuccessfully && !_configured.Result )
			_configured = Configure( weapon );
		return _configured;
	}

	async Task<bool> Configure( BaseCombatWeapon weapon )
	{
		var definition = weapon.ViewModelPrefab?.GetComponentInChildren<MountedModel>();
		var names = weapon.ViewModelPrefab?.GetComponentInChildren<ViewModel>();
		if ( !definition.IsValid() || !names.IsValid() ) return true;
		var mount = await Sandbox.Mounting.Directory.Mount( definition.MountName );
		if ( !weapon.IsValid() || mount?.IsMounted != true ) return false;
		var model = await Model.LoadAsync( ViewModelPath( definition ) );
		if ( !weapon.IsValid() || !model.IsValid() || model.IsError ) return false;
		var skill = await SkillData.Read( definition.MountName );
		if ( !weapon.IsValid() ) return false;
		var worldModel = WorldModelPath( weapon );
		Data ??= ReadData( weapon );
		if ( Data is { } data )
		{
			ApplyAmmo( weapon, data );
			if ( Sounds.WeaponSound( data, Sounds.FireSoundRole( weapon ) ) is { } fire ) weapon.AttackSound = fire;
			if ( Sounds.WeaponSound( data, "empty" ) is { } empty ) weapon.DryFireSound = empty;
			if ( weapon is MeleeWeapon melee && Sounds.WeaponSound( data, "melee_hit" ) is { } hit ) melee.HitSound = hit;
			// weapon_shotgun fires sk_plr_num_shotgun_pellets in code rather than from its script.
			if ( data.Bullets is null && data.ClassName == "weapon_shotgun" && weapon is BulletWeapon shotgun )
				shotgun.Ballistics = shotgun.Ballistics with { Pellets = (int)skill.Get( "sk_plr_num_shotgun_pellets", ShotgunPellets ) };
		}
		// Without its script (Garry's Mod's copy of Counter-Strike) the shot is the sound script the
		// viewmodel's own events are named after: Weapon_AK47.Clipout's weapon fires Weapon_AK47.Single.
		if ( Data is null && weapon is BulletWeapon && ShotSoundOf( model, definition.MountName ) is { } shot ) weapon.AttackSound = shot;
		var counterStrike = Css.Rules.Owns( weapon );
		WeaponType = Data?.WeaponType ?? (counterStrike ? Css.Rules.WeaponTypeOf( worldModel ) : null);
		if ( counterStrike && Sounds.WeaponSound( Data, "empty" ) is null && Sounds.Sound( Css.Rules.EmptySound( WeaponType ), definition.MountName ) is { } click )
			weapon.DryFireSound = click;
		FlashType = WeaponEffects.FlashType( model );
		// Scripts without flash settings use the plain flash at scale 1.
		if ( FlashType is null && counterStrike && Data is { } script )
			(FlashType, FlashScale) = (script.MuzzleFlashStyle ?? Css.Rules.Flash, script.MuzzleFlashScale ?? 1);
		else if ( FlashType is null && counterStrike && Css.Rules.FlashFor( WeaponType, weapon.PrimaryAutomatic ) is { } flash )
			(FlashType, FlashScale) = flash;
		Brass = BrassOf( model );
		if ( weapon is BulletWeapon gun )
		{
			PlayerDamage = gun.Ballistics.Damage;
			FireSound = weapon.AttackSound;
		}
		var clips = WeaponClips.Resolve( model, names );
		float Duration( string clip ) => WeaponClips.Duration( model, clip );
		var draw = Duration( clips.Draw );
		if ( definition.MountName == "hl2" && weapon.DeployTime > 0 && draw > 0 && names.DrawPlaybackRate > 0 )
			weapon.DeployTime = draw / names.DrawPlaybackRate;
		if ( weapon is ProjectileWeapon projectile && clips.Throws )
		{
			if ( projectile.Kind == Projectile.Kinds.Bugbait && clips.SecondaryFire is not null )
				projectile.SecondaryDelay = Duration( clips.SecondaryFire );
			projectile.ThrowClips = clips;
			// Thrown weapons draw a new item rather than swapping a magazine.
			weapon.ReloadTime = draw;
			projectile.RedrawDuration = weapon.ReloadTime;
			weapon.AttackSound = null;
		}
		var reload = Duration( clips.Incremental ? clips.ReloadInsert : clips.Reload );
		if ( reload > 0 && weapon.UsesAmmo ) weapon.ReloadTime = reload;
		if ( weapon is ProjectileWeapon { Kind: Projectile.Kinds.Bolt } crossbow
			&& ModelSequences.Of( model ).Get( clips.Reload )?.Events.FirstOrDefault( e => e.Id == AnimEvents.WeaponThrow ) is { } charge )
			crossbow.BoltChargeDelay = charge.Cycle * reload;
		if ( clips.Incremental && weapon.UsesAmmo )
		{
			weapon.IncrementalReloading = true;
			weapon.CanCancelReload = true;
			var start = Duration( clips.Reload );
			if ( start > 0 ) weapon.ReloadStartTime = start;
			weapon.FirstShellReloadTime = 0;
			var pump = Duration( clips.Pump );
			if ( pump > 0 )
			{
				PumpDelay = Duration( clips.Fire );
				weapon.PrimaryDelay = Math.Max( weapon.PrimaryDelay, PumpDelay.Value + pump );
			}
		}
		if ( weapon is BulletWeapon bullet )
		{
			bullet.DryFireDuration = Duration( clips.DryFire );
			if ( Data?.ClassName == "weapon_357"
				&& ModelSequences.Of( model ).Get( clips.Reload )?.Events.FirstOrDefault( e => e.Id == AnimEvents.WeaponReload ) is { } eject )
				bullet.CylinderEjection = (eject.Cycle * reload, definition.MountName);
			var secondary = Duration( clips.SecondaryFire );
			if ( bullet.Secondary == Weapon.SecondaryFires.DoubleShot && secondary > 0 )
			{
				bullet.ReloadFinishDuration = Duration( clips.ReloadEnd );
				bullet.AlternatePumpDelay = secondary;
				bullet.PumpDuration = Duration( clips.Pump );
			}
			bullet.SilencerDuration = Duration( clips.AttachSilencer );
			// Counter-Strike's attack2 is code; infer it from what the viewmodel authors for it.
			if ( bullet.Secondary == Weapon.SecondaryFires.Default && bullet.AlternateProjectile is null )
				bullet.Secondary = clips.AttachSilencer is not null ? Weapon.SecondaryFires.Silencer
					: clips.Burst is not null ? Weapon.SecondaryFires.Burst
					: model.Materials.Any( m => m.Name.Contains( "scope", StringComparison.OrdinalIgnoreCase ) ) ? Weapon.SecondaryFires.Zoom
					: Weapon.SecondaryFires.Default;
			if ( bullet.Secondary == Weapon.SecondaryFires.Zoom && bullet.ZoomLevels is not { Count: > 0 } ) bullet.ZoomLevels = [.. Css.Rules.ZoomLevels( WeaponType )];
		}
		// Warm each weapon's mounted dependencies once, keyed by path only so no
		// model or task is rooted past scene teardown.
		if ( Warmed.Add( model.ResourcePath + "|" + worldModel ) ) await WarmResources( weapon, model, Data );
		return true;
	}

	static async Task WarmResources( BaseCombatWeapon weapon, Model model, SourceModelWeaponData data )
	{
		var mount = MountContent.Of( model );
		var sounds = (data?.Sounds.Values ?? Enumerable.Empty<string>())
			.Where( path => Sandbox.Mounting.Directory.Get( MountContent.Of( path ) )?.GetByPath( path ) is not null )
			.Select( path => GameResource.Load<SoundEvent>( path ) )
			.Concat( ModelSequences.Of( model ).Events.Where( Sounds.IsSoundEvent ).Select( e => Sounds.Sound( e.Options, mount ) ) )
			.Where( s => s is not null ).SelectMany( s => s.Sounds ).Distinct().ToArray();
		if ( !Application.IsDedicatedServer )
			foreach ( var sound in sounds ) await sound.LoadAsync();
		if ( !Application.IsDedicatedServer && weapon is ProjectileWeapon { Kind: Projectile.Kinds.Rocket } )
			await Model.LoadAsync( $"mount://{mount}/{Hl2.Rocket.LaunchModel}.vmdl" );
		var projectile = (weapon as ProjectileWeapon)?.ProjectileModel ?? Projectile.ModelFor( (weapon as BulletWeapon)?.AlternateProjectile );
		if ( !string.IsNullOrWhiteSpace( projectile ) ) await Model.LoadAsync( $"mount://{mount}/{projectile}.vmdl" );
	}

	static SoundEvent ShotSoundOf( Model model, string mountName ) => ModelSequences.Of( model ).Events
		.Where( Sounds.IsSoundEvent ).Select( e => e.Options?.Split( '.' ) is [var prefix, _] ? prefix : null ).Where( x => x is not null )
		.GroupBy( x => x, StringComparer.OrdinalIgnoreCase ).OrderByDescending( g => g.Count() )
		.Select( g => Sounds.Sound( g.Key + ".Single", mountName ) ).FirstOrDefault( x => x is not null );

	/// <summary>CL_EVENT_EJECTBRASS1 leaves from the second attachment; Counter-Strike's brass events name theirs.</summary>
	static (string Model, string Attachment, float? Speed)? BrassOf( Model model )
	{
		foreach ( var e in ModelSequences.Of( model ).Events )
		{
			if ( e.Id == AnimEvents.EjectBrass1 && model.Attachments.All.Count > 1 )
				return (WeaponEffects.ShellModel( int.TryParse( e.Options, out var type ) ? type : 0 ), model.Attachments.All[1].Name, null);
			if ( e.Name == "AE_CLIENT_EFFECT_ATTACH" && WeaponEffects.Brass( e.Options ) is { } brass ) return (brass.Model, brass.Attachment, brass.Speed);
		}
		return null;
	}
}
