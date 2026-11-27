namespace Source1.GarrysMod;

public sealed class GameRules : Source1.GameRules
{
	public override DuplicatePickup DuplicateWeapons => Game.Ident != "facepunch.sandbox" ? DuplicatePickup.Inventory
		: Ammo.MaxAmmo > 0 ? DuplicatePickup.AlwaysConsume : DuplicatePickup.TakeAmmo;
}
