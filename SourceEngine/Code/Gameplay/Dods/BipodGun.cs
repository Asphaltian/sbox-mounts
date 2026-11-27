namespace Source1.Dods;

public class BipodGun : Firearm
{
	[Sync, Change( nameof( OnDeploymentChanged ) )] public bool Deployed { get; private set; }

	Vector3 _rest;
	TimeUntil _checkRest;
	protected override float LookScale => Deployed ? 0.9f : 1;
	internal override bool UsesDeployedClips => Deployed;
	protected override float? Accuracy => Deployed ? Setup.Data?.SecondaryAccuracy : base.Accuracy;

	public override void DrawCrosshair( Painter painter, Vector2 center )
	{
		if ( Deployed ) base.DrawCrosshair( painter, center );
	}

	public override bool CanReload() => Deployed && base.CanReload();
	public override bool HasSecondaryAmmo() => true;
	public override bool CanSecondaryAttack() => !IsDeploying && NextSecondaryFire <= 0;

	public override void SecondaryAttack()
	{
		if ( !Deployed )
		{
			if ( !FindRest( out _rest ) ) return;
			_checkRest = 0.2f;
		}
		Deployed = !Deployed;
	}

	bool FindRest( out Vector3 rest )
	{
		var ray = AimRay;
		var forward = ray.Forward.WithZ( 0 ).Normal;
		var start = ray.Position + forward * 32;
		var trace = Scene.Trace.Ray( start, start + Vector3.Down * 32 ).Size( 2 )
			.IgnoreGameObjectHierarchy( AimIgnoreRoot ).WithoutTags( "trigger" ).Run();
		rest = trace.EndPosition;
		return trace.Hit && !trace.StartedSolid && trace.Normal.z > 0.5f;
	}

	protected override void Think()
	{
		base.Think();
		if ( !Deployed || _checkRest > 0 ) return;
		_checkRest = 0.2f;
		if ( !FindRest( out var rest ) || rest.Distance( _rest ) > 16 ) Deployed = false;
	}

	void OnDeploymentChanged( bool before, bool deployed )
	{
		if ( !deployed && IsReloading ) CancelReload();
		var model = Setup.ViewModel;
		if ( model.IsValid() )
		{
			var sequences = ModelSequences.Of( model );
			SetNextFire( WeaponClips.Duration( model, sequences.FindActivity( deployed ? "ACT_VM_DEPLOY" : "ACT_VM_UNDEPLOY" ) ) );

		}
		if ( Setup.Data?.Recoil is { } recoil )
		{
			RecoilPitch = deployed ? Vector2.Zero : new( -recoil, -recoil );
			RecoilYaw = deployed ? Vector2.Zero : new( -recoil / 4, recoil / 4 );
		}
		(WeaponModel as ViewModel)?.OnDeployChanged( deployed );
	}

	protected override void OnHolstered()
	{
		Deployed = false;
		base.OnHolstered();
	}
}
