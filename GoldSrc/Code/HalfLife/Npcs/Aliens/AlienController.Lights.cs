namespace GoldSrc.HalfLife;

public sealed partial class AlienController
{
	[Sync( SyncFlags.FromHost )] Vector2 Charge { get; set; }
	[Sync( SyncFlags.FromHost )] TimeUntil ChargeTime { get; set; }
	readonly SpriteRenderer[] _hands = new SpriteRenderer[2];

	readonly PointLight[] _lights = new PointLight[2];
	PointLight _headLight;
	TimeSince _headAge;
	bool _opening;

	[Rpc.Broadcast( NetFlags.HostOnly )]
	void HeadLight( bool opening )
	{
		if ( !_headLight.IsValid() )
		{
			var parent = Renderer.GetAttachmentObject( "attachment1" );
			if ( !parent.IsValid() ) return;
			var go = new GameObject( parent, false, "Controller head light" );
			go.NetworkMode = NetworkMode.Never;
			_headLight = go.AddComponent<PointLight>();
			_headLight.LightColor = new Color32( 255, 192, 64 );
			_headLight.Shadows = false;
			go.Enabled = true;
		}
		_headAge = 0;
		_opening = opening;
		_headLight.Radius = opening ? 1 : 32;
	}

	void UpdateLights()
	{
		if ( _headLight.IsValid() )
		{
			if ( _headAge >= (_opening ? 2 : 1) ) _headLight.GameObject.Destroy();
			else _headLight.Radius = _opening ? 1 + 32 * _headAge : 32 * (1 - _headAge);
		}
		for ( int i = 0; i < _hands.Length; i++ )
		{
			if ( !_hands[i].IsValid() )
			{
				var hand = Renderer.GetAttachmentObject( $"attachment{i + 3}" );
				if ( !hand.IsValid() || Sandbox.Mounting.Directory.Get( "hl1" ) is not HalfLifeMount mount || mount.GetIndexedSprite( "xspark4" ) is not { } sprite ) continue;
				var go = new GameObject( hand, false, "Controller hand glow" );
				go.NetworkMode = NetworkMode.Never;
				_hands[i] = go.AddComponent<SpriteRenderer>();
				_hands[i].Sprite = sprite;
				_lights[i] = go.AddComponent<PointLight>();
				_lights[i].LightColor = new Color32( 255, 192, 64 );
				_lights[i].Shadows = false;
				_hands[i].Size = 32;
				_hands[i].Additive = true;
				_hands[i].Color = Color.Transparent;
				go.Enabled = true;
			}
			var target = (i == 0 ? Charge.x : Charge.y) / 255;
			var alpha = float.Lerp( _hands[i].Color.a, target, (Time.Delta / System.MathF.Max( Time.Delta, ChargeTime )).Clamp( 0, 1 ) );
			_hands[i].Color = Color.White.WithAlpha( alpha );
			_lights[i].Radius = alpha * (255f / 8);
		}
	}
}
