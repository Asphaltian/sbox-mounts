namespace Source1.Dods;

public sealed class Mount : SourceGameMount
{
	protected override string ContentDirectory => "dod";
	public override string Ident => "dods";
	public override string Title => "Day of Defeat: Source";
	public override long AppId => 300;
	protected override string WeaponScriptKey => "Wl0u5B3F";
	protected internal override bool UseDrawSequenceDuration => true;

	private protected override int? AmmoCarryLimit( string ammo ) => ammo switch
	{
		"DOD_AMMO_RIFLEGRENADE_GER" or "DOD_AMMO_RIFLEGRENADE_GER_LIVE" or
		"DOD_AMMO_RIFLEGRENADE_US" or "DOD_AMMO_RIFLEGRENADE_US_LIVE" => 2,
		"DOD_AMMO_HANDGRENADE_EX" or "DOD_AMMO_STICKGRENADE_EX" => 1,
		"DOD_AMMO_STICKGRENADE" or "DOD_AMMO_HANDGRENADE" or "DOD_AMMO_SMOKEGRENADE_US" or "DOD_AMMO_SMOKEGRENADE_GER" => 2,
		"DOD_AMMO_ROCKET" => 5,
		"DOD_AMMO_30CAL" => 300,
		"DOD_AMMO_MG42" => 500,
		"DOD_AMMO_SPRING" => 55,
		"DOD_AMMO_K98" => 65,
		"DOD_AMMO_GARAND" => 88,
		"DOD_AMMO_BAR" => 260,
		"DOD_AMMO_SUBMG" => 210,
		"DOD_AMMO_COLT" => 21,
		"DOD_AMMO_P38" => 24,
		"DOD_AMMO_C96" => 60,
		"DOD_AMMO_M1CARBINE" => 165,
		_ => null
	};

	internal override void ApplyWeaponDefaults( BaseCombatWeapon weapon, WeaponSetup setup, string worldModel )
	{
		if ( weapon is not BulletWeapon gun || setup.Data is not { } data ) return;
		if ( data.Recoil is { } recoil )
		{
			gun.RecoilPitch = new( -recoil, -recoil );
			gun.RecoilYaw = new( -recoil / 4, recoil / 4 );
			gun.PunchRoll = Vector2.Zero;
		}
	}
}
