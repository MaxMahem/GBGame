using Autofac;
using Autofac.Features.Indexed;
using Autofac.Features.ResolveAnything;
using GBGame.Skills;
using GBGame.States;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGayme.States;
using MonoGayme.Utilities;
using System.Reflection;

namespace GBGame;

public class GameWindow : Game
{
    const string CONTENT_DIRECTORY_ROOT = "Content";

    readonly Renderer renderer;
    readonly SpriteBatch spriteBatch = null!;

    public GBStateContext State { get; }

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

        this.gameScope = RegisterTypes(builder).Build().BeginLifetimeScope();

        this.windowOptions = this.gameScope.Resolve<WindowOptions>();
        this.windowSizeManager = this.gameScope.Resolve<WindowSizeManager>();
        this.windowSizeManager.SetWindowSize(this.windowOptions.ScaledWindowSize);

        this.renderer = new Renderer(this.windowOptions.RenderBounds.Size.ToVector2(), GraphicsDevice);
        this.spriteBatch = new SpriteBatch(GraphicsDevice);
        
        this.controlBindings = this.gameScope.Resolve<ControlBindings>();

        this.State = this.gameScope.Resolve<GBStateContext>();
    }

    static ContainerBuilder RegisterTypes(ContainerBuilder builder)
    {
        builder.RegisterType<GraphicsDeviceManager>().SingleInstance();

        builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
               .Except<GameWindow>().SingleInstance();

        builder.RegisterType<MultiplyXP>().As<Skill>().SingleInstance();
        builder.RegisterType<DoubleJump>().As<Skill>().SingleInstance();
        builder.RegisterType<MoreHP>().As<Skill>().SingleInstance();

        builder.RegisterType<InGameState>().As<State>()
               .Keyed<State>(nameof(InGameState)).SingleInstance();
        builder.RegisterType<ControlCentreState>().As<State>()
               .Keyed<State>(nameof(ControlCentreState)).SingleInstance();

        return builder;
    }

    readonly ILifetimeScope gameScope;

    protected override void LoadContent() => this.State.SwitchState(nameof(InGameState));

    protected override void Update(GameTime gameTime)
    {
        if (InputManager.IsKeyPressed(this.controlBindings.FullScreen)) { this.windowSizeManager.ToggleFullScreen(); }

        MousePosition = this.renderer.GetVirtualMousePosition();
        this.State.Update(gameTime);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        InputManager.GetState();
        
        this.renderer.SetRenderer();
        this.State.Draw(gameTime, this.spriteBatch);

        this.renderer.DrawRenderer(this.spriteBatch);

        base.Draw(gameTime);
    }
}

public class GBStateContext(IIndex<string, State> states) : StateContext
{
    public void SwitchState(string stateName) => SwitchState(states[stateName]);
}