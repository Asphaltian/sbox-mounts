using System;

namespace Source1.Css.Prefabs;

[Library]
public static class Weapons
{
	static readonly Firearm.KickbackSettings AugFamasKickback = new(
		Standing: new( 0.625f, 0.375f, 0.25f, 0.0125f, 3.5f, 2.25f, 8 ),
		Crouching: new( 0.575f, 0.325f, 0.2f, 0.011f, 3.25f, 2, 8 ),
		Moving: new( 1, 0.45f, 0.275f, 0.05f, 4, 2.5f, 7 ),
		Airborne: new( 1.25f, 0.45f, 0.22f, 0.18f, 5.5f, 4, 5 ) );

	static readonly Firearm.KickbackSettings GalilM4Kickback = new(
		Standing: new( 0.65f, 0.35f, 0.25f, 0.015f, 3.5f, 2.25f, 7 ),
		Crouching: new( 0.6f, 0.3f, 0.2f, 0.0125f, 3.25f, 2, 7 ),
		Moving: new( 1, 0.45f, 0.28f, 0.045f, 3.75f, 3, 7 ),
		Airborne: new( 1.2f, 0.5f, 0.23f, 0.15f, 5.5f, 3.5f, 6 ) );

	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> Ak47() =>
		Rifle( "ak47", "AK-47", "rif_ak47", w => w.Kickback = new(
			Standing: new( 1, 0.375f, 0.175f, 0.0375f, 5.75f, 1.75f, 8 ),
			Crouching: new( 0.9f, 0.35f, 0.15f, 0.025f, 5.5f, 1.5f, 9 ),
			Moving: new( 1.5f, 0.45f, 0.225f, 0.05f, 6.5f, 2.5f, 7 ),
			Airborne: new( 2, 1, 0.5f, 0.35f, 9, 6, 5 ) ) );

	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> Aug() =>
		Rifle( "aug", "AUG", "rif_aug", w =>
		{
			ScopedRifle( w );
			w.Kickback = AugFamasKickback;
		} );

	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> Awp() =>
		BoltRifle( "awp", "AWP", "snip_awp", 10, 150, 0.1f );

	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> Bomb() =>
		EntityPrefab.WeaponSet<C4>( "css", "c4", "C4", "models/weapons/w_c4.mdl", "models/weapons/cstrike/c_c4.mdl", w =>
		{
			w.DisplayName = "CS:S C4";
			w.HoldType = WeaponHolds.Item;
			w.UsesAmmo = false;
			w.PreferredSlot = 4;
			w.UsableByNpcs = false;
		}, fallback: "models/weapons/v_c4.mdl" );

	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> Deagle() =>
		Pistol( "deagle", "Desert Eagle", "pist_deagle" );

	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> Elite() =>
		Pistol( "elite", "Dual Elites", "pist_elite", w =>
		{
			w.HoldType = "dual_pistols";
		} );

	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> Famas() =>
		Rifle( "famas", "FAMAS", "rif_famas", w =>
		{
			w.Secondary = Weapon.SecondaryFires.Burst;
			w.Burst = new( 3, 0.075f, 0.55f );
			w.Kickback = AugFamasKickback;
		} );

	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> Fiveseven() =>
		Pistol( "fiveseven", "Five-SeveN", "pist_fiveseven", w =>
		{
			w.UseLastShotAnimation = false;
		} );

	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> Flashbang() =>
		Grenade( "flashbang", "Flashbang", "eq_flashbang", Projectile.Kinds.Flashbang );

	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> G3sg1() =>
		SniperRifle( "g3sg1", "G3SG1", "snip_g3sg1", w =>
		{
			w.SniperPunch = new( 0.75f, 1.75f );
		} );

	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> Galil() =>
		Rifle( "galil", "Galil", "rif_galil", w => w.Kickback = GalilM4Kickback );

	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> Glock() =>
		Pistol( "glock", "Glock 18", "pist_glock18", w =>
		{
			w.Secondary = Weapon.SecondaryFires.Burst;
			w.Burst = new( 3, 0.06f, 0.5f );
			w.ShotPunch = 0;
			w.AlternateRangeModifier = 0.9f;
			w.AlternateShotDamage = 18;
			w.UseLastShotAnimation = false;
			w.RecoilPitch = w.RecoilYaw = w.PunchRoll = Vector2.Zero;
		} );

	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> Hegrenade() =>
		Grenade( "hegrenade", "HE Grenade", "eq_fraggrenade", Projectile.Kinds.HeGrenade );

	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> Knife() =>
		EntityPrefab.WeaponSet<Knife>( "css", "knife", "Knife",
			"models/weapons/w_knife_t.mdl", "models/weapons/cstrike/c_knife_t.mdl", w =>
			{
				w.DisplayName = "CS:S Knife";
				w.Handedness = BaseCombatWeapon.WeaponHandedness.Right;
				w.HoldType = "knife";
				w.PreferredSlot = 2;
				w.UsableByNpcs = false;
				w.Value = 10;
			}, fallback: "models/weapons/v_knife_t.mdl" );

	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> M249() =>
		Gun( "m249", "M249", "mach_m249para", w =>
		{
			w.Ballistics = w.Ballistics with { Damage = 35, Range = 8192, Force = 300, SpreadBase = new( 0.5f, 0.5f ), SpreadGrowth = new( 1, 1 ) };
			w.ClipMaxSize = 100;
			w.HoldType = "rifle";
			w.Npc = w.Npc with { MinRange = 65, MaxRange = 2048 };
			w.PrimaryDelay = 0.08f;
			w.ReloadTime = 5;
			w.StartingAmmo = 200;
			w.Kickback = new(
				Standing: new( 0.8f, 0.35f, 0.3f, 0.03f, 3.75f, 3, 9 ),
				Crouching: new( 0.75f, 0.325f, 0.25f, 0.025f, 3.5f, 2.5f, 9 ),
				Moving: new( 1.1f, 0.5f, 0.3f, 0.06f, 4, 3, 8 ),
				Airborne: new( 1.8f, 0.65f, 0.45f, 0.125f, 5, 3.5f, 8 ),
				AirborneFirst: true );
		} );

	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> M3() =>
		Shotgun( "m3", "M3 Super 90", "shot_m3super90" );

	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> M4a1() =>
		Rifle( "m4a1", "M4A1", "rif_m4a1", w =>
		{
			w.Secondary = Weapon.SecondaryFires.Silencer;
			w.SecondaryDelay = 2;
			w.AlternateRangeModifier = 0.95f;
			w.Kickback = GalilM4Kickback;
		} );

	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> Mac10() =>
		Smg( "mac10", "MAC-10", "smg_mac10", new(
			Standing: new( 0.775f, 0.425f, 0.2f, 0.03f, 3, 2.75f, 9 ),
			Crouching: new( 0.75f, 0.4f, 0.175f, 0.03f, 2.75f, 2.5f, 10 ),
			Moving: new( 0.9f, 0.45f, 0.25f, 0.035f, 3.5f, 2.75f, 7 ),
			Airborne: new( 1.3f, 0.55f, 0.4f, 0.05f, 4.75f, 3.75f, 5 ), AirborneFirst: true ) );

	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> Mp5navy() =>
		Smg( "mp5navy", "MP5 Navy", "smg_mp5", new(
			Standing: new( 0.25f, 0.175f, 0.125f, 0.02f, 2.25f, 1.25f, 10 ),
			Crouching: new( 0.225f, 0.15f, 0.1f, 0.015f, 2, 1, 10 ),
			Moving: new( 0.5f, 0.275f, 0.2f, 0.03f, 3, 2, 10 ),
			Airborne: new( 0.9f, 0.475f, 0.35f, 0.0425f, 5, 3, 6 ), AirborneFirst: true ) );

	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> P228() =>
		Pistol( "p228", "P228", "pist_p228", w => w.UseLastShotAnimation = false );

	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> P90() =>
		Smg( "p90", "P90", "smg_p90", new(
			Standing: new( 0.3f, 0.225f, 0.125f, 0.02f, 3.25f, 1.25f, 8 ),
			Crouching: new( 0.275f, 0.2f, 0.125f, 0.02f, 3, 1, 9 ),
			Moving: new( 0.45f, 0.3f, 0.2f, 0.0275f, 4, 2.25f, 7 ),
			Airborne: new( 0.9f, 0.45f, 0.35f, 0.04f, 5.25f, 3.5f, 4 ), AirborneFirst: true ) );

	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> Scout() =>
		BoltRifle( "scout", "Scout", "snip_scout", 15, 220, 0.05f );

	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> Sg550() =>
		SniperRifle( "sg550", "SG 550", "snip_sg550", w => w.SniperPunch = new( 0.75f, 1.25f ) );

	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> Sg552() =>
		Rifle( "sg552", "SG 552", "rif_sg552", w =>
		{
			ScopedRifle( w );
			w.ZoomedMaxSpeed = 200;
			w.Kickback = new(
				Standing: new( 0.625f, 0.375f, 0.25f, 0.0125f, 4, 2.25f, 9 ),
				Crouching: new( 0.6f, 0.35f, 0.2f, 0.0125f, 3.7f, 2, 10 ),
				Moving: new( 1, 0.45f, 0.28f, 0.04f, 4.25f, 2.5f, 7 ),
				Airborne: new( 1.25f, 0.45f, 0.22f, 0.18f, 6, 4, 5 ) );
		} );

	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> Smokegrenade() =>
		Grenade( "smokegrenade", "Smoke Grenade", "eq_smokegrenade", Projectile.Kinds.SmokeGrenade );

	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> Tmp() =>
		Smg( "tmp", "TMP", "smg_tmp", new(
			Standing: new( 0.725f, 0.375f, 0.15f, 0.025f, 2.75f, 2.25f, 9 ),
			Crouching: new( 0.7f, 0.35f, 0.125f, 0.025f, 2.5f, 2, 10 ),
			Moving: new( 0.8f, 0.4f, 0.2f, 0.03f, 3, 2.5f, 7 ),
			Airborne: new( 1.1f, 0.5f, 0.35f, 0.045f, 4.5f, 3.5f, 6 ), AirborneFirst: true ) );

	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> Ump45() =>
		Smg( "ump45", "UMP-45", "smg_ump45", new(
			Standing: new( 0.275f, 0.2f, 0.15f, 0.0225f, 2.5f, 1.5f, 10 ),
			Crouching: new( 0.25f, 0.175f, 0.125f, 0.02f, 2.25f, 1.25f, 10 ),
			Moving: new( 0.55f, 0.3f, 0.225f, 0.03f, 3.5f, 2.5f, 10 ),
			Airborne: new( 0.125f, 0.65f, 0.55f, 0.0475f, 5.5f, 4, 10 ), AirborneFirst: true ) );

	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> Usp() =>
		Pistol( "usp", "USP .45", "pist_usp", w =>
		{
			w.Secondary = Weapon.SecondaryFires.Silencer;
			w.SecondaryDelay = 3;
			w.AlternateShotDamage = 30;
			w.UseLastShotAnimation = false;
		} );

	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> Xm1014() =>
		Shotgun( "xm1014", "XM1014", "shot_xm1014", 3 );

