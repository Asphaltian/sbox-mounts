using Sandbox.Npcs.Layers;

namespace Source1.Css.Prefabs;

[Library]
public static class Npcs
{
	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> All()
	{
		yield return Team( "counter_terrorist", "Counter-Terrorist", "ct_urban", "Idle_Upper_M4", "CT", true, "m4a1",
			["ct_gign", "ct_gsg9", "ct_sas", "ct_urban"] );
		yield return Team( "terrorist", "Terrorist", "t_leet", "Idle_Upper_AK", "TERRORIST", false, "ak47",
			["t_arctic", "t_guerilla", "t_leet", "t_phoenix"] );
	}

	static EntityDefinition Team( string name, string title, string preview, string pose, string team, bool friendly, string weapon, string[] skins )
	{
		static string Model( string skin ) => $"models/player/{skin}.mdl";
		var models = skins.Select( Model ).ToList();
		return new( "css", EntityKind.Npc, name, title, Model( preview ), pose )
		{
			ModelPath = models[0],
			RequiredPrefabs = [$"entities/weapons/{weapon}.prefab"],
			Build = p =>
			{
				p.Root.AddComponent<PlayerAnimState>();
				var npc = p.Npc<CombatNpc>();
				p.Navigation( radius: 16, run: 180, walk: 85 );
				p.Root.GetComponent<SensesLayer>( true ).SightRange = 8192;
				var choice = p.Root.AddComponent<SpawnChoice>();
				choice.ModelChoices = [.. models];
				choice.Team = team;

				npc.Friendly = friendly;
				npc.PatrolRadius = 2048;
				npc.SearchTimeout = 20;

				npc.WeaponChoices = p.WeaponChoices( "glock", "usp", "p228", "deagle", "fiveseven", "elite", "mac10", "tmp", "mp5navy", "ump45", "p90",
					"galil", "famas", "ak47", "m4a1", "sg552", "aug", "scout", "awp", "g3sg1", "sg550", "m3", "xm1014", "m249" );
				p.Root.AddComponent<PlayerSounds>();
				p.Box( 32, 72 );
				npc.Weapon = p.Equip( weapon );
			}
		};
	}
}
