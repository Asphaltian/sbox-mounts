namespace Source1.GarrysMod;

public interface IPhysgunGrab : ISceneEvent<IPhysgunGrab>
{
	bool CanGrab( PhysicsGun weapon ) => true;
	void OnGrabbed( PhysicsGun weapon ) { }
	void OnReleased( PhysicsGun weapon ) { }
}
