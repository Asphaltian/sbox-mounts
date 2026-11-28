using System;
using Sandbox.Npcs;

namespace Source1.Hl2;

[Hide]
public sealed class LookDoor : LinearMover
{
	[Property] public string Looker { get; set; }
	[Property] public Vector3 CenterOffset { get; set; }
	[Property] public float Proximity { get; set; }
	[Property] public float Offset { get; set; }
	[Property] public float FieldOfView { get; set; }
	[Property] public bool Threshold { get; set; }
	[Property] public bool FromOpen { get; set; }
	[Property] public bool Inverted { get; set; }

	GameObject _target;

	protected override void OnFixedUpdate()
	{
		if ( Networking.IsHost && !string.IsNullOrEmpty( Looker ) )
		{
			if ( Looker == "!player" )
			{
				float nearest = float.PositiveInfinity;
				_target = null;
				foreach ( var player in Scene.GetAll<PlayerController>() )
				{
					var distance = player.WorldPosition.DistanceSquared( WorldPosition );
					if ( distance >= nearest ) continue;
					nearest = distance;
					_target = player.GameObject;
				}
			}
			else if ( !_target.IsValid() )
				_target = Scene.Directory.FindByName( Looker ).FirstOrDefault();

			if ( _target.IsValid() ) Follow( _target );
		}
		base.OnFixedUpdate();
	}

	void Follow( GameObject target )
	{
		var center = CenterOffset + (FromOpen ? Closed + Travel : Vector3.Zero);
		if ( GameObject.Parent is { } parent ) center = parent.WorldTransform.PointToWorld( center );
		var distance = Math.Max( 0, center.Distance( target.WorldPosition ) - Offset );
		var fraction = Proximity > 0 && distance < Proximity ? (Threshold ? 1 : 1 - distance / Proximity) : 0;
		if ( FieldOfView > 0 )
		{
			Transform eye;
			if ( target.GetComponent<PlayerController>() is { } player ) eye = player.EyeTransform;
			else if ( target.GetComponent<BaseNpc>() is { } npc ) eye = new Transform( npc.EyePosition, npc.WorldRotation );
			else return;
			var dot = eye.Rotation.Forward.Dot( (center - eye.Position).Normal );
			if ( dot >= FieldOfView ) fraction += FieldOfView < 1 ? (dot - FieldOfView) / (1 - FieldOfView) : 1;
		}
		fraction = Math.Clamp( fraction, 0, 1 );
		SetPosition( Inverted ? 1 - fraction : fraction );
	}

	public void SetInverted( bool inverted )
	{
		if ( Networking.IsHost ) Inverted = inverted;
	}

	internal override Doo.Block BuildInput( string input, string parameter ) => input.ToLowerInvariant() switch
	{
		"inverton" => Input( nameof( SetInverted ), true ),
		"invertoff" => Input( nameof( SetInverted ), false ),
		_ => base.BuildInput( input, parameter )
	};
}
