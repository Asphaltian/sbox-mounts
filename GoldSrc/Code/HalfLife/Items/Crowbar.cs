namespace GoldSrc.HalfLife;

[Title( "Crowbar" ), Category( "Half-Life" )]
[WeaponPickup( "crowbar", null, 0 )]
public sealed class Crowbar : StudioWeapon
{
	static readonly BBox HeadHull = new( new Vector3( -16, -16, -18 ), new Vector3( 16, 16, 18 ) );
	TimeUntil? _retry, _decalDelay;
	TimeUntil _fullDamageAt;
	Vector3 _decalPoint, _decalNormal;
	int _swing, _hit;

	internal override void Configure()
	{
		HoldType = WeaponHolds.Melee;
		PrimaryDelay = 0.5f;
		UsesClips = false;
		UsesAmmo = false;
		AttackDamageTags = ["club"];
		Ballistics = new() { Damage = 10, Range = 32, Radius = 0 };
	}

	public override void PrimaryAttack() => Swing( true );
	public override Ray AimRay => Holder is null ? base.AimRay : Holder.CalculateAimRay( this );

	void Swing( bool first )
	{
		var hit = TraceSwing( AimRay );
		if ( hit.Hit )
		{
			var damage = Networking.MaxPlayers > 1 ? 25 : Ballistics.Damage * (_fullDamageAt <= 0 ? 1 : 0.5f);
			ShootBullet( hit, damage, 0, hitboxTags: false );
			SetNextPrimaryFire( 0.25f );
			_fullDamageAt = 1.25f;
		}
		else if ( first )
		{
			_retry = 0.1f;
			_fullDamageAt = 1 + PrimaryDelay;
		}
		else return;

		ShootEffects( new ShotEffect( hit.EndPosition, hit.Hit, hit.Normal, hit.GameObject, hit.Surface, NoEvents: !first ) );
	}

	SceneTraceResult TraceSwing( Ray aim )
	{
		var trace = BulletTrace( aim, Ballistics.Range, 0 );
		var line = trace.Run();
		if ( line.Hit ) return line;
		var hull = trace.Size( HeadHull ).Run();
		if ( !hull.Hit || hull.GameObject?.GetComponentInParent<IDamageable>() is not null ) return hull;

		// FindHullIntersection resolves a hull hit to a point on the actual surface.
		var end = aim.Position + (hull.EndPosition - aim.Position) * 2;
		line = trace.FromTo( aim.Position, end ).Run();
		if ( line.Hit ) return line;
		var nearest = float.MaxValue;
		foreach ( var corner in HeadHull.Corners )
		{
			line = trace.FromTo( aim.Position, end + corner ).Run();
			if ( !line.Hit || line.Distance >= nearest ) continue;
			nearest = line.Distance;
			hull = line;
		}
		return hull;
	}

	protected override void Think()
	{
		base.Think();
		if ( _retry is not { } due || due > 0 ) return;
		_retry = null;
		Swing( false );
	}

	protected override void OnShootEffects( ShotEffect shot )
	{
		PostWeaponEvent( WeaponEvents.Attack );
		if ( Application.IsDedicatedServer ) return;
		var view = ViewModel?.GetComponent<StudioWeaponView>();
		if ( !shot.NoEvents )
		{
			PlaySample( "weapons/cbar_miss1.wav" );
			view?.Play( (_swing++ % 3) switch { 0 => "attack1miss", 1 => "attack2", _ => "attack3" } );
		}
		if ( !shot.Hit ) return;
		view?.Play( (_hit++ & 1) == 0 ? "attack2hit" : "attack3hit" );
		if ( HitFlesh( shot ) )
		{
			PlaySample( $"weapons/cbar_hitbod{Game.Random.Int( 1, 3 )}.wav" );
			return;
		}
		PlaySample( $"weapons/cbar_hit{Game.Random.Int( 1, 2 )}.wav", pitch: Game.Random.Int( 98, 101 ) / 100f );
		_decalPoint = shot.HitPosition;
		_decalNormal = shot.Normal;
		_decalDelay = 0.2f;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		if ( _decalDelay is not { } due || due > 0 ) return;
		_decalDelay = null;
		ImpactDecal.Place( _decalPoint, _decalNormal, $"shot{Game.Random.Int( 1, 5 )}", 10 );
	}

	protected override void OnHolstered()
	{
		_retry = _decalDelay = null;
		base.OnHolstered();
	}
}
