namespace Source1.Dods;

public class Rifle : Firearm
{
	[Sync, Change( nameof( OnAimChanged ) )] public bool Aiming { get; protected set; }

	TimeSince _sinceAimChanged = 1;
	float _zoomStart;
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

	void OnAimChanged( bool before, bool after )
	{
		_zoomStart = ZoomAt( before );
		_sinceAimChanged = 0;
	}

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

	internal override Vector3 ViewOffset( Vector3 offset )
	{
		var t = ((float)_sinceAimChanged / 0.3f).Clamp( 0, 1 );
		return SightOffset( offset, Aiming ? t : 1 - t );
	}

	protected float ZoomFraction => ZoomAt( Aiming );

	float ZoomAt( bool aiming )
	{
		var t = (((float)_sinceAimChanged - (aiming ? 0.3f : 0)) / 0.1f).Clamp( 0, 1 );
		return float.Lerp( _zoomStart, aiming ? 1 : 0, t * t * (3 - 2 * t) );
	}

	internal override float ViewModelFovOffset => (90 - SightFov) * ZoomFraction;
	internal override float ViewModelSway => Sighted ? 0 : 1;

	protected override float LookScale => float.Lerp( 1, SightFov / 90, ZoomFraction );

	protected override void ModifyCamera( CameraComponent camera, ref CameraView view )
	{
		base.ModifyCamera( camera, ref view );
		var fraction = ZoomFraction;
		if ( fraction <= 0 ) return;
		var rect = camera.ScreenRect;
		var aspect = rect.Height > 0 ? rect.Width / rect.Height : Screen.Aspect;
		var scale = camera.FovAxis == CameraComponent.Axis.Vertical ? 0.75f : aspect * 0.75f;
		view.FieldOfView = float.Lerp( view.FieldOfView, Screen.CreateVerticalFieldOfView( SightFov, scale ), fraction );
	}

	public override void DrawCrosshair( Painter painter, Vector2 center )
	{
		if ( !Aiming ) base.DrawCrosshair( painter, center );
	}
}
