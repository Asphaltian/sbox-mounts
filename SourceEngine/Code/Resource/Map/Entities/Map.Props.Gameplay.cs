namespace Source1;

partial class MapLoader
{
	private sealed partial class EntityBuilder
	{
		private bool CanBuildGameplayProp( MapProp prop, Model model, string path )
		{
			if ( !prop.Gameplay )
				return false;
			if ( model.Data.Flammable || model.Data.Explosive )
			{
				Log.Trace( $"Prop '{prop.Name}' model '{path}' still has unsafe cached fire/explosion metadata; reload the mount before using breakable gameplay." );
				return false;
			}

			if ( !prop.Visible || prop.Solid == 0 || model.Physics is { Parts.Count: > 1 } )
			{
				Log.Trace( $"Prop '{prop.Name}' model '{path}' requires unsupported visibility, collision or multi-body settings." );
				return false;
			}

			return true;
		}

		private static void AddGameplayProp( MapProp prop, GameObject instance, Model model, string materialGroup )
		{
			var gameplay = instance.AddComponent<Prop>( false );
			gameplay.IsStatic = prop.Physics == PropPhysics.Static;
			gameplay.StartAsleep = prop.StartAsleep;
			gameplay.Model = model;
			gameplay.Tint = prop.Tint;
			gameplay.MaterialGroup = materialGroup;
			if ( !prop.MotionEnabled && !gameplay.IsStatic )
				instance.GetOrAddComponent<Rigidbody>().MotionEnabled = false;
			gameplay.Enabled = true;
			PropDamage.Attach( gameplay );
			if ( !gameplay.IsStatic ) PropAirDrag.Attach( instance, model );
		}
	}
}
