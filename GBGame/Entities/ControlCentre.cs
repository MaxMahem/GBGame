using GBGame.States;
using Microsoft.VisualBasic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGayme.Components.Colliders;
using MonoGayme.Entities;
using MonoGayme.Utilities;

namespace GBGame.Entities;

public class ControlCentre(GameWindow windowData, ControlBindings controlBindings) : Entity(windowData, -1)
{
    public required GridManager GridManager { private get; init; }

    readonly Texture2D controlCenterSprite = windowData.Content.Load<Texture2D>("Sprites/Objects/CommandCentre");
    readonly Texture2D questionSprite = windowData.Content.Load<Texture2D>("Sprites/Objects/CommandCentre_Interact");
    private float _questionOpacity = 0;

    public RectCollider Collider { get; } = new("Centre");

    public bool Colliding { get; set; } = false;

    public required WindowOptions GameWindowOptions { private get; init; }

    public override void LoadContent()
    {
        Position = new Vector2(0, GridManager.GroundLine - 12);
        Collider.Bounds = new Rectangle( (int)Position.X, (int)Position.Y + 8, 16, 8 );
    }

    public override void Update(GameTime time)
    {
        if ((InputManager.IsGamePadPressed(controlBindings.ControllerAction) 
          || InputManager.IsKeyPressed(controlBindings.KeyboardAction)) && Colliding)
        {
            windowData.State.SwitchState(nameof(ControlCentreState));
        }        
    }
  
    public override void Draw(SpriteBatch batch, GameTime time)
    {
        batch.Draw(this.controlCenterSprite, Position, Color.White);
        batch.Draw(this.questionSprite, Position with { Y = Position.Y - 10 }, Color.White * _questionOpacity);
    }
}
