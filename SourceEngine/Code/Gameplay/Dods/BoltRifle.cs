namespace Source1.Dods;

public class BoltRifle : Rifle
{
	TimeUntil? _lowerSightsAt;

	public override void PrimaryAttack()
	{
		var rounds = Clip1;
		base.PrimaryAttack();
		if ( rounds == Clip1 ) return;
		if ( Aiming ) _lowerSightsAt = 0.5f;

		if ( Clip1 == 0 && Setup.ViewModel is { } model )
		{
			var last = ModelSequences.Of( model ).FindActivity( "ACT_VM_PRIMARYATTACK_EMPTY" );
			var duration = WeaponClips.Duration( model, last );
			if ( duration > 0 ) SetNextPrimaryFire( duration );
		}
	}

	protected override void Think()
	{
		if ( _lowerSightsAt is { } delay && delay <= 0 )
		{
			_lowerSightsAt = null;
			if ( Aiming )
			{
				Aiming = false;
				SetNextSecondaryFire( 0.5f );
				OnBoltCycled();
			}
		}
		base.Think();
	}

	protected virtual void OnBoltCycled() { }

	protected override void OnHolstered()
	{
		_lowerSightsAt = null;
		base.OnHolstered();
	}
}
