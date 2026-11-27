using System;

namespace Source1.Dods;

[WeaponProjectile( Projectile.Kinds.DodSmoke )]
public sealed class SmokeGrenade : ThrownGrenade, ISmokeCloud
{
	[Sync( SyncFlags.FromHost )] public float SmokeStart { get; private set; }
	bool _emitting;
	protected override float Lifetime => 0.5f;

	protected override string BounceSound => "SmokeGrenade.Bounce";

	protected override void Expire()
	{
		if ( SmokeStart > 0 )
		{
			if ( Time.Now - SmokeStart > 24 ) GameObject.Destroy();
			return;
		}
		var body = GetComponent<Rigidbody>();
		if ( body.Velocity.LengthSquared >= 15 * 15 ) return;
		body.MotionEnabled = false;
		SmokeStart = Time.Now;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		if ( Application.IsDedicatedServer || SmokeStart <= 0 ) return;
		if ( !_emitting && Renderer.IsValid() && Renderer.Model.IsValid() && !Renderer.Model.IsError )
		{
			_emitting = true;
			SpawnEffect( "smokegrenade", GameObject );
			SpawnEffect( "smokegrenade_jet", Renderer.GetAttachmentObject( "jet" ) ?? GameObject );
			if ( Sounds.Sound( "BaseSmokeEffect.Sound", MountName ) is { } sound ) GameObject.PlaySound( sound );
			SmokeFog.Ensure( Scene );
		}
		if ( Renderer.IsValid() ) Renderer.Tint = Color.White.WithAlpha( (1 - (Time.Now - SmokeStart - 10) / 4).Clamp( 0, 1 ) );
	}

	void SpawnEffect( string name, GameObject parent )
	{
		if ( MountedEffects.Prefab( name, MountName ) is not { } prefab ) return;
		var effect = prefab.Clone( new CloneConfig { Parent = parent, Transform = global::Transform.Zero, StartEnabled = false } );
		effect.NetworkMode = NetworkMode.Never;
		effect.Enabled = true;
	}

	public float FogAlpha( Vector3 eye )
	{
		if ( SmokeStart <= 0 ) return 0;
		var t = Time.Now - SmokeStart;
		var radius = 330 * MathF.Sin( (t / 2).Clamp( 0, 1 ) * MathF.PI / 2 );
		if ( radius <= 0 ) return 0;
		var fade = MathF.Min( t / 3, (12 - t) / 3 ).Clamp( 0, 1 );
		var distance = eye.Distance( WorldPosition + Vector3.Up * 32 );
		return fade * fade * (3 - 2 * fade) * ((radius - distance) / (0.7f * radius)).Clamp( 0, 1 );
	}
}
