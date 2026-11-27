using System;

namespace Source1.Dods;

public sealed class ScopedRifle : BoltRifle
{
	[Property] public string ScopeSprite { get; set; }
	[Property] public bool DrawScopeLines { get; set; }

	protected override float SightFov => 20;
	internal override float ViewModelFovOffset => 0;
	TimeUntil? _reaim;
	Texture[] _quadrants;

	protected override Angles ApplyLook( Angles input )
	{
		input = base.ApplyLook( input );
		if ( !Aiming || ZoomFraction < 1 ) return input;

		var scale = Owner.IsDucking ? 0.2f : 0.5f;
		if ( Owner.Body.Velocity.LengthSquared > 100 ) scale += 0.4f;
		var time = Time.Now;
		return input + new Angles( 2 * MathF.Cos( 2 * time ), MathF.Cos( time ), 0 ) * (Time.Delta * scale);
	}

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

	bool ShowScope => ZoomFraction > 0 && Owner.IsValid() && !Owner.ThirdPerson;

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
		var center = new Vector2( (int)size.x / 2, (int)size.y / 2 );
		var halfWidth = (int)(size.y / 3) * 2;
		var left = center.x - halfWidth;
		var right = center.x + halfWidth;
		using var scope = painter.Scope();
		painter.Fill = Color.Black;
		painter.Stroke = Stroke.None;
		if ( left > 0 )
		{
			painter.Rect( new Rect( 0, 0, left + 1, size.y ) );
			painter.Rect( new Rect( right - 1, 0, size.x - right + 1, size.y ) );
		}
		for ( var i = 0; i < _quadrants.Length; i++ )
		{
			if ( !_quadrants[i].IsValid() ) continue;
			var x = i % 2 == 0 ? left : center.x;
			var y = i < 2 ? 0 : center.y;
			var height = i < 2 ? center.y : size.y - center.y;
			painter.Texture( _quadrants[i], new Rect( x, y, halfWidth, height ), Color.Black );
		}
		if ( DrawScopeLines )
		{
			painter.Rect( new Rect( 0, center.y, size.x, 1 ) );
			painter.Rect( new Rect( center.x, 0, 1, size.y ) );
		}
	}
}
