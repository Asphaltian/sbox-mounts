using System;

namespace Source1.Css.Prefabs;

[Library]
public static class Weapons
{
	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> Ak47() =>
		Rifle( "ak47", "AK-47", "rif_ak47" );

	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> Aug() =>
		Rifle( "aug", "AUG", "rif_aug" );

	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> Awp() =>
		SniperRifle( "awp", "AWP", "snip_awp", w => w.ZoomLevels = [40, 10] );

	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> Deagle() =>
		Pistol( "deagle", "Desert Eagle", "pist_deagle" );

	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> Elite() =>
		Pistol( "elite", "Dual Elites", "pist_elite", w => w.HoldType = "dual_pistols" );

	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> Famas() =>
		Rifle( "famas", "FAMAS", "rif_famas", w => w.Secondary = Weapon.SecondaryFires.Burst );

	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> Fiveseven() =>
		Pistol( "fiveseven", "Five-SeveN", "pist_fiveseven" );

	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> Flashbang() =>
		Grenade( "flashbang", "Flashbang", "eq_flashbang", Projectile.Kinds.Flashbang );

	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> G3sg1() =>
		SniperRifle( "g3sg1", "G3SG1", "snip_g3sg1" );

	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> Galil() =>
		Rifle( "galil", "Galil", "rif_galil" );

	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> Glock() =>
		Pistol( "glock", "Glock 18", "pist_glock18" );

	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> Hegrenade() =>
		Grenade( "hegrenade", "HE Grenade", "eq_fraggrenade", Projectile.Kinds.HeGrenade );

	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> Knife() =>
		EntityPrefab.WeaponSet<Knife>( "css", "knife", "Knife",
			"models/weapons/w_knife_t.mdl", "models/weapons/cstrike/c_knife_t.mdl", w =>
			{
				w.Ballistics = w.Ballistics with { Damage = 8, Force = 300 };
				w.CanCancelReload = false;
				w.ClipMaxSize = 45;
				w.DeployTime = 0.2f;
				w.DisplayName = "CS:S Knife";
				w.Handedness = BaseCombatWeapon.WeaponHandedness.Right;
				w.HoldType = "knife";
				w.PreferredSlot = 2;
				w.PrimaryDelay = 0.075f;
				w.ReloadTime = 1.5f;
				w.SecondaryDelay = 1;
				w.StartingAmmo = 135;
				w.UsableByNpcs = false;
				w.Value = 10;
			}, v =>
			{
				v.Fire = "midslash1";
				v.SecondaryFire = "stab";
			}, fallback: "models/weapons/v_knife_t.mdl" );

	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> M249() =>
		MachineGun( "m249", "M249", "mach_m249para" );

	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> M3() =>
		Shotgun( "m3", "M3 Super 90", "shot_m3super90" );

	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> M4a1() =>
		Rifle( "m4a1", "M4A1", "rif_m4a1" );

	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> Mac10() =>
		Smg( "mac10", "MAC-10", "smg_mac10" );

	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> Mp5navy() =>
		Smg( "mp5navy", "MP5 Navy", "smg_mp5" );

	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> P228() =>
		Pistol( "p228", "P228", "pist_p228" );

	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> P90() =>
		Smg( "p90", "P90", "smg_p90" );

	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> Scout() =>
		SniperRifle( "scout", "Scout", "snip_scout" );

	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> Sg550() =>
		SniperRifle( "sg550", "SG 550", "snip_sg550" );

	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> Sg552() =>
		Rifle( "sg552", "SG 552", "rif_sg552" );

	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> Smokegrenade() =>
		Grenade( "smokegrenade", "Smoke Grenade", "eq_smokegrenade", Projectile.Kinds.SmokeGrenade );

	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> Tmp() =>
		Smg( "tmp", "TMP", "smg_tmp" );

	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> Ump45() =>
		Smg( "ump45", "UMP-45", "smg_ump45" );

	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> Usp() =>
		Pistol( "usp", "USP .45", "pist_usp" );

	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> Xm1014() =>
		Shotgun( "xm1014", "XM1014", "shot_xm1014" );

