namespace Source1;

[Hide]
public sealed class AreaPortalWindow : Component
{
	[Property] public ModelRenderer Renderer { get; set; }
	[Property] public ModelRenderer Background { get; set; }
	[Property] public float FadeStart { get; set; }
	[Property] public float FadeEnd { get; set; }
	[Property] public float MinimumOpacity { get; set; }

	protected override void OnUpdate()
	{
		if ( Scene.Camera is not { } camera || !Renderer.IsValid() || Renderer.Model is null ) return;
		var eye = Renderer.WorldTransform.PointToLocal( camera.WorldPosition );
		var distance = Renderer.Model.Bounds.ClosestPoint( eye ).Distance( eye ) * camera.FieldOfView / Preferences.FieldOfView;
		var fade = FadeStart == FadeEnd ? (distance >= FadeEnd ? 1f : 0f) : float.Clamp( (distance - FadeStart) / (FadeEnd - FadeStart), 0, 1 );
		var opacity = float.Lerp( MinimumOpacity, 1, fade );
		Renderer.Enabled = opacity > 0;
		Renderer.Tint = Renderer.Tint.WithAlpha( opacity );
		if ( Background.IsValid() ) Background.Enabled = Renderer.Enabled;
	}
}
