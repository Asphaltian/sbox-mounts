using System;

namespace Source1.Css;

static class Effects
{
	public static ShellEjection Brass( string options )
	{
		var parts = options?.Split( ' ', StringSplitOptions.RemoveEmptyEntries ) ?? [];
		const string prefix = "EjectBrass_";
		if ( parts.Length < 2 || parts[0].Length <= prefix.Length || !parts[0].StartsWith( prefix, StringComparison.OrdinalIgnoreCase ) ) return null;
		var speed = parts.Length > 2 && int.TryParse( parts[2], out var value ) ? value : 0;
		return new BrassEjection( $"models/shells/shell_{parts[0][prefix.Length..].ToLowerInvariant()}.mdl", parts[1], speed );
	}

	public static bool MuzzleFlash( string type, GameObject attachment, bool firstPerson, float scale, string mount )
	{
		if ( type == Rules.FlashNone ) return true;
		if ( !Rules.IsFlash( type ) ) return false;
		var burst = WeaponEffects.CreateMuzzleBurst( attachment );
		if ( firstPerson ) PlayerFlash( type, burst, mount, scale );
		else WeaponEffects.WorldFlash( type, burst, mount, scale * 0.75f );
		burst.GameObject.Enabled = true;
		MuzzleLight( attachment );
		return true;
	}

	static void PlayerFlash( string type, SpriteBurst burst, string mount, float scale )
	{
		if ( type == Rules.FlashX )
		{
			var size = 6 * scale * Game.Random.Float( 0.9f, 1.1f );
			var x = burst.Add( "effects/muzzleflashx", mount, 0, size, Color.White * (130 / 255f), 0.08f, Game.Random.Float( -0.25f, 0.25f ).RadianToDegree() );
			burst.FadeTo( x, size * 0.8f, Color.White * (80 / 255f) );
			return;
		}
		for ( var i = 0; i < 3; i++ )
		{
			var size = (3 + 3 * i) * scale;
			var sprite = burst.Add( "effects/muzzleflash4", mount, 0, size, Color.White * (80 / 255f), 0.08f, ((float)Game.Random.Int( 0, 3 )).RadianToDegree() );
			burst.FadeTo( sprite, size * 0.8f, Color.White * (30 / 255f) );
		}
	}

	/// <summary>Counter-Strike's muzzle dlight: 255 192 64 at exponent 5, radius 70, for 0.05 seconds.</summary>
	static void MuzzleLight( GameObject attachment )
	{
		var go = new GameObject( attachment, true, "Muzzle flash light" );
		go.NetworkMode = NetworkMode.Never;
		go.LocalPosition = Vector3.Zero;
		var light = go.AddComponent<PointLight>();
		light.Radius = 70;
		light.LightColor = new Color( 1, 192 / 255f, 64 / 255f ) * 4;
		light.Shadows = false;
		go.AddComponent<TemporaryEffect>().DestroyAfterSeconds = 0.05f;
	}
}

internal sealed record BrassEjection( string Model, string Attachment, float Speed ) : ShellEjection( Model, Attachment )
{
	public override void Eject( GameObject port, Rotation eye, Vector3 inherited, string mount, ViewModel view = null )
	{
		var axes = port.WorldRotation;
		Vector3 Axis( Vector3 direction ) => view?.DrawnDirection( direction ) ?? direction;
		Launch( view?.DrawnPosition( port ) ?? port.WorldPosition, Axis( axes.Forward ), Axis( axes.Right ), Axis( axes.Up ),
			Speed, eye, inherited, mount, view is not null );
	}

	public override void EjectFromMuzzle( GameObject muzzle, Rotation eye, Vector3 inherited, string mount )
	{
		var toss = Rotation.LookAt( eye.Right * 100 + eye.Up * 20 );
		Launch( muzzle.WorldPosition, toss.Forward, toss.Right, toss.Up, 120, eye, inherited, mount, false );
	}

	void Launch( Vector3 position, Vector3 forward, Vector3 right, Vector3 up, float speed, Rotation eye, Vector3 inherited, string mount, bool firstPerson )
	{
		var velocity = forward * speed * Game.Random.Float( 1.2f, 2.8f ) + up * Game.Random.Float( -10, 10 ) + right * Game.Random.Float( -20, 20 ) + inherited;
		var spin = new Angles( Game.Random.Float( -256, 256 ), Game.Random.Float( -256, 256 ), 0 );
		var bounce = Model.Contains( "12gauge", StringComparison.OrdinalIgnoreCase ) ? "Bounce.ShotgunShell"
			: Model.Contains( "9mm", StringComparison.OrdinalIgnoreCase ) || Model.Contains( "_57", StringComparison.OrdinalIgnoreCase ) ? "Bounce.PistolShell"
			: "Bounce.RifleShell";
		WeaponEffects.SpawnShell( Model, position, eye, velocity, spin, mount, 10, bounce, casing: false, viewModelLayer: firstPerson );
	}
}
