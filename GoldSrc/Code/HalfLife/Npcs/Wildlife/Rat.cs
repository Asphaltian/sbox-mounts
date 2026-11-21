namespace GoldSrc.HalfLife;

[Title( "Rat" ), Category( "Half-Life" )]
[Alias( "GoldSrc.HalfLifeRat" )]
[NpcDefinition( "monster_rat", "bigrat", MonsterClass.Insect, 8, 5, 8, 55, Gibs = GibSet.Alien, Blood = BloodColor.Red )]
public sealed class Rat : Npc
{
	internal override void OnModelLoaded( ModelLoader loader ) => EyeHeight = loader.EyePosition.z;
}
