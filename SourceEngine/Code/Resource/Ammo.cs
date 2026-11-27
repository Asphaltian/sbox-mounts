namespace Source1;

internal sealed class AmmoLoader( string name, int maxReserve ) : ResourceLoader<SourceGameMount>
{
	protected override object Load()
	{
		var ammo = Host.CreateAmmoResource();
		ammo.Title = name;
		ammo.MaxReserve = maxReserve;
		return ammo;
	}
}
