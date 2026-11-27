namespace Source1.Dods;

public sealed class Mg42 : BipodGun
{
	// Cooling is continuous even while the gun is holstered.
	[Sync] TimeUntil Cold { get; set; }
	[Sync] public bool Overheated { get; private set; }
	public int Heat => System.Math.Clamp( (int)System.MathF.Ceiling( (float)Cold / 0.16f ), 0, 99 );

	public override bool CanPrimaryAttack() => (!Overheated || Heat < 80) && base.CanPrimaryAttack();

	public override void PrimaryAttack()
	{
		var heat = Heat;
		if ( Overheated && heat >= 80 ) return;
		if ( heat >= 99 )
		{
			Overheated = true;
			if ( Sounds.Sound( "Weapon_Mg42.OverHeat", MountContent.OfWeapon( this ) ) is { } sound )
				Sounds.PlayWeaponSound( this, sound );
			return;
		}
		var clip = Clip1;
		base.PrimaryAttack();
		if ( Clip1 < clip )
		{
			Overheated = false;
			Cold = (heat + 1) * 0.16f;
		}
	}

	protected override void OnReloadFinished()
	{
		base.OnReloadFinished();
		Cold = 0;
		Overheated = false;
	}
}
