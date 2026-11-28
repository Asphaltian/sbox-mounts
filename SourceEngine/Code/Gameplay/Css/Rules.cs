using System;

namespace Source1.Css;

/// <summary>
/// Counter-Strike: Source's code constants and the choices its weapon scripts leave to code, for the shared
/// mounted weapons. Garry's Mod's copy of the content mounts as the same game, without the scripts, so the
/// weapon family read from a model name stands in for the script's WeaponType.
/// </summary>
static class Rules
{
	public const string Mount = "css";

	/// <summary>
	/// Approximation: Counter-Strike gives bots the player's damage, which ends an NPC firefight in a second or two.
	/// HL2's skill.cfg has NPC bullets hit for about 3 (sk_npc_dmg_smg1, _ar2, _pistol, _buckshot), a tenth of a
	/// Counter-Strike rifle round; at 0.15 the weapons keep their differences and a duel at 600 units lasts 3-12 seconds.
	/// </summary>
	public const float NpcDamageScale = 0.15f;

	/// <summary>CWeaponCSBaseGun toggles zoom, burst and silencer at most every 0.3s.</summary>
	public const float ToggleDelay = 0.3f;

	/// <summary>MuzzleFlashStyle values, used as flash types.</summary>
	public const string Flash = "CS_MUZZLEFLASH_NORM";
	public const string FlashX = "CS_MUZZLEFLASH_X";
	public const string FlashNone = "CS_MUZZLEFLASH_NONE";

	/// <summary>True for a weapon made from Counter-Strike: Source content.</summary>
	public static bool Owns( BaseCombatWeapon weapon ) => MountContent.OfWeapon( weapon ) == Mount;

	public static bool IsFlash( string type ) => type is Flash or FlashX;

	/// <summary>
	/// A weapon family's usual MuzzleFlashStyle and MuzzleFlashScale, for Garry's Mod's copy of the content,
	/// which has no scripts: the X flash for rifles, machine guns and automatic snipers, the plain one otherwise.
	/// </summary>
	public static (string Type, float Scale)? FlashFor( string weaponType, bool automatic ) => weaponType switch
	{
		"Pistol" => (Flash, 1),
		"SubMachinegun" => (Flash, 1.1f),
		"Rifle" => (FlashX, 1.5f),
		"SniperRifle" => automatic ? (FlashX, 1.5f) : (Flash, 1.35f),
		"Shotgun" => (Flash, 1.3f),
		"Machinegun" => (FlashX, 1.5f),
		_ => null
	};

	/// <summary>The scripts name no empty sound, the code clicks by weapon family.</summary>
	public static string EmptySound( string weaponType ) => weaponType == "Pistol" ? "Default.ClipEmpty_Pistol" : "Default.ClipEmpty_Rifle";

	/// <summary>The weapon family a world model's name carries: w_rif_ak47 is a Rifle.</summary>
	public static string WeaponTypeOf( string model )
	{
		var parts = System.IO.Path.GetFileNameWithoutExtension( model ?? "" ).Split( '_' );
		if ( parts.Length < 3 ) return null;
		return parts[1].ToLowerInvariant() switch
		{
			"pist" => "Pistol",
			"smg" => "SubMachinegun",
			"rif" => "Rifle",
			"snip" => "SniperRifle",
			"shot" => "Shotgun",
			"mach" => "Machinegun",
			"knife" => "Knife",
			"eq" => "Grenade",
			_ => null
		};
	}

}
