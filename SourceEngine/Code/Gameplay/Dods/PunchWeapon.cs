namespace Source1.Dods;

public sealed class PunchWeapon : Firearm
{
	Swing _punch;

	public override bool HasSecondaryAmmo() => true;
	public override bool CanSecondaryAttack() => !IsDeploying && NextSecondaryFire <= 0;

	public override void SecondaryAttack()
	{
		CancelReload();
		_punch.Start( this, allowStrong: false );
	}

	protected override void Think()
	{
		_punch.Update( this, 60 );
		base.Think();
	}

	protected override void OnHolstered()
	{
		_punch = default;
		base.OnHolstered();
	}
}