	static IEnumerable<EntityDefinition> Grenade( string name, string title, string model, Projectile.Kinds kind ) =>
		EntityPrefab.WeaponSet<GrenadeWeapon>( "css", name, title, $"models/weapons/w_{model}.mdl",
			$"models/weapons/cstrike/c_{model}.mdl", w =>
			{
				w.Ballistics = w.Ballistics with { Damage = 8, Force = 300 };
				w.CanCancelReload = false;
				w.ClipMaxSize = 1;
				w.DeployTime = 0.2f;
				w.DisplayName = $"CS:S {title}";
				w.HoldType = "throwable";
				w.PreferredSlot = 3;
				w.PrimaryAutomatic = false;
				w.PrimaryDelay = 0.8f;
				w.ReloadTime = 0.5f;
				w.SecondaryDelay = 1;
				w.StartingAmmo = 3;
				w.UsableByNpcs = false;
				w.Value = 10;
				w.Kind = kind;
				w.ProjectileModel = $"models/weapons/w_{model}_thrown.mdl";
			}, fallback: $"models/weapons/v_{model}.mdl" );

	static IEnumerable<EntityDefinition> Rifle( string name, string title, string model, Action<Firearm> configure = null ) =>
		Gun( name, title, model, w =>
		{
			w.Ballistics = w.Ballistics with { Damage = 32, Range = 8192, Force = 300, SpreadBase = new( 0.25f, 0.25f ), SpreadGrowth = new( 1, 1 ) };
			w.HoldType = "rifle";
			w.Npc = w.Npc with { MinRange = 65, MaxRange = 2048 };
			w.ReloadTime = 2.5f;
			w.StartingAmmo = 90;
			configure?.Invoke( w );
		} );

