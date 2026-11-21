namespace GoldSrc.HalfLife;

[Hide]
public abstract class ProjectileWeapon : StudioWeapon
{
	TimeUntil _nextLaunch, _nextSecondary;

	public override void PrimaryAttack() => Launch( AimRay );

	[Rpc.Host( NetFlags.OwnerOnly )]
	void Launch( Ray ray ) => TryLaunch( ray, Release );

	protected void TryLaunch( Ray ray, System.Func<Ray, bool> release, bool secondary = false, float extraDelay = 0 )
	{
		if ( !IsActive || !Owner.IsValid() || (secondary ? _nextSecondary : _nextLaunch) > 0 || !ray.Position.IsFinite || !ray.Forward.IsFinite ||
			ray.Position.Distance( Owner.EyePosition ) > 96 || !HasPrimaryAmmo() ) return;
		if ( !release( ray ) ) return;
		TakePrimaryAmmo();
		_nextLaunch = (secondary ? SecondaryDelay : PrimaryDelay) + extraDelay;
		if ( secondary ) _nextSecondary = _nextLaunch;
		ShowLaunch();
	}

	internal abstract bool Release( Ray ray );

	[Rpc.Broadcast( NetFlags.HostOnly )]
	void ShowLaunch()
	{
		ViewModel?.GetComponent<StudioWeaponView>()?.Play( ShotSequence );
		PlayLaunchSound();
		PostWeaponEvent( WeaponEvents.Attack );
	}

	internal virtual void PlayLaunchSound()
	{
		if ( ShotSound is { } sound ) PlaySample( sound );
	}
}
