using System;

namespace Source1.Hl2;

sealed class PhysCannonEffects : IDisposable
{
	static readonly string[] WorldProngs = ["fork1m", "fork1t", "fork2m", "fork2t", "fork3m", "fork3t"];
	static readonly string[] ViewProngs = ["fork1b", "fork1m", "fork1t", "fork2b", "fork2m", "fork2t"];
	static readonly string[] ViewTips = ["fork1t", "fork2t"];
	static readonly string[] WorldTips = ["fork1t", "fork2t", "fork3t"];
	static readonly Curve ViewBeamWidth = BeamWidth( 0, 4 );
	static readonly Curve WorldBeamWidth = BeamWidth( 0.5f, 2 );
	static Curve BeamWidth( float start, float end ) => new( new Curve.Frame( 0, start ) { Mode = Curve.HandleMode.Linear }, new Curve.Frame( 1, end ) );
	// StartEffects tints the prong glows kRenderTransAdd 255, 128, 0.
	static readonly Color GlowColor = new( 1, 0.5f, 0 );
	SkinnedModelRenderer _renderer;
	ViewModel _view;
	GameObject _root;
	GameObject _coreAnchor;
	LineRenderer _puntBeam;
	Vector3 _impact;
	SpriteRenderer _core;
	SpriteRenderer _blast;
	readonly List<SpriteRenderer> _glows = new();
	readonly List<SpriteRenderer> _endcaps = new();
	readonly List<(GameObject Tip, LineRenderer Beam)> _beams = new();
	readonly float[] _beamNoise = new float[129];
	readonly Dictionary<SpriteRenderer, GameObject> _anchors = new();
	TimeSince _launch = 10;
	TimeUntil _changeAt;
	bool _closePending;
	bool _open;
	float _elementFrom;
	TimeSince _elementMoved = 10;
	SoundHandle _holdSound;
	Vector2 _coreFrom, _coreTarget;
	TimeSince _coreChanged;
	bool _motorHolding;
	TimeSince _motorChanged;
	float _releaseVolume;

	public void Launch( GravityWeapon weapon, bool fromHold, Vector3 impact )
	{
		_impact = impact;
		_launch = 0;
		_changeAt = fromHold ? 0.1f : 0.5f;
		_closePending = true;
		if ( !_coreAnchor.IsValid() ) return;
		var muzzle = Place( _coreAnchor );
		var direction = (impact - muzzle).Normal;
		var local = weapon.Owner is { IsProxy: false };
		WeaponEffects.GaussImpact( local ? impact : muzzle, local ? -direction : direction, MountContent.Of( _renderer.Model ) );
	}

	public void Update( GravityWeapon weapon, GameObject held, bool ready )
	{
		var model = weapon.WeaponModel;
		var renderer = model.IsValid() ? model.Renderer : null;
		if ( renderer != _renderer )
		{
			Dispose();
			_renderer = renderer;
		}
		if ( !renderer.IsValid() || !renderer.Model.IsValid() || !renderer.Enabled ) return;
		var mount = MountContent.Of( renderer.Model );
		if ( !_root.IsValid() ) Build( renderer, model as ViewModel, mount, weapon.Owner is { IsProxy: false } );
		var holding = held.IsValid();
		UpdateElements( weapon, holding, ready );
		UpdateMotor( weapon, holding, mount );
		UpdateCore( holding, weapon.IsHeld && weapon.Active );
		float launchAge = _launch;
		SetGlow( _blast, float.Lerp( 8, 64, Math.Min( 1, launchAge / 0.1f ) ), 255 * Math.Max( 0, 1 - launchAge / 0.2f ) );
		foreach ( var glow in _glows )
			SetGlow( glow, Game.Random.Float( 6.4f, 9.6f ), Game.Random.Float( 16, 24 ), GlowColor );
		foreach ( var endcap in _endcaps )
			SetGlow( endcap, Game.Random.Float( 3, 5 ), holding ? Game.Random.Float( 200, 255 ) : 0 );
		foreach ( var (tip, beam) in _beams )
			beam.Enabled = tip.IsValid() && holding;
		if ( _puntBeam.IsValid() ) _puntBeam.Enabled = launchAge < 0.1f;
		if ( !_view.IsValid() ) Follow();
	}

