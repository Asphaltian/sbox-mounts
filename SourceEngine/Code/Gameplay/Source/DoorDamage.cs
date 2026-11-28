using Sandbox.ModelEditor.Nodes;

namespace Source1;

[Hide]
public sealed class DoorDamage : Component, Component.IDamageable
{
	[Property] public PropDoor Door { get; set; }
	[Property] public int MaxHealth { get; set; }
	[Property, Sync] public int Health { get; set; }
	[Property] public bool Breakable { get; set; }
	[Property] public List<Model> Stages { get; set; } = [];
	[Property] public Doo OnBreak { get; set; }

	[Sync, Change( nameof( ApplyStage ) )] public int Stage { get; private set; }

	public void OnDamage( in DamageInfo hit )
	{
		if ( !Networking.IsHost || !Breakable || Health <= 0 || hit.Damage <= 0 ) return;
		var modifiers = Door.Renderer.Model.GetData<PropDamageModifiers>();
		var blast = hit.Tags.Has( DamageTags.Explosion ) || hit.Tags.Has( "blast" );
		var damage = blast && hit.Damage >= 40 ? MaxHealth : hit.Damage;
		if ( blast ) damage *= modifiers?.Explosive ?? 1;
		if ( hit.Tags.Has( DamageTags.Bullet ) ) damage *= (modifiers?.Bullet ?? 1) * (hit.Tags.Has( "buckshot" ) ? 2 : 1);
		if ( hit.Tags.Has( DamageTags.Melee ) ) damage *= modifiers?.Club ?? 1;
		Health = System.Math.Max( 0, Health - (int)damage );

		var perStage = MaxHealth / (Stages.Count + 1);
		var stage = Stage;
		while ( stage < Stages.Count && (Health == 0 || Health < MaxHealth - perStage * (stage + 1)) ) stage++;
		if ( stage == Stage && Health > 0 ) return;
		var model = Door.Renderer.Model;
		for ( var i = Stage; i < stage; i++ )
		{
			BreakPieces( model, final: false );
			model = Stages[i];
		}
		if ( Health == 0 ) BreakPieces( model, final: true );
		Stage = stage;

		RunDoo( OnBreak, c => c.SetArgument( "user", GameObject ) );
		if ( Health == 0 ) GameObject.Destroy();
	}

	void BreakPieces( Model model, bool final )
	{
		if ( model?.GetData<ModelBreakPiece[]>() is not { Length: > 0 } ) return;
		SpawnPieces( model, WorldTransform, Door.Renderer.MaterialGroup, Door.Renderer.Tint, final );
	}

	[Rpc.Broadcast( NetFlags.HostOnly )]
	void SpawnPieces( Model model, Transform pose, string skin, Color tint, bool final )
	{
		if ( model?.GetData<ModelBreakPiece[]>() is not { } pieces ) return;
		foreach ( var piece in pieces )
		{
			var local = !final || piece.IsClientOnly;
			if ( local ? Application.IsDedicatedServer : !Networking.IsHost ) continue;
			var fragment = Model.Load( piece.Model );
			if ( fragment is null || fragment.IsError ) continue;
			var offset = fragment.Attachments.GetTransform( "placementOrigin" )?.PointToLocal( piece.Offset ) ?? piece.Offset;
			var obj = new GameObject( false, fragment.Name );
			obj.WorldTransform = pose.WithPosition( pose.PointToWorld( offset ) );
			obj.Tags.Add( piece.CollisionTags.Split( ' ', System.StringSplitOptions.RemoveEmptyEntries ) );
			var gib = obj.AddComponent<Gib>();
			gib.Model = fragment;
			gib.MaterialGroup = skin;
			gib.Tint = tint;
			gib.FadeTime = piece.FadeTime;
			if ( local )
			{
				obj.Tags.Add( "debris", "clientside" );
				obj.Flags |= GameObjectFlags.NotNetworked;
			}
			else obj.NetworkSpawn();
			obj.Enabled = true;
		}
	}

	void ApplyStage( int previous, int current )
	{
		if ( current < 1 || current > Stages.Count || !Door.IsValid() ) return;
		var model = Stages[current - 1];
		Door.Renderer.Model = model;
		if ( GetComponent<ModelCollider>() is { } collider ) collider.Model = model;
	}
}
