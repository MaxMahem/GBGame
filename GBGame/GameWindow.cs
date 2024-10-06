using System;
using System.Threading;
using Autofac;
using Autofac.Features.ResolveAnything;
using GBGame.Components;
using GBGame.Entities;
using GBGame.Skills;
using GBGame.States;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGayme.States;
using MonoGayme.Utilities;

namespace GBGame;

public class GameWindow : Game
{
    const string CONTENT_DIRECTORY_ROOT = "Content";

    readonly Renderer renderer;
    readonly SpriteBatch spriteBatch = null!;

    readonly StateContext context;

    public InGame InGame { get; }
    public ControlCentre ControlCentre { get; }

    readonly WindowOptions windowOptions;
    readonly WindowSizeManager windowSizeManager;

    readonly ControlBindings controlBindings;

    public Vector2 MousePosition { get; private set; }

    public GameWindow()
    {
        Content.RootDirectory = CONTENT_DIRECTORY_ROOT;

        IsMouseVisible = true;
        Window.AllowUserResizing = true;

        var builder = new ContainerBuilder();
        builder.RegisterInstance(this).AsSelf().As<Game>();
        builder.RegisterSource(new AnyConcreteTypeNotAlreadyRegisteredSource());

        this.gameScope = builder.Build().BeginLifetimeScope();

        this.windowOptions = this.gameScope.Resolve<WindowOptions>();
        this.windowSizeManager = this.gameScope.Resolve<WindowSizeManager>();
        this.windowSizeManager.SetWindowSize(this.windowOptions.ScaledWindowSize);

        this.renderer = new Renderer(this.windowOptions.RenderBounds.Size.ToVector2(), GraphicsDevice);
        this.spriteBatch = new SpriteBatch(GraphicsDevice);
        
        this.controlBindings = this.gameScope.Resolve<ControlBindings>();

        this.context = this.gameScope.Resolve<StateContext>();
        this.InGame = this.gameScope.Resolve<InGame>();
        this.ControlCentre = this.gameScope.Resolve<ControlCentre>();
    }

    readonly ILifetimeScope gameScope;

    protected override void LoadContent() => this.context.SwitchState(this.InGame);

    protected override void Update(GameTime gameTime)
    {
        if (InputManager.IsKeyPressed(this.controlBindings.FullScreen)) { this.windowSizeManager.ToggleFullScreen(); }

        MousePosition = this.renderer.GetVirtualMousePosition();
        this.context.Update(gameTime);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        InputManager.GetState();
        
        this.renderer.SetRenderer();
        this.context.Draw(gameTime, this.spriteBatch);

        this.renderer.DrawRenderer(this.spriteBatch);

        base.Draw(gameTime);
    }
}

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

public class WindowOptions
{
    public Rectangle RenderBounds { get; set; } = new(0, 0, 160, 144);

    public Point VisualWindowSizeMultiplier { get; set; } = new(3, 3);

    public Point ScaledWindowSize => RenderBounds.Size * VisualWindowSizeMultiplier;
}

public class InGameOptions
{
    public int XPMultiplier { get; set; } = 1;

    public Point FieldSize { get; set; } = new(288, 144);

    public int GroundTileRows { get; set; } = 2;

    public Point TileSize { get; set; } = new(8, 8);
}