using Sandbox.UI;

namespace Source1.Dods;

public sealed class Stun : Component
{
	CameraComponent _camera;
	Texture _frame;
	Panel _image;
	float _duration;
	float _alpha;
	TimeSince _age;

	internal static void Burst( Scene scene, Vector3 origin, float radius, float strength )
	{
		foreach ( var player in scene.GetAllComponents<PlayerController>() )
		{
			var owner = player.Network.Owner;
			var target = Vector3.Lerp( player.WorldPosition, player.EyeTransform.Position, Game.Random.Float( 0.7f, 1 ) );
			var amount = strength * (1 - target.Distance( origin ) / radius);
			if ( amount <= 0 || owner is null || !scene.IsPointVisibleToConnection( owner, origin ) ) continue;

			using var recipient = Rpc.FilterInclude( owner );
			Apply( player, amount );
		}
	}

	[Rpc.Broadcast( NetFlags.HostOnly )]
	static void Apply( PlayerController player, float amount )
	{
		if ( !player.IsValid() || player.IsProxy || player.Scene.Camera is not { } camera ) return;
		float Kick( float min, float max ) => Game.Random.Float( min, max ) * (Game.Random.Int( 0, 1 ) == 0 ? -amount : amount );
		ViewPunch.SetAngle( player, new( Kick( 30, 50 ), Kick( 120, 150 ), Kick( 15, 30 ) ) );

		var stun = camera.Scene.GetAllComponents<Stun>().FirstOrDefault();

		if ( !stun.IsValid() )
		{
			var go = new GameObject( true, "DoD:S stun" ) { NetworkMode = NetworkMode.Never };
			go.Flags |= GameObjectFlags.NotSaved | GameObjectFlags.NotNetworked;
			var screen = go.AddComponent<ScreenPanel>();
			stun = go.AddComponent<Stun>();
			stun._image = screen.AddOverlay();
			stun._image.Style.BackgroundSizeX = stun._image.Style.BackgroundSizeY = Length.Percent( 100 );
		}
		stun._camera = camera;
		stun._age = 0;
		stun._duration = 4 * amount;
		stun._alpha = 1;
	}

	protected override void OnUpdate()
	{
		if ( _age >= _duration || !_camera.IsValid() )
		{
			GameObject.Destroy();
			return;
		}

		var size = _camera.ScreenRect.Size;
		if ( size.x < 1 || size.y < 1 ) return;
		var alpha = 1 - _age / _duration;
		if ( _alpha < 255 )
		{
			_frame ??= Texture.CreateRenderTarget( "DoD:S stun", ImageFormat.RGBA8888, size );
			_camera.RenderToTexture( _frame );
			_image.Style.BackgroundImage = _frame;
			_alpha = System.MathF.Min( _alpha + 45, 255 );
			alpha = _alpha / 255;
		}

		// Four blends of the same captured frame in one draw.
		_image.Style.Opacity = 1 - System.MathF.Pow( 1 - alpha, 4 );
	}

	protected override void OnDestroy() => _frame?.Dispose();
}
