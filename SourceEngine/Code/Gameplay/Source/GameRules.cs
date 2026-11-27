namespace Source1;

public class GameRules
{
	public enum DuplicatePickup { Inventory, TakeAmmo, AlwaysConsume }

	public virtual DuplicatePickup DuplicateWeapons => DuplicatePickup.Inventory;
	public float DroppedWeaponPickupDelay { get; init; }
}
