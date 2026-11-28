namespace Source1.Css;

public sealed class C4 : Weapon, PlayerController.IEvents
{
	internal const string SiteTag = "css_bombsite";
	const float PlantTime = 3;
	bool _holding;
	bool _placing;
	TimeUntil? _plantAt;
	SourceModelSequenceData _worldSequence;
	TimeSince _sequenceStarted;
	int _nextEvent;

	public override bool CanSecondaryAttack() => false;
	public override bool CanPrimaryAttack() => !_holding && base.CanPrimaryAttack() && AtSite();

	void PlayerController.IEvents.PreInput()
	{
		if ( !_holding || !IsActive ) return;
		Input.AnalogMove = Vector3.Zero;
		Input.Clear( "jump" );
	}

	bool AtSite()
	{
		if ( !Owner.IsValid() || !Owner.IsOnGround ) return false;
		var ground = Owner.GroundObject;
		if ( ground.IsValid() && (ground.GetComponentInParent<PlayerController>().IsValid()
			|| ground.GetComponentInParent<Rigidbody>() is { MotionEnabled: true }) ) return false;
		return InZone( Owner.BodyCollider ) || InZone( Owner.FeetCollider );
	}

	static bool InZone( Collider collider ) =>
		collider.IsValid() && collider.Touching.Any( other => other.Tags.Has( SiteTag ) );

	public override void PrimaryAttack()
	{
		_holding = true;
		BeginPlant();
	}

	[Rpc.Host( NetFlags.OwnerOnly )]
	void BeginPlant()
	{
		if ( !IsActive || _plantAt.HasValue || !AtSite() ) return;
		_plantAt = PlantTime;
		PlantAnimation( "ACT_VM_PRIMARYATTACK" );
	}

	protected override void Think()
	{
		base.Think();
		if ( !_holding || (Input.Down( "attack1" ) && AtSite()) ) return;
		_holding = false;
		SetNextPrimaryFire( 1 );
		CancelPlant();
	}

	protected override void OnHolstered()
	{
		_holding = false;
		if ( Networking.IsHost ) StopPlant();
		base.OnHolstered();
	}

	[Rpc.Host( NetFlags.OwnerOnly )]
	void CancelPlant() => StopPlant();

	void StopPlant()
	{
		if ( !_plantAt.HasValue ) return;
		_plantAt = null;
		PlantAnimation( _placing ? "ACT_VM_DRAW" : "ACT_VM_IDLE" );
		_placing = false;
	}

	[Rpc.Broadcast( NetFlags.HostOnly )]
	void PlantAnimation( string activity )
	{
		_worldSequence = null;
		if ( Application.IsDedicatedServer || Setup.ViewModel is not { } model ) return;
		var sequences = ModelSequences.Of( model );
		var clip = sequences.FindActivity( activity );
		if ( WeaponModel is ViewModel view )
			view.OnThrow( clip );
		else if ( activity == "ACT_VM_PRIMARYATTACK" )
		{
			_worldSequence = sequences.Get( clip );
			_sequenceStarted = 0;
			_nextEvent = 0;
		}
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();
		if ( IsActive && _worldSequence is { } sequence )
		{
			var cycle = (float)_sequenceStarted / (PlantTime - 0.75f);
			while ( _nextEvent < sequence.Events.Length && sequence.Events[_nextEvent].Cycle <= cycle )
			{
				var evt = sequence.Events[_nextEvent++];
				if ( Sounds.IsSoundEvent( evt ) && Sounds.Sound( evt.Options, MountContent.OfWeapon( this ) ) is { } sound )
					Sound.Play( sound, WorldPosition );
			}
		}
		if ( !Networking.IsHost || _plantAt is not { } remaining ) return;
		if ( !IsActive || !AtSite() )
		{
			StopPlant();
			return;
		}
		if ( !_placing && remaining <= 0.75f )
		{
			_placing = true;
			PlantAnimation( "ACT_VM_SECONDARYATTACK" );
		}
		if ( remaining > 0 ) return;
		_plantAt = null;
		var ground = Scene.Trace.Ray( Owner.WorldPosition + Vector3.Up * 8, Owner.WorldPosition + Vector3.Down * 200 )
			.IgnoreGameObjectHierarchy( Owner.GameObject ).WithoutTags( "trigger" ).Run();
		var position = ground.Hit ? ground.EndPosition : Owner.WorldPosition;
		PlantedC4.Spawn( position, Owner.EyeAngles.yaw, Owner.GameObject, MountContent.OfWeapon( this ) );
		Inventory.Remove( this );
	}
}

public sealed class PlantedC4 : Component
{
	GameObject _planter;
	const float Timer = 45;
	[Sync( SyncFlags.FromHost )] TimeUntil Expires { get; set; }
	TimeUntil _nextBeep;
	TimeUntil _glowEnds;
	SpriteRenderer _glow;
	SoundEvent _beep;

	internal static void Spawn( Vector3 position, float yaw, GameObject planter, string mount )
	{
		var go = new GameObject( false, "Planted C4" );
		go.WorldTransform = new Transform( position, Rotation.FromYaw( yaw ) );
		var model = go.AddComponent<MountedModel>();
		model.Renderer = go.AddComponent<SkinnedModelRenderer>();
		model.Renderer.Enabled = false;
		model.MountName = mount;
		model.ModelPath = "models/weapons/w_c4_planted.mdl";
		var bomb = go.AddComponent<PlantedC4>();
		bomb._planter = planter;
		bomb.Expires = Timer;
		go.Enabled = true;
		go.NetworkSpawn();
	}

	protected override void OnStart()
	{
		if ( Application.IsDedicatedServer ) return;
		_nextBeep = 1;
		var mount = GetComponent<MountedModel>().MountName;
		_beep = Sounds.Sound( "C4.PlantSound", mount );
		_glow = MountedEffects.AddSprite( GameObject, "sprites/ledglow", mount, useViewModelLayer: false );
		if ( !_glow.IsValid() ) return;
		_glow.LocalPosition = Vector3.Up * 4;
		_glow.Size = 3;
	}

	protected override void OnUpdate()
	{
		if ( Application.IsDedicatedServer ) return;
		if ( _nextBeep <= 0 )
		{
			if ( _beep is not null ) Sound.Play( _beep, WorldPosition );
			_nextBeep = System.MathF.Max( 0.15f, 0.1f + 0.9f * ((float)Expires / Timer).Clamp( 0, 1 ) );
			_glowEnds = 0.05f;
		}
		if ( _glow.IsValid() ) _glow.Enabled = _glowEnds > 0;
	}

	protected override void OnFixedUpdate()
	{
		if ( !Networking.IsHost || Expires > 0 ) return;
		var mount = GetComponent<MountedModel>().MountName;
		ExplosionEffect.Shake( WorldPosition, 25, 3000 );
		new Explosion
		{
			Position = WorldPosition,
			Source = GameObject,
			Attacker = _planter,
			Prefab = PrefabFile.Load( $"mount://{mount}/entities/effects/c4_explosion.prefab" ),
			Damage = 500,
			Radius = 1750
		}.Spawn();
		GameObject.Destroy();
	}
}
