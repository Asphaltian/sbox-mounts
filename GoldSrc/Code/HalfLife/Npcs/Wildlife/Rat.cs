namespace GoldSrc.HalfLife;

[Title( "Rat" ), Category( "Half-Life" )]
[Alias( "GoldSrc.HalfLifeRat" )]
[NpcDefinition( "monster_rat", "bigrat", MonsterClass.Insect, 8, 5, 8, 55, Gibs = GibSet.Alien, Blood = BloodColor.Red )]
public sealed class Rat : Npc
{
	// CRat uses the base idle-stand schedule until it has a route.
	internal override void IdleMovement() { }

	internal override void OnModelLoaded( ModelLoader loader ) => EyeHeight = loader.EyePosition.z;
}
