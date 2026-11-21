namespace GoldSrc.HalfLife;

public sealed partial class HumanGrunt
{
	IEnumerable<HumanGrunt> Squadmates() => Scene.GetAllComponents<HumanGrunt>().Where( grunt =>
		grunt != this && grunt.Health > 0 && WorldPosition.DistanceSquared( grunt.WorldPosition ) <= 1024 * 1024 );

	bool SquadBusyFiring( GameObject target ) => Squadmates().Where( grunt => grunt.IsAttacking( target ) ).Take( 2 ).Count() == 2;

	bool FriendlyInLine( GameObject target )
	{
		var aim = Rotation.LookAt( TargetHullCenter( target ) - WorldPosition );
		foreach ( var ally in Squadmates() )
		{
			var offset = ally.WorldPosition - WorldPosition;
			if ( aim.Forward.Dot( offset ) >= 0 && System.MathF.Abs( aim.Right.Dot( offset ) ) <= Definition.Radius * 3 * System.MathF.Abs( WorldTransform.UniformScale ) ) return true;
		}
		return false;
	}
}
