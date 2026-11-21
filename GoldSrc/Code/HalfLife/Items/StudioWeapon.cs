namespace GoldSrc.HalfLife;

[System.AttributeUsage( System.AttributeTargets.Class, Inherited = false )]
internal sealed class WeaponPickupAttribute( string model, string ammo, int limit ) : System.Attribute
{
	internal string Model => model;
	internal string Ammo => ammo;
	internal int Limit => limit;
	public string AlternateModel { get; set; }
	internal IEnumerable<string> ViewModelPaths
	{
		get
		{
			yield return $"models/v_{Model}.mdl";
			if ( AlternateModel is not null ) yield return $"models/v_{AlternateModel}.mdl";
		}
	}
	public string ClassName { get; set; }
	public string PickupModel { get; set; }
	internal string WorldPickupPath => $"models/{PickupModel ?? $"w_{model}"}.mdl";
	internal string PrefabPath => $"entities/items/weapon_{ClassName ?? model}.prefab";
}

[Hide]
public abstract partial class StudioWeapon : BaseCombatWeapon
{
	WeaponPickupAttribute _definition;
	internal WeaponPickupAttribute Definition => _definition ??= Game.TypeLibrary.GetType( GetType() ).GetAttribute<WeaponPickupAttribute>();
	internal virtual string DeploySequence => "draw";
	internal virtual bool CanIdle => true;
	internal virtual string IdleSequence => "idle1";
	internal virtual string ShotSequence => "shoot";
	internal virtual string ReloadSequence => "reload";

	internal abstract void Configure();
	internal virtual void PosePickup( SkinnedModelRenderer renderer ) { }

	// The p_ models have no attachments. These are barrel-tip positions in their right-hand bone space.
	internal virtual Vector3 WorldMuzzleOffset( bool hd ) => Vector3.Zero;
	internal virtual string ShotSound => null;

	internal virtual void PlayShotSound()
	{
		if ( ShotSound is { } sample ) PlaySample( sample );
	}

	protected override void OnShootEffects( ShotEffect shot )
	{
		if ( !Application.IsDedicatedServer && (shot.Hit || shot.HitPosition != default) )
		{
			var start = GetMuzzleTransform().Position;
			shot = shot with { Origin = start };
			DrawShotTrail( start, shot.HitPosition );
		}
		base.OnShootEffects( shot );
		if ( !shot.NoEvents ) PlayShotSound();
	}

	internal virtual void DrawShotTrail( Vector3 start, Vector3 end ) => BulletEffects.ShowTracer( start, end );

	protected override void OnShootImpact( in ShotEffect shot )
	{
		if ( !shot.Hit || Sandbox.Mounting.Directory.Get( "hl1" ) is not HalfLifeMount mount ) return;
		var flesh = HitFlesh( shot );
		var sound = mount.GetBulletImpactSound( shot.Surface, flesh );
		BulletEffects.ShowImpact( shot.HitPosition, shot.Normal, sound, flesh ? 0 : Game.Random.Int( 1, 5 ) );
	}

	protected static bool HitFlesh( in ShotEffect shot ) => shot.HitObject.IsValid() &&
		GoldSrc.HalfLife.Npc.ClassOf( shot.HitObject ) is not (MonsterClass.None or MonsterClass.Machine);

	protected override SceneTrace BulletTrace( Ray ray, float distance, float radius ) =>
		TraceLineOfFire( ray, distance, radius );

	SceneTrace TraceLineOfFire( Ray ray, float distance, float radius ) =>
		base.BulletTrace( ray, distance, radius ).WithCollisionRules( "bullet" ).WithoutTags( "playercontroller" );

	Transform _worldMuzzle;
	SkinnedModelRenderer _viewRenderer;

	public override Transform GetMuzzleTransform()
	{
		if ( Owner is { ThirdPerson: false } && !IsProxy && _viewRenderer.IsValid() &&
			_viewRenderer.GetAttachment( "attachment1" ) is { } muzzle )
			return muzzle;

		if ( !WorldModel.IsValid() ) return base.GetMuzzleTransform();
		UpdateHeldGrip();
		return WorldModel.WorldTransform.ToWorld( _worldMuzzle );
	}

	public override Ray AimRay
	{
		get
		{
			if ( !IsHeld ) return base.AimRay;
			var sight = IsProxy && Owner.IsValid() ? Owner.EyeTransform.ForwardRay : base.AimRay;
			var aim = TraceLineOfFire( sight, Ballistics.Range, 0 ).Run().EndPosition;
			var muzzle = GetMuzzleTransform().Position;

			// If the barrel sticks through cover, keep the shot on the shooter's side.
			var eye = Owner.IsValid() ? Owner.EyePosition : sight.Position;
			var cover = TraceLineOfFire( new Ray( eye, (muzzle - eye).Normal ), eye.Distance( muzzle ), 0 ).Run();
			if ( cover.Hit || cover.StartedSolid )
				return new Ray( cover.StartedSolid ? eye : cover.EndPosition - (muzzle - eye).Normal * 0.1f, (muzzle - eye).Normal );

			var direction = aim - muzzle;
			return new Ray( muzzle, direction.Dot( sight.Forward ) > 0 ? direction.Normal : sight.Forward );
		}
	}

