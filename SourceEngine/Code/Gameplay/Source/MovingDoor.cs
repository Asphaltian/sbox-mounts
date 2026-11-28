namespace Source1;

[Hide]
public abstract class MovingDoor : MovingBrush
{
	[Property] public Angles ClosedAngles { get; set; }
	[Property] public bool OpenBothWays { get; set; }
	[Property] public float Speed { get; set; }
	[Property] public bool IsUsable { get; set; }
	[Property, Sync] public bool IsLocked { get; set; }
	[Property] public SoundEvent OpenFinishedSound { get; set; }
	[Property] public SoundEvent CloseFinishedSound { get; set; }
	[Property] public SoundEvent LockedSound { get; set; }
	[Property] public Doo OnOpen { get; set; }
	[Property] public Doo OnClose { get; set; }
	[Property] public Doo FullyOpen { get; set; }
	[Property] public Doo FullyClosed { get; set; }
	[Property] public Doo LockedUse { get; set; }
	[Property] public bool ForceClosed { get; set; }
	[Property] public Doo BlockedOpening { get; set; }
	[Property] public Doo BlockedClosing { get; set; }
	[Property] public Doo UnblockedOpening { get; set; }
	[Property] public Doo UnblockedClosing { get; set; }

	protected IPressable.Tooltip? GetUseHint( bool canUse, bool closes )
	{
		if ( !canUse ) return null;
		if ( IsLocked ) return new IPressable.Tooltip( "Locked", "lock", null );
		return new IPressable.Tooltip( closes ? "Close" : "Open", "touch_app", null );
	}

	bool _blocked;

	protected bool UpdateBlocked( GameObject blocker, bool closing, bool sendUnblocked = true )
	{
		var blocked = blocker.IsValid();
		if ( blocked == _blocked ) return false;
		_blocked = blocked;
		if ( blocked || sendUnblocked ) FireBlockOutput( blocker, closing );
		return true;
	}

	protected void FireBlockOutput( GameObject blocker, bool closing )
	{
		bool blocked = blocker.IsValid();
		var output = blocked ? (closing ? BlockedClosing : BlockedOpening) : (closing ? UnblockedClosing : UnblockedOpening);
		RunDoo( output, c => c.SetArgument( "user", blocked ? blocker : GameObject ) );
	}
}
