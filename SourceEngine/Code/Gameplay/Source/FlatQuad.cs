using System;

namespace Source1;

/// <summary>FX_AddQuad with FXQUAD_BIAS_SCALE and FXQUAD_BIAS_ALPHA: a full-width quad facing the normal, alpha 1 to 0.</summary>
[Title( "Flat Quad" )]
[Category( "Source 1" )]
[Alias( "Source1.Hl2.FlatQuad", "Source1.Hl2FlatQuad", "Hl2FlatQuad" )]
public sealed class FlatQuad : Component
{
	float _scalePower, _alphaPower, _rollSpeed;
	SpriteRenderer _sprite;
	float _start;
	float _end;
	float _life;
	TimeSince _age;

	public static void Create( string material, Vector3 position, Vector3 normal, float startWidth, float endWidth, float life, string mount, bool linear = false )
		=> Spawn( material, position, normal, startWidth, endWidth, life, mount, linear ? 0.5f : 0.75f, linear ? 0.5f : 0.4f );

	internal static void WaterRipple( Vector3 position, Vector3 normal, string mount )
		=> Spawn( "effects/splashwake1", position + normal * 0.5f, normal, 16, 128, 1.5f, mount, 0.7f, 0.25f,
			rollSpeed: Game.Random.Float( -16, 16 ), additive: false );

	internal static void ExplosionRipple( Vector3 surface, string mount )
		=> Spawn( "effects/splashwake1", surface + Vector3.Up * 2, Vector3.Up, 64, 256, 2, mount, 0.85f, 0.25f,
			rollSpeed: Game.Random.Float( -4, 4 ), additive: false );

	static void Spawn( string material, Vector3 position, Vector3 normal, float startWidth, float endWidth, float life, string mount,
		float scaleBias, float alphaBias, float rollSpeed = 0, bool additive = true )
	{
		var go = new GameObject( false, "Flat quad" );
		go.NetworkMode = NetworkMode.Never;
		go.WorldPosition = position;
		go.WorldRotation = Rotation.LookAt( normal ) * Rotation.FromRoll( Game.Random.Int( 0, 360 ) );
		var sprite = MountedEffects.AddSprite( go, material, mount );
		if ( !sprite.IsValid() )
		{
			go.Destroy();
			return;
		}
		sprite.Billboard = SpriteRenderer.BillboardMode.None;
		sprite.Additive = additive;
		var quad = go.AddComponent<FlatQuad>();
		quad._sprite = sprite;
		quad._start = startWidth;
		quad._end = endWidth;
		quad._scalePower = -MathF.Log2( scaleBias );
		quad._alphaPower = -MathF.Log2( alphaBias );
		quad._rollSpeed = rollSpeed;
		quad._life = life;
		quad.Draw( 0 );
		go.Enabled = true;
	}

	protected override void OnStart() => _age = 0;

	protected override void OnUpdate()
	{
		var t = _age / _life;
		if ( t >= 1 )
		{
			GameObject.Destroy();
			return;
		}
		if ( _rollSpeed != 0 ) LocalRotation *= Rotation.FromRoll( _rollSpeed * Time.Delta );
		Draw( t );
	}

	void Draw( float t )
	{
		_sprite.Size = float.Lerp( _start, _end, MathF.Pow( t, _scalePower ) );
		_sprite.Color = Color.White.WithAlpha( 1 - (MathF.Pow( t, _alphaPower )) );
	}
}
