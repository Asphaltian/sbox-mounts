namespace Source1.GarrysMod;

/// <summary>HL2 ammunition uses the Sandbox reserve override while playing Sandbox.</summary>
public sealed class Ammo : BaseAmmoResource
{
	[ConVar( "gmod_maxammo", ConVarFlags.Server | ConVarFlags.Replicated, Max = 99999, Help = "Override mounted HL2 ammo limits in Sandbox. Zero uses each ammo type's carry limit." )]
	public static int MaxAmmo { get; set; } = 9999;

	internal static bool? ConsumeDuplicate => Game.Ident == "facepunch.sandbox" ? MaxAmmo > 0 : null;

	public override int MaxReserve
	{
		get => ConsumeDuplicate == true ? System.Math.Min( MaxAmmo, 99999 ) : base.MaxReserve;
		set => base.MaxReserve = value;
	}
}
