using Microsoft.Xna.Framework;

namespace GBGame;

public class WindowOptions
{
    public Rectangle RenderBounds { get; set; } = new(0, 0, 160, 144);

    public Point VisualWindowSizeMultiplier { get; set; } = new(3, 3);

    public Point ScaledWindowSize => RenderBounds.Size * VisualWindowSizeMultiplier;
}
