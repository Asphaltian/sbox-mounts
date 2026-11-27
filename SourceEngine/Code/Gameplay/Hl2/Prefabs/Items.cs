namespace Source1.Hl2.Prefabs;

[Library]
public static class Items
{
	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> All()
	{
		yield return Ammo( "ammo_357", ".357 Ammo", "models/items/357ammo.mdl", "357", 6 );
		yield return Ammo( "ammo_ar2", "Pulse Rifle Ammo", "models/items/combine_rifle_cartridge01.mdl", "ar2", 20 );
		yield return Ammo( "ammo_ar2_altfire", "Pulse Rifle Orb", "models/items/combine_rifle_ammo01.mdl", "ar2altfire", 1 );
		yield return Ammo( "ammo_buckshot", "Shotgun Ammo", "models/items/boxbuckshot.mdl", "buckshot", 20 );
		yield return Ammo( "ammo_crossbow", "Crossbow Bolts", "models/items/crossbowrounds.mdl", "xbowbolt", 6 );
		yield return Ammo( "ammo_pistol", "Pistol Ammo", "models/items/boxsrounds.mdl", "pistol", 20 );
		yield return Ammo( "ammo_rpg", "RPG Rocket", "models/weapons/w_missile_closed.mdl", "rpg_round", 1 );
		yield return Ammo( "ammo_smg1", "SMG Ammo", "models/items/boxmrounds.mdl", "smg1", 45 );
		yield return Ammo( "ammo_smg1_grenade", "SMG Grenade", "models/weapons/ar2_grenade.mdl", "smg1_grenade", 1 );

		yield return new( "hl2", EntityKind.Pickup, "battery", "Suit Battery", "models/items/battery.mdl", null )
		{
			Build = p =>
			{
				var pickup = BuildPickup( p );
				pickup.Kind = Pickup.Kinds.Armour;
				pickup.SoundName = "ItemBattery.Touch";
				pickup.AmountSetting = "sk_battery";
				pickup.Amount = 15;
			}
		};

		yield return new( "hl2", EntityKind.Pickup, "healthkit", "Health Kit", "models/items/healthkit.mdl", null )
		{
			Build = p =>
			{
				var pickup = BuildPickup( p );
				pickup.Kind = Pickup.Kinds.Health;
				pickup.SoundName = "HealthKit.Touch";
				pickup.AmountSetting = "sk_healthkit";
				pickup.Amount = 25;
			}
		};

		yield return new( "hl2", EntityKind.Pickup, "healthvial", "Health Vial", "models/healthvial.mdl", null )
		{
			Build = p =>
			{
				var pickup = BuildPickup( p );
				pickup.Kind = Pickup.Kinds.Health;
				pickup.SoundName = "HealthVial.Touch";
				pickup.AmountSetting = "sk_healthvial";
				pickup.Amount = 10;
			}
		};

		yield return new( "hl2", EntityKind.Charger, "health_charger", "Health Charger", "models/props_combine/health_charger001.mdl", null )
		{
			Build = p =>
			{
				var charger = BuildCharger( p );
				charger.CapacitySetting = "sk_healthcharger";
				charger.StartSound = "WallHealth.Start";
				charger.LoopSound = "WallHealth.LoopingContinueCharge";
				charger.DenySound = "WallHealth.Deny";
			}
		};

		yield return new( "hl2", EntityKind.Charger, "suit_charger", "Suit Charger", "models/props_combine/suit_charger001.mdl", null )
		{
			Build = p =>
			{
				var charger = BuildCharger( p );
				charger.Armour = true;
				charger.CapacitySetting = "sk_suitcharger";
				charger.StartSound = "SuitRecharge.Start";
				charger.LoopSound = "SuitRecharge.ChargingLoop";
				charger.DenySound = "SuitRecharge.Deny";
			}
		};
	}

	static EntityDefinition Ammo( string name, string title, string model, string ammo, int amount ) =>
		new( "hl2", EntityKind.Pickup, name, title, model, null )
		{
			Build = p =>
			{
				var pickup = BuildPickup( p );
				pickup.AmmoName = ammo;
				pickup.Amount = amount;
			}
		};

	static Pickup BuildPickup( EntityPrefab p )
	{
		p.Root.Tags.Add( "prop", "removable" );
		p.Root.NetworkMode = NetworkMode.Object;
		p.Root.Network.SetOrphanedMode( NetworkOrphaned.ClearOwner );
		var pickup = p.Root.AddComponent<Pickup>();
		pickup.Kind = Pickup.Kinds.Ammo;
		pickup.SoundName = "BaseCombatCharacter.AmmoPickup";
		var collider = p.Root.AddComponent<ModelCollider>();
		p.Root.AddComponent<Rigidbody>().EnableImpactDamage = false;
		p.AddModel( new GameObject( p.Root, true, "model" ) );
		p.Model.Collider = collider;
		return pickup;
	}

	static Charger BuildCharger( EntityPrefab p )
	{
		p.Root.Tags.Add( "prop", "removable" );
		p.Root.NetworkMode = NetworkMode.Object;
		var charger = p.Root.AddComponent<Charger>();
		p.AddModel( p.Root );
		p.Model.Collider = p.Root.AddComponent<ModelCollider>();
		p.Model.Collider.Static = true;
		return charger;
	}
}
