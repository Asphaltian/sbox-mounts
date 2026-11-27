namespace Source1.Dods.Prefabs;

[Library]
public static class Weapons
{
	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> All() =>
		[
			.. EntityPrefab.WeaponSet<ObjectiveBomb>( "dods", "basebomb", "TNT Charge", "models/weapons/p_tnt.mdl", "models/weapons/v_tnt.mdl",
				w =>
				{
					w.DisplayName = "TNT Charge";
					w.HoldType = "grenade";
					w.UsesAmmo = w.UsesClips = false;
					w.PrimaryAutomatic = w.SecondaryAutomatic = true;
					w.PrimaryDelay = w.SecondaryDelay = 0.1f;
				},
				view => view.ReplaceHands = false ),
			new EntityDefinition( "dods", EntityKind.Pickup, "bomb_target", "TNT Target", BombTarget.TargetModel, null )
			{
				Build = p =>
				{
					p.AddModel( p.Root );
					p.Root.AddComponent<BoxCollider>().Scale = new( 16, 12, 10 );
					p.Root.AddComponent<BombTarget>();
				}
			},
			.. Gun<RocketLauncher>( "bazooka", "Bazooka", "rpg", 0, configure: w => w.RocketModel = "models/weapons/w_bazooka_rocket.mdl" ),
			.. Gun<RocketLauncher>( "pschreck", "Panzerschreck", "rpg", 0, viewModel: "panzerschreck", configure: w => w.RocketModel = "models/weapons/w_panzerschreck_rocket.mdl" ),
			.. Gun<BipodGun>( "30cal", "Browning .30 Cal", "rifle", 9000, automatic: true ),
			.. Gun<Mg42>( "mg42", "MG42", "rifle", 9000, automatic: true, worldModel: "mg42bu" ),
			.. Gun<Firearm>( "colt", "Colt M1911", "pistol", 5000, emptyReload: true ),
			.. Gun<Firearm>( "p38", "Walther P38", "pistol", 5000 ),
			.. Gun<Firearm>( "c96", "Mauser C96", "pistol", 5000, automatic: true, emptyReload: true ),
			.. Gun<Firearm>( "m1carbine", "M1 Carbine", "rifle", 9000, worldModel: "m1carb" ),
			.. Gun<PunchWeapon>( "thompson", "Thompson", "smg", 7000, automatic: true ),
			.. Gun<ScopedRifle>( "spring", "Springfield M1903", "rifle", 9000, viewModel: "springfield", configure: w =>
			{
				w.ScopeSprite = "scope_spring";
				w.DrawScopeLines = true;
			} ),
			.. Gun<ScopedRifle>( "k98_scoped", "Scoped Kar98k", "rifle", 9000, worldModel: "k98s", configure: w => w.ScopeSprite = "scope_k43" ),
			.. Gun<BoltRifle>( "k98", "Karabiner 98k", "rifle", 9000 ),
			.. Gun<Garand>( "garand", "M1 Garand", "rifle", 9000 ),
			.. Gun<SelectFire>( "bar", "BAR", "rifle", 9000, automatic: true ),
			.. Gun<SelectFire>( "mp44", "MP44", "rifle", 7000, automatic: true ),
			.. Gun<PunchWeapon>( "mp40", "MP40", "smg", 7000, automatic: true ),
			.. RifleGrenades( "riflegren_us", "M1 Garand", "garand", "garand_gren" ),
			.. RifleGrenades( "riflegren_ger", "Kar98k", "k98", "k98_rg" ),
			.. Grenade( "frag_ger", "German Stick Grenade", "stick", Projectile.Kinds.DodFrag, liveName: "frag_ger_live" ),
			.. Grenade( "frag_ger_live", "Live German Stick Grenade", "stick", Projectile.Kinds.DodFrag, liveName: "frag_ger_live" ),
			.. Grenade( "frag_us", "US Frag Grenade", "frag", Projectile.Kinds.DodFrag, liveName: "frag_us_live" ),
			.. Grenade( "frag_us_live", "Live US Frag Grenade", "frag", Projectile.Kinds.DodFrag, liveName: "frag_us_live" ),
			.. Grenade( "smoke_ger", "German Smoke Grenade", "smoke_ger", Projectile.Kinds.DodSmoke, 10 ),
			.. Grenade( "smoke_us", "US Smoke Grenade", "smoke_us", Projectile.Kinds.DodSmoke, 10 ),
			.. Melee( "amerknife", "American Knife", "amerk" ),
			.. Melee( "spade", "Spade", "spade" )
		];

	static IEnumerable<EntityDefinition> RifleGrenades( string name, string title, string rifle, string world )
	{
		var live = $"{name}_live";
		var projectile = $"{rifle}_rg_grenade";
		return [
			.. Gun<RifleGrenade>( name, $"{title} Rifle Grenade", "rifle", 0, worldModel: world, viewModel: $"{rifle}_rg", configure: w =>
			{
				w.GrenadeModel = $"models/weapons/w_{projectile}.mdl";
				w.LivePrefab = $"mount://dods/entities/weapons/{live}.prefab";
			} ),
			.. Grenade( live, $"Live {title} Rifle Grenade", projectile, Projectile.Kinds.DodFrag, 3.5f, live, $"{rifle}_rglive" )
		];
	}

	static IEnumerable<EntityDefinition> Grenade( string name, string title, string model, Projectile.Kinds kind, float fuse = 5, string liveName = null, string viewModel = null ) =>
		EntityPrefab.WeaponSet<HandGrenade>( "dods", name, title, $"models/weapons/p_{model}.mdl", $"models/weapons/v_{viewModel ?? model}.mdl", w =>
		{
			w.DisplayName = title;
			w.HoldType = "grenade";
			w.Kind = kind;
			w.Live = name == liveName;
			w.LivePrefab = liveName is null ? null : $"mount://dods/entities/weapons/{liveName}.prefab";
			w.FuseTime = fuse;
			w.ThrownModel = $"models/weapons/w_{model}.mdl";
		}, view => view.ReplaceHands = false );

	static IEnumerable<EntityDefinition> Melee( string name, string title, string model ) =>
		EntityPrefab.WeaponSet<Melee>( "dods", name, title, $"models/weapons/w_{model}.mdl", $"models/weapons/v_{model}.mdl", w =>
		{
			w.DisplayName = title;
			w.HoldType = "knife";
			w.Range = 48;
		}, view => view.ReplaceHands = false );

	static IEnumerable<EntityDefinition> Gun<T>( string name, string title, string hold, float force, bool automatic = false,
		bool emptyReload = false, string worldModel = null, string viewModel = null, System.Action<T> configure = null ) where T : Firearm, new() =>
		EntityPrefab.WeaponSet<T>( "dods", name, title, $"models/weapons/w_{worldModel ?? name}.mdl", $"models/weapons/v_{viewModel ?? name}.mdl", w =>
		{
			w.DisplayName = title;
			w.HoldType = hold;
			w.PrimaryAutomatic = automatic;
			w.Ballistics = w.Ballistics with { Force = force };
			configure?.Invoke( w );
		}, view =>
		{
			view.ReplaceHands = false;
			view.UseEmptyReload = emptyReload;
		} );
}
