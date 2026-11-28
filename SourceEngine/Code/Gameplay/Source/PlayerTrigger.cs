namespace Source1;

[Hide]
public sealed class PlayerTrigger : Component, Component.ITriggerListener
{
	[Property] public Doo Enter { get; set; }
	[Property] public Doo Leave { get; set; }
	[Property] public Doo First { get; set; }
	[Property] public Doo Last { get; set; }
	[Property] public Doo Activate { get; set; }
	[Property] public float Wait { get; set; }

	readonly HashSet<GameObject> _players = [];
	TimeUntil _next;
	bool _spent;

	public void TurnOn() => SetListening( true );
	public void TurnOff() => SetListening( false );
	public void Toggle() => SetListening( !GetComponentsInChildren<Collider>( true ).Any( c => c.Enabled ) );

	void SetListening( bool enabled )
	{
		if ( enabled && _spent ) return;
		foreach ( var collider in GetComponentsInChildren<Collider>( true ) ) collider.Enabled = enabled;
		if ( enabled || _players.Count == 0 ) return;
		foreach ( var player in _players.ToArray() ) EndTouch( player );
	}

	void ITriggerListener.OnTriggerEnter( GameObject other )
	{
		if ( !Networking.IsHost || other.GetComponentInParent<PlayerController>() is not { } player || !_players.Add( player.GameObject ) ) return;
		Fire( Enter, player.GameObject );
		if ( _players.Count == 1 ) Fire( First, player.GameObject );
		if ( _players.Contains( player.GameObject ) ) TryActivate( player.GameObject );
	}

	void ITriggerListener.OnTriggerExit( GameObject other )
	{
		if ( !Networking.IsHost || other.GetComponentInParent<PlayerController>() is not { } player ) return;
		EndTouch( player.GameObject );
	}

	void EndTouch( GameObject player )
	{
		if ( !_players.Remove( player ) ) return;
		Fire( Leave, player );
		if ( _players.Count == 0 ) Fire( Last, player );
	}

	protected override void OnFixedUpdate()
	{
		if ( _next > 0 ) return;
		if ( _spent )
		{
			TurnOff();
			Enabled = false;
		}
		else if ( Activate is not null && _players.Count > 0 ) TryActivate( _players.First() );
	}

	void TryActivate( GameObject player )
	{
		if ( _spent || _next > 0 ) return;
		_next = Wait == 0 ? 0.2f : Wait;
		_spent = Wait < 0;
		if ( _spent ) _next = 0.1f;
		Fire( Activate, player );
	}

	void Fire( Doo output, GameObject player ) => RunDoo( output, c => c.SetArgument( "user", player ) );
}