	static void ScopedRifle( Firearm weapon )
	{
		weapon.Secondary = Weapon.SecondaryFires.Zoom;
		weapon.ZoomLevels = [55];
		weapon.ZoomTransition = new( 0.2f, 0.15f );
		weapon.ZoomedCycleTime = 0.135f;
		weapon.SecondaryDelay = 0.3f;
	}

	static IEnumerable<EntityDefinition> BoltRifle( string name, string title, string model, int deepZoom, int zoomedSpeed, float zoomOutTime ) =>
		SniperRifle( name, title, model, w =>
		{
			w.ZoomLevels = [40, deepZoom];
			w.BoltZoomOutTime = zoomOutTime;
			w.ZoomedMaxSpeed = zoomedSpeed;
			w.ShotPunch = 2;
		} );

	static IEnumerable<EntityDefinition> SniperRifle( string name, string title, string model, Action<Firearm> configure = null ) =>
		Gun( name, title, model, w =>
		{
			w.Ballistics = w.Ballistics with { Damage = 75, Range = 8192, Force = 300, SpreadBase = new( 0.05f, 0.05f ), SpreadGrowth = new( 0, 0 ) };
			w.ClipMaxSize = 10;
			w.HoldType = "rifle";
			w.Npc = w.Npc with { MaxRange = 8192 };
			w.PrimaryAutomatic = false;
			w.PrimaryDelay = 1.25f;
			w.ReloadTime = 3;
			w.Secondary = Weapon.SecondaryFires.Zoom;
			w.ZoomLevels = [40, 15];
			w.ZoomTransition = new( 0.1f, 0.1f );
			w.SecondaryDelay = 0.3f;
			w.ZoomedMaxSpeed = 150;
			w.HasScope = true;
			w.StartingAmmo = 30;
			configure?.Invoke( w );
		} );

