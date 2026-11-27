namespace Source1.GarrysMod.Prefabs;

[Library]
public static class Weapons
{
	[Entities.Definitions]
	public static IEnumerable<EntityDefinition> Physgun() =>
		EntityPrefab.WeaponSet<PhysicsGun>( "gmod", "physgun", "Physics Gun",
			"models/weapons/w_physics.mdl", "models/weapons/c_superphyscannon.mdl", w =>
			{
				w.DisplayName = "Physics Gun";
				w.HoldType = "tool";
			}, v =>
			{
				v.Fire = "fire";
				v.Reload = null;
				v.EjectBrass = null;
			}, skin: "Skin 1" );
}
