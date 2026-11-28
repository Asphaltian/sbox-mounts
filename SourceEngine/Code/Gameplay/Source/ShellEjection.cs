namespace Source1;

internal record ShellEjection( string Model, string Attachment )
{
	public virtual void Eject( GameObject port, Rotation eye, Vector3 inherited, string mount, ViewModel view = null )
	{
		var forward = port.WorldRotation.Forward;
		WeaponEffects.EjectBrass( Model, view?.DrawnPosition( port ) ?? port.WorldPosition, view?.DrawnDirection( forward ) ?? forward, eye, mount );
	}

	public virtual void EjectFromMuzzle( GameObject muzzle, Rotation eye, Vector3 inherited, string mount ) { }
}
