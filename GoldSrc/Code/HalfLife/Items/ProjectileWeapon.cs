namespace GoldSrc.HalfLife;

[Hide]
public abstract class ProjectileWeapon : StudioWeapon
{
	TimeUntil _nextLaunch;

	public override void PrimaryAttack() => Launch( AimRay );

	[Rpc.Host( NetFlags.OwnerOnly )]
	void Launch( Ray ray ) => TryLaunch( ray, Release );

	protected void TryLaunch( Ray ray, System.Func<Ray, bool> release )
	{
		if ( !IsActive || !Owner.IsValid() || _nextLaunch > 0 || !ray.Position.IsFinite || !ray.Forward.IsFinite ||
			ray.Position.Distance( Owner.EyePosition ) > 96 || !HasPrimaryAmmo() ) return;
		if ( !release( ray ) ) return;
		TakePrimaryAmmo();
		_nextLaunch = PrimaryDelay;
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
