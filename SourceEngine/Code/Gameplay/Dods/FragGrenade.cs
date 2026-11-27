using System;

namespace Source1.Dods;

[WeaponProjectile( Projectile.Kinds.DodFrag )]
public sealed class FragGrenade : ThrownGrenade, Component.IPressable
{
	[Sync( SyncFlags.FromHost )] public string LivePrefab { get; set; }
	[Property] public float Fuse { get; set; } = 5;
	protected override float Lifetime => Fuse;
	protected internal override BlastMode Blast => BlastMode.Traced;

	protected override string BounceSound => "HEGrenade.Bounce";

	bool IPressable.CanPress( IPressable.Event e ) => LivePrefab is not null;

	bool IPressable.Press( IPressable.Event e )
	{
		PickUp( e.Source.GameObject );
		return true;
	}

	[Rpc.Host]
	void PickUp( GameObject source )
	{
		var player = source?.GetComponentInParent<PlayerController>();
		if ( !player.IsValid() || (Rpc.Caller != Connection.Local && player.Network.Owner != Rpc.Caller) ) return;
		if ( player.EyeTransform.Position.Distance( WorldPosition ) > player.ReachLength || Age >= Fuse || LivePrefab is null ) return;
		var inventory = player.GameObject.Root.GetComponentInChildren<BaseInventoryComponent>();
		if ( !inventory.IsValid() || inventory.GetComponentsInChildren<HandGrenade>( true ).Any( w => w.Live ) ) return;
		var prefab = GameObject.GetPrefab( LivePrefab );
		if ( prefab?.GetComponent<HandGrenade>( true ) is not { Live: true } template ) return;
		var reserve = inventory.GetAmmo( template.PrimaryAmmoType );
		if ( inventory.Pickup( prefab ) is not HandGrenade item ) return;
		item.DetonateAt = Time.Now + Fuse - Age;
		inventory.Switch( item );
		if ( inventory.ActiveItem != item )
		{
			item.DetonateAt = 0;
			inventory.Remove( item );
			inventory.SetAmmo( template.PrimaryAmmoType, reserve );
			return;
		}
		GameObject.Destroy();
	}

	protected override void Expire()
	{
		Damage = 150;
		Radius = Damage * 2.5f;
		var floor = Scene.Trace.Ray( WorldPosition + Vector3.Up * 8, WorldPosition + Vector3.Down * 32 )
			.IgnoreGameObjectHierarchy( GameObject ).WithoutTags( "trigger" ).Run();
		Explode( particle: floor.Hit ? "explosioncore_floor" : "explosioncore_midair",
			rotation: floor.Hit ? Rotation.LookAt( floor.Normal ) : Rotation.Identity );
	}
}