	static IEnumerable<EntityDefinition> Pistol( string name, string title, string model, Action<Firearm> configure = null ) =>
		Gun( name, title, model, w =>
		{
			w.PreferredSlot = 1;
			w.HoldType = "pistol";
			w.ShotPunch = 2;
			w.Ballistics = w.Ballistics with { Damage = 25, Force = 300, SpreadBase = new( 0.2f, 0.2f ), SpreadGrowth = new( 1, 1 ) };
			w.ClipMaxSize = 13;
			w.Npc = w.Npc with { MinRange = 24, MaxRange = 1500 };
			w.PrimaryAutomatic = false;
			w.PrimaryDelay = 0.15f;
			w.ReloadTime = 2.2f;
			w.StartingAmmo = 52;
			configure?.Invoke( w );
		} );

	static IEnumerable<EntityDefinition> Shotgun( string name, string title, string model, int minimumPunch = 4 ) =>
		Gun<ShotgunWeapon>( name, title, model, w =>
		{
			w.Ballistics = w.Ballistics with { Damage = 20, Pellets = 8, Range = 3000, Force = 300, SpreadBase = new( 4, 4 ), SpreadGrowth = new( 0, 0 ) };
			w.FiresUnderwater = false;
			w.MinimumPitchPunch = minimumPunch;
			w.ClipMaxSize = 8;
			w.HoldType = "shotgun";
			w.Npc = w.Npc with { MaxRange = 500 };
			w.PrimaryDelay = 0.9f;
			w.ReloadTime = 1.5f;
			w.StartingAmmo = 32;
		} );

	static IEnumerable<EntityDefinition> Smg( string name, string title, string model, Firearm.KickbackSettings kickback ) =>
		Gun( name, title, model, w =>
		{
			w.Kickback = kickback;
			w.Ballistics = w.Ballistics with { Damage = 26, Force = 300, SpreadBase = new( 0.4f, 0.4f ), SpreadGrowth = new( 1, 1 ) };
			w.HoldType = "smg";
			w.PrimaryDelay = 0.08f;
			w.ReloadTime = 3;
			w.StartingAmmo = 120;
		} );

	static IEnumerable<EntityDefinition> Gun( string name, string title, string model, Action<Firearm> configure ) =>
		Gun<Firearm>( name, title, model, configure );

	static IEnumerable<EntityDefinition> Gun<T>( string name, string title, string model, Action<T> configure ) where T : Firearm, new() =>
		EntityPrefab.WeaponSet<T>( "css", name, title, $"models/weapons/w_{model}.mdl",
			$"models/weapons/cstrike/c_{model}.mdl", w =>
			{
				w.DisplayName = $"CS:S {title}";
				w.TracerFrequency = 0;
				w.Ballistics = w.Ballistics with { Radius = 0 };
				w.SecondaryDelay = Rules.ToggleDelay;
				configure( w );
			}, fallback: $"models/weapons/v_{model}.mdl" );
}
