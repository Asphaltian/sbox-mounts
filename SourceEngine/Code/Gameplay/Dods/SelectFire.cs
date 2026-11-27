namespace Source1.Dods;

public sealed class SelectFire : Firearm
{
	[Sync, Change( nameof( OnModeChanged ) )] public bool SemiAuto { get; private set; }

	TimeSince _sinceSwitch;
	internal override bool UsesDeployedClips => !SemiAuto;
	protected override float? Accuracy => SemiAuto ? Setup.Data?.SecondaryAccuracy ?? base.Accuracy : base.Accuracy;

	public override bool HasSecondaryAmmo() => true;
	public override bool CanSecondaryAttack() => !IsDeploying && !IsReloading && NextSecondaryFire <= 0;

	public override void SecondaryAttack()
	{
		SemiAuto = !SemiAuto;
		SetNextFire( Duration( SemiAuto ? "ACT_VM_UNDEPLOY" : "ACT_VM_DEPLOY" ) );
	}

	void OnModeChanged( bool before, bool semi )
	{
		_sinceSwitch = 0;
		PrimaryAutomatic = !semi;
		if ( Setup.Data is { } data )
			PrimaryDelay = (semi ? data.SecondaryCycleTime : data.CycleTime) ?? PrimaryDelay;

		var reload = Duration( semi ? "ACT_VM_RELOAD" : "ACT_VM_RELOAD_DEPLOYED" );
		if ( reload > 0 ) ReloadTime = reload;
		(WeaponModel as ViewModel)?.OnDeployChanged( !semi );
	}

	float Duration( string activity )
	{
		var model = Setup.ViewModel;
		return model.IsValid() ? WeaponClips.Duration( model, ModelSequences.Of( model ).FindActivity( activity ) ) : 0;
	}

	internal override Vector3 ViewOffset( Vector3 offset )
	{
		var blend = ((float)_sinceSwitch / 0.3f).Clamp( 0, 1 );
		return SightOffset( offset, SemiAuto ? blend : 1 - blend );
	}

	protected override bool OnDrop()
	{
		SemiAuto = false;
		return base.OnDrop();
	}
}
