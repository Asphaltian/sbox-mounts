namespace GoldSrc.HalfLife;

[Hide]
public abstract class BulletWeapon : StudioWeapon
{
	internal abstract Vector2 PelletSpread { get; }
	internal static bool Deathmatch => Networking.MaxPlayers > 1;

	// FireBulletsPlayer applies its spread at trace time, independently for each pellet.
	public sealed override Vector2 CurrentSpread => Vector2.Zero;

	protected sealed override SceneTrace BulletTrace( Ray ray, float distance, float radius )
	{
		var spread = PelletSpread;
		var basis = Rotation.LookAt( ray.Forward );
		var direction = ray.Forward + basis.Right * (SampleOffset() * spread.x) + basis.Up * (SampleOffset() * spread.y);
		return base.BulletTrace( new Ray( ray.Position, direction.Normal ), distance * direction.Length, radius );
	}

	static float SampleOffset() => Game.Random.Float( -0.5f, 0.5f ) + Game.Random.Float( -0.5f, 0.5f );
}
