using Sandbox.UI;

namespace Source1;

internal static class ScreenOverlay
{
	internal static Panel AddOverlay( this ScreenPanel screen )
	{
		var panel = new Panel( screen.GetPanel() );
		panel.Style.Position = PositionMode.Absolute;
		panel.Style.Width = panel.Style.Height = Length.Percent( 100 );
		panel.Style.PointerEvents = PointerEvents.None;
		return panel;
	}
}
