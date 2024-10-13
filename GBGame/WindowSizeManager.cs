using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGayme.Utilities;

namespace GBGame;

public class WindowSizeManager(GraphicsDeviceManager graphicsDeviceManager)
{ 
    Point sizeBeforeResize;
    bool isFullScreen = false;

    public void ToggleFullScreen()
    {
        if (this.isFullScreen) { graphicsDeviceManager.SetWindowSize(this.sizeBeforeResize.X, this.sizeBeforeResize.Y); }
        else {
            this.sizeBeforeResize = graphicsDeviceManager.GraphicsDevice.Viewport.Bounds.Size;

            graphicsDeviceManager.SetWindowSize(
                GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width,
                GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height
            );
        }

        this.isFullScreen = !this.isFullScreen;
        graphicsDeviceManager.ToggleFullScreen();
    }

    public void SetWindowSize(Point size) => graphicsDeviceManager.SetWindowSize(size.X, size.Y);
}