	protected override bool OnDrop()
	{
		var player = Owner;
		if ( !player.IsValid() ) return base.OnDrop();

		var eye = player.EyeTransform;
		var velocity = player.Velocity + eye.Forward * 200f + Vector3.Up * 100f;
		Network.DropOwnership();
		GameObject.SetParent( null, true );
		GameObject.Enabled = true;
		WorldPosition = eye.Position + eye.Forward * 48f;
		Slot = -1;

		if ( GetComponent<Rigidbody>( true ) is { } body )
		{
			body.Velocity = velocity;
			body.AngularVelocity = Vector3.Random * 8f;
		}
		return true;
	}

	internal void SetupPickup()
	{
		DisplayName = Game.TypeLibrary.GetType( GetType() ).Title;
		UsesAmmo = UsesClips = true;
		if ( Definition.Ammo is not null ) PrimaryAmmoType = GameResource.Load<BaseAmmoResource>( $"mount://hl1/ammo/{Definition.Ammo}.ammo" );
		Configure();
	}

	protected void EjectShells( int count, bool shotgun )
	{
		if ( Application.IsDedicatedServer || !Owner.IsValid() ) return;
		var pose = Owner.EyeTransform;
		var origin = pose.PointToWorld( new Vector3( 32, -6, -12 ) );
		for ( int i = 0; i < count; i++ ) ShellCasing.Eject( origin, pose.Rotation, shotgun, Owner.Velocity );
	}

	protected SoundHandle PlaySample( string sample, float volume = 1, float pitch = 1 )
	{
		if ( !Application.IsDedicatedServer && Sandbox.Mounting.Directory.Get( "hl1" ) is HalfLifeMount mount )
			return NpcAudio.Play( mount.NpcAudio.Find( sample ), WorldPosition, volume, pitch );
		return null;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		if ( GetComponent<ModelRenderer>( true ) is { } loose ) loose.Enabled = !IsHeld;
	}

	protected bool Zoomed { get; private set; }
	internal virtual float ZoomFieldOfView => 0;

	public override void SecondaryAttack()
	{
		if ( ZoomFieldOfView > 0 ) Zoomed = !Zoomed;
		else base.SecondaryAttack();
	}

	protected override void ModifyCamera( CameraComponent camera, ref CameraView view )
	{
		base.ModifyCamera( camera, ref view );
		if ( Zoomed ) view.FieldOfView = ZoomFieldOfView;
	}

	protected override void OnReloadStarted()
	{
		Zoomed = false;
		base.OnReloadStarted();
	}

	protected override void OnHolstered()
	{
		Zoomed = false;
		base.OnHolstered();
	}

	protected override void CreateViewModel()
	{
		if ( ViewModel.IsValid() || IsProxy || Holder is not PlayerController ) return;
		ViewModel = MakeModel( $"models/v_{HeldModelName}.mdl", GameObject, true );
	}

	GameObject MakeModel( string relativePath, GameObject parent, bool firstPerson )
	{
		if ( Sandbox.Mounting.Directory.Get( "hl1" ) is not HalfLifeMount mount ) return null;
		foreach ( var dir in mount.GameDirs )
		{
			var path = $"mount://hl1/{dir}/{relativePath}.vmdl";
			if ( mount.GetByPath( path ) is not ModelLoader loader ) continue;
			var model = Model.Load( path );
			if ( !model.IsValid() || model.IsError ) continue;
			var go = new GameObject( parent, false, DisplayName );
			go.Flags |= GameObjectFlags.NotNetworked | GameObjectFlags.NotSaved;
			var renderer = go.AddComponent<SkinnedModelRenderer>();
			renderer.Model = model;
			if ( firstPerson )
			{
				_viewRenderer = renderer;
				go.Flags |= GameObjectFlags.Absolute;
				go.Tags.Add( "firstperson", "viewmodel" );
				renderer.RenderOptions.Game = false;
				renderer.RenderOptions.Overlay = true;
				var view = go.AddComponent<StudioWeaponView>();
				view.Renderer = renderer;
				view.TracerEffect = null;
				view.Initialize( this, loader, mount.NpcAudio );
			}
			else AlignHeldModel( renderer, dir.EndsWith( "_hd" ) );
			go.Enabled = true;
			return go;
		}
		return null;
	}
}
