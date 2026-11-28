using System;
using System.Globalization;

namespace Source1;

[Library]
public static class DoorInputs
{
	internal static Doo.Block Build( Component door, string input, string parameter )
	{
		var name = input.ToLowerInvariant();
		string method;
		Variant value;
		if ( door is BrushDoor or LinearMover && name == "setparentattachment" )
		{
			method = nameof( SetParentAttachment );
			value = parameter;
		}
		else if ( door is BrushDoor or LinearMover && name is ("clearparent" or "setparent") )
		{
			method = nameof( SetParent );
			value = name == "clearparent" ? "" : parameter;
		}
		else if ( door is LinearMover ) return null;
		else if ( name is "lock" or "unlock" )
		{
			method = nameof( SetLocked );
			value = name == "lock";
		}
		else if ( door is PropDoor && name is ("setbreakable" or "setunbreakable") )
		{
			method = nameof( SetBreakable );
			value = name == "setbreakable";
		}
		else if ( name == "setspeed" || (door is BrushDoor && name == "settogglestate") || (door is PropDoor && name is ("setrotationdistance" or "movetorotationdistance")) )
		{
			if ( !float.TryParse( parameter, NumberStyles.Float, CultureInfo.InvariantCulture, out var amount ) || !float.IsFinite( amount ) ) return null;
			method = name switch
			{
				"setspeed" => nameof( SetSpeed ),
				"settogglestate" => nameof( SetToggleState ),
				"movetorotationdistance" => nameof( MoveToRotationDistance ),
				_ => nameof( SetRotationDistance )
			};
			value = amount;
		}
		else return null;

		return new Doo.InvokeBlock
		{
			InvokeType = Doo.InvokeType.Static,
			Member = $"{typeof( DoorInputs ).FullName}.{method}",
			Arguments = [new Doo.LiteralExpression { LiteralValue = door.GameObject }, new Doo.LiteralExpression { LiteralValue = value }]
		};
	}

	public static void SetParent( GameObject target, string name )
	{
		if ( !Networking.IsHost || !target.IsValid() ) return;
		var parent = string.IsNullOrEmpty( name ) ? target.Scene : target.Scene.Directory.FindByName( name ).FirstOrDefault();
		if ( parent.IsValid() && !parent.IsAncestor( target ) ) Reparent( target, parent );
	}

	internal static void Reparent( GameObject target, GameObject parent )
	{
		if ( target.Parent == parent ) return;
		var frame = parent.WorldTransform.ToLocal( target.Parent.WorldTransform );
		target.SetParent( parent, keepWorldPosition: true );
		Rebase( target, frame );
	}

	public static void SetParentAttachment( GameObject target, string name )
	{
		if ( !Networking.IsHost || !target.IsValid() || string.IsNullOrEmpty( name ) ) return;
		var parent = target.Parent;
		if ( !parent.IsValid() || parent is Scene ) return;
		if ( parent.Flags.Contains( GameObjectFlags.Attachment ) ) parent = parent.Parent;
		var renderer = parent?.GetComponent<ModelRenderer>();
		if ( renderer?.Model?.Attachments.Get( name ) is not { } attachment ) return;
		renderer.CreateAttachments = true;
		if ( renderer.GetAttachmentObject( attachment ) is not { } anchor ) return;

		var frame = target.WorldTransform.WithScale( anchor.WorldScale ).ToLocal( target.Parent.WorldTransform );
		target.SetParent( anchor, keepWorldPosition: true );
		target.LocalPosition = Vector3.Zero;
		target.LocalRotation = Rotation.Identity;
		Rebase( target, frame );
	}

	static void Rebase( GameObject target, Transform frame )
	{
		if ( target.GetComponent<BrushDoor>( true ) is { } door ) door.Rebase( frame );
		else target.GetComponent<LinearMover>( true )?.Rebase( frame );
	}

	public static void SetToggleState( GameObject target, float state )
	{
		if ( target.IsValid() ) target.GetComponent<BrushDoor>()?.SetToggleState( state );
	}

	public static void SetBreakable( GameObject target, bool value )
	{
		if ( Networking.IsHost && target.IsValid() && target.GetComponent<DoorDamage>() is { } damage ) damage.Breakable = value;
	}

	public static void SetLocked( GameObject target, bool value )
	{
		if ( !Networking.IsHost || !target.IsValid() ) return;
		if ( target.GetComponent<MovingDoor>() is { } door ) door.IsLocked = value;
	}

	public static void SetSpeed( GameObject target, float value )
	{
		if ( !Networking.IsHost || value <= 0 || !target.IsValid() ) return;
		if ( target.GetComponent<MovingDoor>() is { } door ) door.Speed = value;
	}

	public static void MoveToRotationDistance( GameObject target, float value )
	{
		if ( Networking.IsHost && target.IsValid() ) target.GetComponent<PropDoor>()?.SetRotationDistance( value, true );
	}

	public static void SetRotationDistance( GameObject target, float value )
	{
		if ( !Networking.IsHost || !target.IsValid() ) return;
		if ( target.GetComponent<PropDoor>() is { } prop ) prop.SetRotationDistance( value, false );
	}
}
