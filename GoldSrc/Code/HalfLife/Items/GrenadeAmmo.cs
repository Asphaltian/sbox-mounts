namespace GoldSrc.HalfLife;

[Hide]
public sealed class GrenadeAmmo : BaseAmmoPickup, Component.ITriggerListener
{
	internal static void Drop( Transform transform, bool toss = false )
	{
		if ( Sandbox.Mounting.Directory.Get( "hl1" ) is not HalfLifeMount mount ) return;
		foreach ( var dir in mount.GameDirs )
		{
			var path = $"mount://hl1/{dir}/models/w_argrenade.mdl.vmdl";
			if ( mount.GetByPath( path ) is not ModelLoader ) continue;
			var model = Model.Load( path );
			if ( !model.IsValid() || model.IsError ) continue;

			var go = new GameObject( false, "AR grenades" );
			go.Tags.Add( "prop", "removable" );
			go.WorldTransform = transform;
			go.AddComponent<ModelRenderer>().Model = model;
			var collider = go.AddComponent<BoxCollider>();
			collider.Center = model.Bounds.Center;
			collider.Scale = model.Bounds.Size;
			var body = go.AddComponent<Rigidbody>();
			var pickup = go.AddComponent<GrenadeAmmo>();
			pickup.DisplayName = "AR grenades";
			pickup.AmmoType = GameResource.Load<BaseAmmoResource>( $"mount://hl1/{Rifle.GrenadeAmmoPath}" );
			pickup.Amount = 2;
			var trigger = go.AddComponent<BoxCollider>();
			trigger.IsTrigger = true;
			trigger.Center = collider.Center;
			trigger.Scale = collider.Scale + new Vector3( 24, 24, 12 );
			go.Enabled = true;
			WeaponPickupLoader.ApplyDropVelocity( body, toss );
			go.NetworkSpawn( true, null );
			return;
		}
	}

	void ITriggerListener.OnTriggerEnter( GameObject other )
	{
		if ( !Networking.IsHost || GameObject.IsDestroyed ) return;
		var inventory = other.GetComponentInParent<PlayerController>()?.GameObject.GetComponentInChildren<BaseInventoryComponent>();
		if ( inventory?.PickupMode == BaseInventoryComponent.PickupBehaviour.Touch ) inventory.Add( this );
	}

	protected override bool OnAdding( BaseInventoryComponent inventory )
	{
		if ( AmmoType is not null && inventory.GetAmmo( AmmoType ) < AmmoType.MaxReserve
			&& Sandbox.Mounting.Directory.Get( "hl1" ) is HalfLifeMount mount )
			mount.NpcAudio.Emit( "items/9mmclip1.wav", WorldPosition );
		return base.OnAdding( inventory );
	}
}