	/// <summary>
	/// Moves the effects onto the attachments. The weapon also calls this once the viewmodel is placed for the frame,
	/// which happens after updates, so they don't trail the view.
	/// </summary>
	public void Follow()
	{
		foreach ( var (sprite, anchor) in _anchors )
		{
			if ( !sprite.IsValid() || !anchor.IsValid() ) continue;
			sprite.WorldPosition = Place( anchor );
			if ( _view.IsValid() ) sprite.WorldScale = _view.WorldScale.y;
		}
		foreach ( var (tip, beam) in _beams )
		{
			if ( !tip.IsValid() || !beam.IsValid() || !beam.Enabled ) continue;
			DrawCarryBeam( beam, Place( tip ), Place( _coreAnchor ) );
		}
		if ( _puntBeam.IsValid() && _puntBeam.Enabled )
		{
			_puntBeam.VectorPoints[0] = Place( _coreAnchor );
			_puntBeam.VectorPoints[1] = _impact;
		}
	}

	/// <summary>
	/// OpenElements and CloseElements: the claws spline between closed and open over 0.2s
	/// through the viewmodel's "active" pose parameter.
	/// </summary>
	void UpdateElements( GravityWeapon weapon, bool holding, bool ready )
	{
		var open = _open;
		if ( holding || (ready && _launch >= 0.25f) )
		{
			_closePending = false;
			open = true;
		}
		else if ( _changeAt <= 0 )
		{
			if ( _closePending ) open = false;
			else _changeAt = 0.5f;
			_closePending = !_closePending;
		}

		if ( open != _open )
		{
			_elementFrom = Element();
			_elementMoved = 0;
			_open = open;
			_view?.OnClawsChanged( holding );
			if ( !weapon.IsProxy && weapon.IsHeld ) weapon.PlayClawSound( open );
		}
		_view?.SetPoseParameter( "active", Element() );
	}

	void UpdateCore( bool holding, bool equipped )
	{
		if ( !_core.IsValid() ) return;
		var firstPerson = _view.IsValid();
		var target = holding ? new Vector2( firstPerson ? 13 : 14, 255 )
			: equipped ? new Vector2( firstPerson ? 10 : 8, firstPerson ? 128 : 0 ) : Vector2.Zero;
		if ( target != _coreTarget )
		{
			_coreFrom = _core.Size.x > 0 ? new Vector2( _core.Size.x, _core.Color.a * 255 ) : Vector2.Zero;
			_coreTarget = target;
			_coreChanged = 0;
		}
		var width = float.Lerp( _coreFrom.x, target.x, Math.Min( 1, _coreChanged * 5 ) );
		var brightness = float.Lerp( _coreFrom.y, target.y, Math.Min( 1, _coreChanged / (holding ? 0.1f : 0.2f) ) );
		SetGlow( _core, width, brightness );
	}

	static void SetGlow( SpriteRenderer sprite, float width, float brightness, Color? tint = null )
	{
		if ( !sprite.IsValid() ) return;
		sprite.Size = width;
		sprite.Color = (tint ?? Color.White).WithAlpha( brightness / 255f );
		sprite.Enabled = brightness > 0;
	}

	void UpdateMotor( GravityWeapon weapon, bool holding, string mount )
	{
		if ( holding != _motorHolding )
		{
			_motorHolding = holding;
			_motorChanged = 0;
			if ( holding && _holdSound is null && Sounds.Sound( "Weapon_PhysCannon.HoldSound", mount ) is { } sound )
				_holdSound = weapon.GameObject.PlaySound( sound );
			else if ( !holding ) _releaseVolume = _holdSound?.Volume ?? 0;
		}
		if ( _holdSound is null ) return;
		var volume = holding ? 0.8f * Math.Min( 1, _motorChanged * 2 ) : _releaseVolume * Math.Max( 0, 1 - _motorChanged );
		_holdSound.Volume = volume;
		_holdSound.Pitch = float.Lerp( 0.5f, 1, volume / 0.8f );
		if ( !holding && volume == 0 )
		{
			_holdSound.Stop();
			_holdSound = null;
		}
	}

	void DrawCarryBeam( LineRenderer beam, Vector3 start, Vector3 end )
	{
		var delta = end - start;
		var length = delta.Length;
		var side = (beam.Scene.Camera?.WorldRotation.Forward ?? Vector3.Forward).Cross( delta ).Normal;
		var maxWidth = _view.IsValid() ? 4 : 2;
		var count = (int)(length * 0.25f) + 3;
		var overlapLimit = (int)(length / (maxWidth * 0.707f)) + 1;
		count = Math.Clamp( Math.Min( count, overlapLimit ), 2, 128 );
		var points = beam.VectorPoints;
		if ( points.Count > count ) points.RemoveRange( count, points.Count - count );
		while ( points.Count < count ) points.Add( default );

		// Source samples a midpoint-displacement waveform perpendicular to the beam and camera.
		for ( var half = 64; half > 0; half /= 2 )
		{
			for ( var i = half; i < 128; i += half * 2 )
				_beamNoise[i] = (_beamNoise[i - half] + _beamNoise[i + half]) * 0.5f + Random.Shared.Float( -1, 1 ) * (half / 64f);
		}
		// The first and last noise samples stay zero, pinning both ends to their attachments.
		var last = count - 1;
		for ( var i = 0; i < count; i++ )
			points[i] = start + delta * (i / (float)last) + side * (_beamNoise[i * 128 / last] * length * 0.16f);

		beam.Texturing = beam.Texturing with { Scale = length * 0.01f, Scroll = 150 };
	}

