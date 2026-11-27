namespace Source1.Dods;

public sealed class ScopedRifle : BoltRifle
{
	[Property] public string ScopeSprite { get; set; }
	[Property] public bool DrawScopeLines { get; set; }

	protected override float SightFov => 20;
	TimeUntil? _reaim;
	Texture[] _quadrants;

	protected override void OnBoltCycled()
	{
		if ( Clip1 > 0 ) _reaim = 1;
	}

	protected override void Think()
	{
		base.Think();
		if ( _reaim is null ) return;
		if ( IsReloading || (Input.Down( "run" ) && Owner.IsValid() && Owner.Velocity.WithZ( 0 ).Length > 20) )
			_reaim = null;
		else if ( _reaim <= 0 )
			SecondaryAttack();
	}

	public override void SecondaryAttack()
	{
		_reaim = null;
		base.SecondaryAttack();
	}

	protected override void OnHolstered()
	{
		_reaim = null;
		base.OnHolstered();
	}

	bool ShowScope => Sighted && Owner.IsValid() && !Owner.ThirdPerson;

	protected override void OnUpdate()
	{
		base.OnUpdate();
		if ( ViewModel.IsValid() ) ViewModel.Tags.Set( "sniper_scoped", ShowScope );
	}

	public override void DrawCrosshair( Painter painter, Vector2 center ) { }

	public override void DrawHud( Painter painter, Vector2 crosshair )
	{
		base.DrawHud( painter, crosshair );
		if ( !ShowScope || !Scene.Camera.IsValid() ) return;
		_quadrants ??= new[] { "ul", "ur", "ll", "lr" }.Select( corner =>
			MountedEffects.SpriteTexture( $"sprites/scopes/{ScopeSprite}_{corner}", MountContent.OfWeapon( this ) ) ).ToArray();

		var size = Scene.Camera.ScreenRect.Size;
		var half = new Vector2( size.y * 2 / 3, size.y / 2 );
		var left = size.x / 2 - half.x;
		using var scope = painter.Scope();
		painter.Fill = Color.Black;
		painter.Stroke = Stroke.None;
		if ( left > 0 )
		{
			painter.Rect( new Rect( 0, 0, left, size.y ) );
			painter.Rect( new Rect( size.x - left, 0, left, size.y ) );
		}
		for ( var i = 0; i < _quadrants.Length; i++ )
		{
			if ( !_quadrants[i].IsValid() ) continue;
			var position = new Vector2( left + (i % 2) * half.x, (i / 2) * half.y );
			painter.Texture( _quadrants[i], new Rect( position, half ), Color.Black );
		}
		if ( DrawScopeLines )
		{
			painter.Stroke = Stroke.Solid( Color.Black, 1 );
			painter.Line( new( 0, half.y ), new( size.x, half.y ) );
			painter.Line( new( size.x / 2, 0 ), new( size.x / 2, size.y ) );
		}
	}
}
