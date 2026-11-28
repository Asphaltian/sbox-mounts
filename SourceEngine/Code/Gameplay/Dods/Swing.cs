namespace Source1.Dods;

/// <summary>Check reach at swing time and again when the animation makes contact.</summary>
internal struct Swing
{
	TimeUntil? _impact;
	bool _strong;

	public void Start( Weapon weapon, bool allowStrong )
	{
		var trace = Trace( weapon );
		var victim = trace.GameObject?.GetComponentInParent<PlayerController>();
		_strong = allowStrong && victim.IsValid() && victim.EyeAngles.WithPitch( 0 ).Forward.Dot( weapon.AimRay.Forward ) > 0.3f;
		weapon.SetNextFire( _strong ? 0.9f : 0.4f );
		if ( trace.Hit ) _impact = _strong ? 0.4f : 0.2f;
		weapon.MeleeSwingEffects( trace.StartPosition, trace.EndPosition, alternate: !allowStrong || _strong, playSound: !_strong );
	}

	public void Update( Weapon weapon, float damage )
	{
		if ( _impact is not { } delay || delay > 0 ) return;
		_impact = null;
		var trace = Trace( weapon );
		if ( !trace.Hit ) return;
		var player = trace.GameObject.GetComponentInParent<PlayerController>().IsValid();
		var role = player ? (_strong ? "special1" : "melee_hit") : "melee_hit_world";
		weapon.MeleeHit( trace, _strong ? 300 : damage, 2400, role );
	}

	static SceneTraceResult Trace( Weapon weapon )
	{
		var aim = weapon.AimRay;
		var trace = weapon.Scene.Trace.Ray( aim, 48 ).IgnoreGameObjectHierarchy( weapon.AimIgnoreRoot );
		var hit = trace.UseHitboxes().Run();
		if ( hit.Hit ) return hit;
		var reach = trace.FromTo( aim.Position, aim.Project( 88 ) ).UseHitboxes().Run();
		if ( reach.GameObject?.GetComponentInParent<PlayerController>().IsValid() == true ) return reach;
		return trace.Size( new BBox( new Vector3( -16, -16, -18 ), new Vector3( 16, 16, 18 ) ) ).Run();
	}
}
