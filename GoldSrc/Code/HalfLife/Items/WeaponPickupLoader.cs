namespace GoldSrc.HalfLife;

internal sealed class WeaponPickupLoader( TypeDescription type, string modelPath, string iconPath ) : ResourceLoader<GameMount>
{
	PrefabFile _prefab;

	protected override object Load()
	{
		var path = $"mount://{Host.Ident}/{modelPath}.vmdl";
		var builder = new PrefabBuilder().WithName( Path ).WithMetadata( "Title", type.Title ).WithMetadata( "PreviewModel", path );
		using ( builder.Scope() )
		{
			Game.ActiveScene.Enabled = false;
			var root = new GameObject( true, type.Title );
			root.NetworkMode = NetworkMode.Object;
			root.Tags.Add( "removable" );
			root.Tags.Add( "nocollide_props" );
			var model = Model.Load( path );
			var renderer = root.AddComponent<SkinnedModelRenderer>();
			renderer.Model = model;
			var hull = root.AddComponent<BoxCollider>();
			hull.Scale = model.Bounds.Size;
			hull.Center = model.Bounds.Center;
			root.AddComponent<Rigidbody>();
			var weapon = (StudioWeapon)root.Components.Create( type );
			if ( Texture.Load( iconPath ) is { IsError: false } icon ) weapon.DisplayIcon = icon;
			weapon.SetupPickup();
			weapon.PosePickup( renderer );
		}
		return _prefab = builder.Create();
	}

	internal static bool Drop<T>( Transform transform, bool toss = false ) where T : StudioWeapon
	{
		var definition = Game.TypeLibrary.GetType( typeof( T ) ).GetAttribute<WeaponPickupAttribute>();
		var prefab = GameObject.GetPrefab( $"mount://hl1/{definition.PrefabPath}" );
		if ( !prefab.IsValid() ) return false;
		var weapon = prefab.Clone( transform );
		var body = weapon.GetComponent<Rigidbody>();
		ApplyDropVelocity( body, toss );
		weapon.NetworkSpawn( true, null );
		return true;
	}

	internal static void ApplyDropVelocity( Rigidbody body, bool toss )
	{
		if ( toss ) body.Velocity = new Vector3( Game.Random.Float( -100, 100 ), Game.Random.Float( -100, 100 ), Game.Random.Float( 200, 300 ) );
		body.AngularVelocity = Vector3.Up * (toss ? Game.Random.Float( 200, 400 ) : Game.Random.Float( 0, 100 )).DegreeToRadian();
	}

	protected override void Shutdown()
	{
		PrefabBuilder.Destroy( _prefab );
		_prefab = null;
		base.Shutdown();
	}
}
