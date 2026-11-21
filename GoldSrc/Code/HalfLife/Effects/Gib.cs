namespace GoldSrc.HalfLife;

[Hide]
public sealed class Gib : Component, Component.ICollisionListener
{
	Npc.BloodColor _blood;
	ModelRenderer _renderer;
	Rigidbody _body;
	int _decals = 5;
	TimeSince _age;
	TimeUntil _fade;
	bool _landed;

	[Rpc.Broadcast( NetFlags.HostOnly )]
	internal static void Scatter( BBox hull, Vector3 eye, Vector3 direction, float health, bool human, Npc.BloodColor blood, int pieces = 4, bool skull = true )
	{
		if ( Sandbox.Mounting.Directory.Get( "hl1" ) is not HalfLifeMount mount ) return;
		var file = human ? "hgibs" : "agibs";
		var path = mount.GameDirs.Select( dir => $"mount://hl1/{dir}/models/{file}.mdl.vmdl" ).FirstOrDefault( name => mount.GetByPath( name ) is ModelLoader );
		if ( path is null ) return;
		var model = Model.Load( path );
		if ( !model.IsValid() || model.IsError || model.Parts.All.FirstOrDefault() is not { } part ) return;
		var scale = health > -50 ? 0.7f : health > -200 ? 2 : 4;
		// The human model also contains prop pieces. Monsters only use its first six choices.
		var lastPiece = System.Math.Min( human ? 6 : 4, part.Choices.Count ) - 1;
		if ( lastPiece < (human ? 1 : 0) ) return;
		if ( human && skull )
		{
			var velocity = new Vector3( Game.Random.Float( -100, 100 ), Game.Random.Float( -100, 100 ), Game.Random.Float( 200, 300 ) );
			if ( Game.Random.Int( 0, 100 ) <= 5 && Game.ActiveScene.GetAllComponents<PlayerController>().OrderBy( p => p.WorldPosition.DistanceSquared( eye ) ).FirstOrDefault() is { } player )
				velocity = (player.EyeTransform.Position - eye).Normal * 300 + Vector3.Up * 100;
			Spawn( model, part.Name, 0, eye, velocity * scale, blood );
		}
		for ( int i = 0; i < pieces; ++i )
		{
			var spread = new Vector3( Game.Random.Float( -0.25f, 0.25f ), Game.Random.Float( -0.25f, 0.25f ), Game.Random.Float( -0.25f, 0.25f ) );
			var velocity = (direction + spread) * Game.Random.Float( 300, 400 ) * scale;
			Spawn( model, part.Name, Game.Random.Int( human ? 1 : 0, lastPiece ), hull.RandomPointInside + Vector3.Up, velocity, blood );
		}
	}

	internal static void Spawn( Model model, string group, int variant, Vector3 point, Vector3 velocity, Npc.BloodColor blood, float fadeAfter = 0 )
	{
		var go = new GameObject( false, "Gib" );
		go.NetworkMode = NetworkMode.Never;
		go.Tags.Add( "debris" );
		go.WorldPosition = point;
		var gib = go.AddComponent<Gib>();
		gib._blood = blood;
		gib._renderer = go.AddComponent<ModelRenderer>();
		gib._renderer.Model = model;
		gib._renderer.SetBodyGroup( group, variant );
		var collider = go.AddComponent<SphereCollider>();
		collider.Radius = 1;
		collider.Friction = 0.55f;
		collider.Elasticity = 0.5f;
		gib._body = go.AddComponent<Rigidbody>();
		go.Enabled = true;
		gib._body.Velocity = velocity.ClampLength( 1500 );
		gib._body.AngularVelocity = new Vector3( 0, Game.Random.Float( 100, 200 ).DegreeToRadian(), Game.Random.Float( 100, 300 ).DegreeToRadian() );
		gib._age = 0;
		if ( fadeAfter > 0 )
		{
			gib._landed = true;
			gib._fade = fadeAfter;
		}
	}

	void ICollisionListener.OnCollisionStart( Collision collision )
	{
		if ( _decals <= 0 || _blood == Npc.BloodColor.None ) return;
		_decals--;
		var trace = Scene.Trace.Ray( WorldPosition + Vector3.Up * 8, WorldPosition + Vector3.Down * 16 )
			.WithoutTags( "debris", "npc", "trigger", "player", "ragdoll" ).Run();
		if ( trace.Hit )
		{
			var prefix = _blood == Npc.BloodColor.Red ? "blood" : "yblood";
			ImpactDecal.Place( trace.EndPosition, trace.Normal, $"{prefix}{Game.Random.Int( 1, 6 )}", 0 );
		}
	}

	protected override void OnUpdate()
	{
		if ( !_landed )
		{
			if ( WorldPosition.z < -32768 ) { GameObject.Destroy(); return; }
			if ( _age < 4 || !_body.Sleeping ) return;
			_landed = true;
			_fade = 25;
			if ( Networking.IsHost && _blood != Npc.BloodColor.None )
				Scene.GetSystem<Sandbox.Npcs.NpcSystem>().EmitStimulus( "meat", WorldPosition, GameObject, 384, 25 );
		}
		if ( _fade > 0 ) return;
		var alpha = 1f + (float)_fade * 0.7f;
		if ( alpha <= 0 ) GameObject.Destroy();
		else _renderer.Tint = Color.White.WithAlpha( alpha );
	}
}