	float Element()
	{
		var t = Math.Clamp( _elementMoved / (_open ? 0.2f : 0.5f), 0, 1 );
		var spline = t * t * (3 - 2 * t);
		return float.Lerp( _elementFrom, _open ? 1 : 0, spline );
	}

	Vector3 Place( GameObject anchor ) => _view.IsValid() ? _view.DrawnPosition( anchor ) : anchor.WorldPosition;

	SpriteRenderer AddSprite( GameObject anchor, string name, string mount )
	{
		var sprite = MountedEffects.AddSprite( _root, name, mount );
		if ( !sprite.IsValid() ) return null;
		sprite.RenderOptions.Game = !_view.IsValid();
		sprite.RenderOptions.Overlay = _view.IsValid();
		_anchors[sprite] = anchor;
		return sprite;
	}

	void Build( SkinnedModelRenderer renderer, ViewModel view, string mount, bool localPlayer )
	{
		_view = view;
		var viewModel = view.IsValid();
		_root = new GameObject( true, "Physcannon effects" );
		_root.NetworkMode = NetworkMode.Never;
		var coreName = renderer.Model.Attachments.All.FirstOrDefault()?.Name;
		var core = coreName is null ? renderer.GameObject : renderer.GetAttachmentObject( coreName ) ?? renderer.GameObject;
		_coreAnchor = core;
		var content = Sandbox.Mounting.Directory.Get( mount );
		string MaterialVariant( string name, string suffix = "_gmod" )
			=> content?.GetByPath( $"mount://{mount}/materials/{name}{suffix}.vmt.vmat" ) is null ? name : name + suffix;

		_core = AddSprite( core, MaterialVariant( "sprites/orangecore1" ), mount );
		_blast = AddSprite( core, MaterialVariant( "sprites/orangecore2" ), mount );
		var worldBeam = MaterialVariant( "sprites/orangelight1" );
		if ( localPlayer && MountedEffects.AddBeam( _root, worldBeam, mount ) is { } punt )
		{
			_puntBeam = punt;
			punt.Width = BeamWidth( 12, 4 );
			punt.Texturing = punt.Texturing with { WorldSpace = true, UnitsPerTexture = 100 };
			punt.Color = Color.White;
			punt.Enabled = false;
		}
		var glowMaterial = MaterialVariant( "sprites/glow04_noz" );
		foreach ( var name in viewModel ? ViewProngs : WorldProngs )
		{
			var prong = renderer.GetAttachmentObject( name );
			if ( prong.IsValid() && AddSprite( prong, glowMaterial, mount ) is { } glow ) _glows.Add( glow );
		}

		var beamSprite = viewModel ? MaterialVariant( "sprites/orangelight1", "_noz" ) : worldBeam;
		var endcapMaterial = MaterialVariant( "sprites/orangeflare1" );
		foreach ( var name in viewModel ? ViewTips : WorldTips )
		{
			var tip = renderer.GetAttachmentObject( name );
			if ( !tip.IsValid() ) continue;
			if ( AddSprite( tip, endcapMaterial, mount ) is { } endcap ) _endcaps.Add( endcap );
			if ( MountedEffects.AddBeam( _root, beamSprite, mount ) is { } beam )
			{
				beam.Width = viewModel ? ViewBeamWidth : WorldBeamWidth;
				beam.Color = (Color.White * (128f / 255)).WithAlpha( 1 );
				_beams.Add( (tip, beam) );
			}
		}
	}

	public void Dispose()
	{
		_holdSound?.Stop( 0.2f );
		_holdSound = null;
		_motorHolding = false;
		_coreFrom = _coreTarget = Vector2.Zero;
		_root?.Destroy();
		_root = null;
		_puntBeam = null;
		_view = null;
		_glows.Clear();
		_endcaps.Clear();
		_beams.Clear();
		_anchors.Clear();
	}
}
