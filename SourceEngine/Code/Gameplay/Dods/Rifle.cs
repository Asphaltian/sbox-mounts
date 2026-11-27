namespace Source1.Dods;

public class Rifle : Firearm
{
	[Sync, Change( nameof( OnAimChanged ) )] public bool Aiming { get; protected set; }

	TimeSince _sinceAimChanged = 1;
	protected virtual float SightFov => 55;
	protected bool Sighted => Aiming && _sinceAimChanged >= 0.3f;
	protected override float? Accuracy => Sighted ? Setup.Data?.SecondaryAccuracy ?? base.Accuracy : base.Accuracy;

	public override bool HasSecondaryAmmo() => true;
	public override bool CanSecondaryAttack() => !IsDeploying && !IsReloading && NextSecondaryFire <= 0;

	public override void SecondaryAttack()
	{
		Aiming = !Aiming;
		SetNextPrimaryFire( System.Math.Max( NextPrimaryFire, 0.5f ) );
		SetNextSecondaryFire( 0.5f );
	}

	void OnAimChanged( bool before, bool after ) => _sinceAimChanged = 0;

	protected override void OnHolstered()
	{
		Aiming = false;
		base.OnHolstered();
	}

	protected override void OnReloadStarted()
	{
		Aiming = false;
		base.OnReloadStarted();
	}

	internal override Vector3 ViewOffset( Vector3 offset ) => SightOffset( offset, AimFraction( 0, 0.3f ) );

	float AimFraction( float delay, float duration )
	{
		var t = (((float)_sinceAimChanged - delay) / duration).Clamp( 0, 1 );
		return Aiming ? t : 1 - t;
	}

	protected override void ModifyCamera( CameraComponent camera, ref CameraView view )
	{
		base.ModifyCamera( camera, ref view );
		view.FieldOfView = float.Lerp( view.FieldOfView, ZoomedFov( view.FieldOfView, SightFov ), AimFraction( Aiming ? 0.3f : 0, 0.1f ) );
	}

	public override void DrawCrosshair( Painter painter, Vector2 center )
	{
		if ( !Aiming ) base.DrawCrosshair( painter, center );
	}
}
