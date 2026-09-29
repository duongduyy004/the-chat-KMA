using UnityEngine;
using UnityEngine.UI;

namespace KMA.UI.Kit
{
    public enum ControlState { Rest, Hint, Pressed, Disabled }

    /// Fill and border colours for touch controls drawn as a Surface plate inside an Accent
    /// border (Sprint tap zones, the Volleyball joystick).
    public static class KitControlState
    {
        public static Color Fill(ControlState state) => state switch
        {
            ControlState.Hint => MinigameUiTheme.WithAlpha(MinigameUiTheme.Surface, MinigameUiTheme.SurfaceControlActive),
            ControlState.Pressed => MinigameUiTheme.Lighten(
                MinigameUiTheme.WithAlpha(MinigameUiTheme.Surface, MinigameUiTheme.SurfaceControlActive),
                MinigameUiTheme.PressLighten),
            ControlState.Disabled => MinigameUiTheme.WithAlpha(MinigameUiTheme.Surface, MinigameUiTheme.SurfaceDisabled),
            _ => MinigameUiTheme.WithAlpha(MinigameUiTheme.Surface, MinigameUiTheme.SurfaceControl)
        };

        public static Color Border(ControlState state) => state switch
        {
            ControlState.Hint or ControlState.Pressed =>
                MinigameUiTheme.WithAlpha(MinigameUiTheme.Accent, MinigameUiTheme.BorderHint),
            ControlState.Disabled => MinigameUiTheme.WithAlpha(MinigameUiTheme.Accent, MinigameUiTheme.BorderDisabled),
            _ => MinigameUiTheme.WithAlpha(MinigameUiTheme.Accent, MinigameUiTheme.BorderRest)
        };

        public static void Apply(Image fill, Image border, ControlState state)
        {
            if (fill != null)
                fill.color = Fill(state);
            if (border != null)
                border.color = Border(state);
        }
    }
}
