namespace Source1.Css;

sealed class Accuracy( VmtParameters script ) : SourceModelWeaponData
{
	public readonly AccuracyProfile Primary = new( script, "" );
	public readonly AccuracyProfile Alternate = script.Contains( "SpreadAlt" ) ? new( script, "Alt" ) : null;
}

sealed class AccuracyProfile( VmtParameters script, string suffix )
{
	public readonly float Spread = script.Float( "Spread" + suffix );
	public readonly float Stand = script.Float( "InaccuracyStand" + suffix );
	public readonly float Crouch = script.Float( "InaccuracyCrouch" + suffix );
	public readonly float Move = script.Float( "InaccuracyMove" + suffix );
	public readonly float Fire = script.Float( "InaccuracyFire" + suffix );
	public readonly float Jump = script.Float( "InaccuracyJump" + suffix );
	public readonly float Land = script.Float( "InaccuracyLand" + suffix );
	public readonly float Ladder = script.Float( "InaccuracyLadder" + suffix );
	public readonly float Reload = script.Float( "InaccuracyReload" );
	public readonly float RecoveryStand = script.Float( "RecoveryTimeStand", 1 );
	public readonly float RecoveryCrouch = script.Float( "RecoveryTimeCrouch", 1 );
	public readonly float MaxSpeed = script.Float( "MaxPlayerSpeed", 1 );
}
