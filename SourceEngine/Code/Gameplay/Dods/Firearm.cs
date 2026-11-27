using System;

namespace Source1.Dods;

public class Firearm : BulletWeapon, PlayerController.IEvents
{
	internal override string BrassForEvent( SourceModelSequenceData.Event e ) => e.Id == 6002 ? e.Options switch
	{
		"0" => "models/shells/shell_small.mdl",
		"1" => "models/shells/shell_medium.mdl",
		"2" or "3" => "models/shells/shell_large.mdl",
		"4" => "models/shells/garand_clip.mdl",
		_ => null
	} : null;

	protected override bool UseSpreadDisk => true;

	protected virtual float LookScale => 1;

	void PlayerController.IEvents.OnEyeAngles( ref Angles input )
	{
		if ( IsActive ) input *= LookScale;
	}

	protected virtual float? Accuracy => Setup.Data?.Accuracy;

	protected Vector3 SightOffset( Vector3 offset, float blend ) => Setup.Data?.IronSightOffset is { } sight
		? Vector3.Lerp( offset, new( sight.x, -sight.y, sight.z ), blend ) : offset;

	public override Vector2 CurrentSpread
	{
		get
		{
			if ( Accuracy is not { } cone ) return base.CurrentSpread;
			if ( Owner.IsValid() && Owner.Velocity.WithZ( 0 ).LengthSquared > 45 * 45 )
				cone += Setup.Data.AccuracyMovePenalty ?? 0;
			var degrees = 2 * MathF.Asin( Math.Clamp( cone, 0, 1 ) ).RadianToDegree();
			return new Vector2( degrees, degrees ) * SpreadScale;
		}
	}
}
