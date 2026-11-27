namespace Source1.Dods;

public sealed class Melee : MeleeWeapon
{
	Swing _swing;

	public override void PrimaryAttack() => _swing.Start( this, allowStrong: true );

	protected override void Think()
	{
		_swing.Update( this, Damage );
		base.Think();
	}

	protected override void OnHolstered()
	{
		_swing = default;
		base.OnHolstered();
	}
}
