namespace Source1.Css;

public sealed class GrenadeWeapon : ProjectileWeapon
{
	internal override WeaponClips ConfigureThrow( Model model, WeaponClips clips ) => clips with
	{
		ThrowWindup = ModelSequences.Of( model ).FindActivity( "ACT_VM_PULLPIN", clips.ThrowWindup ),
		WindupDuration = 0,
		PrimaryThrow = clips.PrimaryThrow with { Delay = 0.1f }
	};

	protected override void OnRedraw()
	{
		if ( !Inventory.IsValid() || !IsActive ) return;
		if ( HasPrimaryAmmo() ) Inventory.SwitchToBest();
		else Inventory.Remove( this );
	}

	protected override (Vector3 Origin, Vector3 Velocity) LaunchTransform( Vector3 eye, Vector3 forward, Vector3 inherited, ThrowStyle style )
	{
		var (direction, speed) = Grenade.ThrowDirection( forward );
		return (CheckThrowPosition( eye, eye + direction * 16, Grenade.HullSize ), inherited + direction * speed);
	}
}
