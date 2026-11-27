using System;
using Sandbox.UI;

namespace Source1;

public interface ISmokeCloud
{
	float FogAlpha( Vector3 eye );
}

/// <summary>DrawSmokeFogOverlay: the screen greyed to 0.3 by the summed fog of every cloud the eye is inside, under the HUD.</summary>
[Title( "Source Smoke Fog" )]
[Category( "Source 1" )]
[Alias( "Source1.Css.SmokeFog", "Source1.CssSmokeFog", "CssSmokeFog" )]
public sealed class SmokeFog : PanelComponent
{
	const float Grey = 0.3f;

	public static void Ensure( Scene scene )
	{
		if ( scene.GetAllComponents<SmokeFog>().Any() ) return;
		var go = new GameObject( true, "Smoke fog overlay" );
		go.NetworkMode = NetworkMode.Never;
		go.Flags |= GameObjectFlags.NotSaved | GameObjectFlags.NotNetworked;
		go.AddComponent<ScreenPanel>().ZIndex = 0;
		go.AddComponent<SmokeFog>();
	}

	protected override void OnUpdate()
	{
		var camera = Scene.Camera;
		var visible = Panel is not null && camera.IsValid();
		var anyClouds = false;
		double alpha = 0;
		foreach ( var cloud in Scene.GetAllComponents<ISmokeCloud>() )
		{
			anyClouds = true;
			if ( visible ) alpha += cloud.FogAlpha( camera.WorldPosition );
		}
		if ( !anyClouds )
		{
			GameObject.Destroy();
			return;
		}
		if ( Panel is null ) return;
		Panel.Style.Position = Sandbox.UI.PositionMode.Absolute;
		Panel.Style.Width = Sandbox.UI.Length.Percent( 100 );
		Panel.Style.Height = Sandbox.UI.Length.Percent( 100 );
		Panel.Style.BackgroundColor = new Color( Grey, Grey, Grey );
		Panel.Style.Opacity = Math.Clamp( (float)alpha, 0, 1 );
	}
}