	static IEnumerable<EntityDefinition> Grenade( string name, string title, string model, Projectile.Kinds kind ) =>
		EntityPrefab.WeaponSet<ProjectileWeapon>( "css", name, title, $"models/weapons/w_{model}.mdl",
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

	static IEnumerable<EntityDefinition> Rifle( string name, string title, string model, Action<BulletWeapon> configure = null ) =>
		Gun( name, title, model, w =>
		{
			w.Ballistics = w.Ballistics with { Damage = 32, Range = 8192, Force = 300, SpreadBase = new( 0.25f, 0.25f ), SpreadGrowth = new( 1, 1 ) };
			w.HoldType = "rifle";
			w.Npc = w.Npc with { MinRange = 65, MaxRange = 2048 };
			w.ReloadTime = 2.5f;
			w.StartingAmmo = 90;
			configure?.Invoke( w );
		} );

	static IEnumerable<EntityDefinition> SniperRifle( string name, string title, string model, Action<BulletWeapon> configure = null ) =>
		Gun( name, title, model, w =>
		{
			w.Ballistics = w.Ballistics with { Damage = 75, Range = 8192, Force = 300, SpreadBase = new( 0.05f, 0.05f ), SpreadGrowth = new( 0, 0 ) };
			w.ClipMaxSize = 10;
			w.HoldType = "rifle";
			w.Npc = w.Npc with { MaxRange = 8192 };
			w.PrimaryAutomatic = false;
			w.PrimaryDelay = 1.25f;
			w.ReloadTime = 3;
			w.StartingAmmo = 30;
			configure?.Invoke( w );
		} );

	static IEnumerable<EntityDefinition> Pistol( string name, string title, string model, Action<BulletWeapon> configure = null ) =>
		Gun( name, title, model, w =>
		{
			w.PreferredSlot = 1;
			w.HoldType = "pistol";
			w.Ballistics = w.Ballistics with { Damage = 25, Force = 300, SpreadBase = new( 0.2f, 0.2f ), SpreadGrowth = new( 1, 1 ) };
			w.ClipMaxSize = 13;
			w.Npc = w.Npc with { MinRange = 24, MaxRange = 1500 };
			w.PrimaryAutomatic = false;
			w.PrimaryDelay = 0.15f;
			w.ReloadTime = 2.2f;
			w.StartingAmmo = 52;
			configure?.Invoke( w );
		} );

	static IEnumerable<EntityDefinition> MachineGun( string name, string title, string model ) =>
		Gun( name, title, model, w =>
		{
			w.Ballistics = w.Ballistics with { Damage = 32, Range = 8192, Force = 300, SpreadBase = new( 0.5f, 0.5f ), SpreadGrowth = new( 1, 1 ) };
			w.ClipMaxSize = 100;
			w.HoldType = "rifle";
			w.Npc = w.Npc with { MinRange = 65, MaxRange = 2048 };
			w.PrimaryDelay = 0.08f;
			w.ReloadTime = 5;
			w.StartingAmmo = 200;
		} );

	static IEnumerable<EntityDefinition> Shotgun( string name, string title, string model ) =>
		Gun( name, title, model, w =>
		{
			w.Ballistics = w.Ballistics with { Damage = 20, Pellets = 8, Range = 3000, Force = 300, SpreadBase = new( 4, 4 ), SpreadGrowth = new( 0, 0 ) };
			w.ClipMaxSize = 8;
			w.HoldType = "shotgun";
			w.Npc = w.Npc with { MaxRange = 500 };
			w.PrimaryDelay = 0.9f;
			w.ReloadTime = 1.5f;
			w.StartingAmmo = 32;
		} );

	static IEnumerable<EntityDefinition> Smg( string name, string title, string model ) =>
		Gun( name, title, model, w =>
		{
			w.Ballistics = w.Ballistics with { Damage = 26, Force = 300, SpreadBase = new( 0.4f, 0.4f ), SpreadGrowth = new( 1, 1 ) };
			w.HoldType = "smg";
			w.PrimaryDelay = 0.08f;
			w.ReloadTime = 3;
			w.StartingAmmo = 120;
		} );

	static IEnumerable<EntityDefinition> Gun( string name, string title, string model, Action<BulletWeapon> configure ) =>
		EntityPrefab.WeaponSet<BulletWeapon>( "css", name, title, $"models/weapons/w_{model}.mdl",
			$"models/weapons/cstrike/c_{model}.mdl", w =>
			{
				w.DisplayName = $"CS:S {title}";
				configure( w );
			}, fallback: $"models/weapons/v_{model}.mdl" );
}
