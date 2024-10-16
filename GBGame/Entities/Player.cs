using GBGame.Components;
using GBGame.States;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGayme.Components;
using MonoGayme.Components.Colliders;
using MonoGayme.Entities;
using MonoGayme.Utilities;
using System;
using System.Collections.Generic;

namespace GBGame.Entities;

public class PlayerOptions
{
    public int ZIndex { get; set; } = 1;

    public float TerminalVelocity { get; set; } = 2f;
    public float Acceleration { get; set; } = 0.5f;
    public float JumpVelocity { get; set; } = 6f;

    public int BaseXP { get; set; } = 14;
    public int BaseXPMultiplier { get; set; } = 1;
}

public class Player(Game windowData, ControlBindings controlBindings, CameraGB camera, PlayerOptions playerOptions) 
    : Entity(windowData, playerOptions.ZIndex)
{
    public int SkillPoints { get; set; } = 0;
    public int ToLevelUp { get; set; } = playerOptions.BaseXP;

    public int XPMultiplier { get; set; } = playerOptions.BaseXPMultiplier;

    private Texture2D _sprite = null!;
    private Vector2 _origin = Vector2.Zero;

    public bool IsOnFloor = false;
    public bool FacingRight = true;

    public RectCollider Collider = null!;
    private Timer _immunityTimer = null!;

    public Health Health = null!;

    private List<SpriteSheet> _health = [];

    private Jump _jump = null!;

    public int Level = 1;
    public int XP = 0;

    private Texture2D _healthSheet = null!;
    private int _basePosition = 1;

    public void ApplyKnockBack(RectCollider other)
    {
        Vector2 dir = Vector2.Normalize(Collider.GetCentre() - other.GetCentre());
        Velocity += 5 * dir;

        _health[Health.HealthPoints - 1].DecrementY();

        Health.HealthPoints--;
        if (Health.HealthPoints <= 0)
        {
            // :trollface:
            WindowData.Exit();
        }

        Collider.Enabled = false;
        _immunityTimer.Start();
    }

    public void AddHealth()
    { 
        Health.HealthPoints++;

        SpriteSheet sheet = new SpriteSheet(_healthSheet, new Vector2(1, 2), new Vector2(_basePosition, 20));
        sheet.IncrementY();

        _health.Add(sheet);
        if (Health.HealthPoints <= Health.OriginalHealthPoints)
        {
            sheet.DecrementY();
            foreach (SpriteSheet health in _health)
            {
                if (health.Y == 0)
                {
                    health.IncrementY();
                    break;
                }
            }
        }

        _basePosition += 17;
    }

    public void CalculateXP(Entity entity)
    {
        XPDropper? dropper = entity.Components.GetComponent<XPDropper>();
        if (dropper is null) { return; }

        this.XP += dropper.XP * XPMultiplier;
        if (this.XP >= ToLevelUp) {
            this.Level++;

            this.XP = int.Min(0, this.XP - ToLevelUp);

            ToLevelUp *= 2;
            SkillPoints++;
        }
    }

    public override void LoadContent()
    {
        Position.X = 40;
        
        _sprite = WindowData.Content.Load<Texture2D>("Sprites/Ground/Ground_4");
        _origin = new Vector2(_sprite.Width / 2, _sprite.Height / 2);

        Components.AddComponent(new RectCollider());
        Collider = Components.GetComponent<RectCollider>()!;

        Components.AddComponent(new Jump(1));
        _jump = Components.GetComponent<Jump>()!;

        Components.AddComponent(new Timer(1, false, true, "ImmunityTimer"));
        _immunityTimer = Components.GetComponent<Timer>()!;
        _immunityTimer.OnTimeOut = () => Collider.Enabled = true;

        Components.AddComponent(new Health(3));
        Health = Components.GetComponent<Health>()!;

        _healthSheet = WindowData.Content.Load<Texture2D>("Sprites/UI/Health");
        for (int i = 0; i < Health.HealthPoints; i++)
        { 
            SpriteSheet sheet = new SpriteSheet(_healthSheet, new Vector2(1, 2), new Vector2(_basePosition, 20));
            sheet.IncrementY();

            _health.Add(sheet);

            _basePosition += 17;
        }
    }

    public override void Update(GameTime time)
    {
        _immunityTimer.Cycle(time);

        if (InputManager.IsKeyDown(controlBindings.KeyboardLeft) || InputManager.IsGamePadDown(controlBindings.ControllerLeft))
        {
            Velocity.X = MathUtility.MoveTowards(Velocity.X, -playerOptions.TerminalVelocity, playerOptions.Acceleration);
            FacingRight = false;
        } 
        else if (InputManager.IsKeyDown(controlBindings.KeyboardRight) || InputManager.IsGamePadDown(controlBindings.ControllerRight))
        {
            Velocity.X = MathUtility.MoveTowards(Velocity.X, playerOptions.TerminalVelocity, playerOptions.Acceleration);
            FacingRight = true;
        }
        else 
        {
            Velocity.X = MathUtility.MoveTowards(Velocity.X, 0, playerOptions.Acceleration);
        }

        if (IsOnFloor && (InputManager.IsKeyPressed(controlBindings.KeyboardJump) || InputManager.IsGamePadPressed(controlBindings.ControllerJump)))
        {
            Velocity.Y = -playerOptions.JumpVelocity;
            IsOnFloor = false;
        }

        if (!IsOnFloor && _jump.Count > 0 && (InputManager.IsKeyPressed(controlBindings.KeyboardJump) || InputManager.IsGamePadPressed(controlBindings.ControllerJump)))
        {
            Velocity.Y = -playerOptions.JumpVelocity;
            _jump.Count--;
        }

        if (!IsOnFloor)
        {
            Velocity.Y = MathUtility.MoveTowards(Velocity.Y, playerOptions.TerminalVelocity, 0.8f);
        }

        Position += Velocity;
        Position.X = float.Round(Position.X);

        Collider.Bounds = new Rectangle((int)Position.X - 2, (int)Position.Y - 2, 4, 4);
    }

    public override void Draw(SpriteBatch batch, GameTime time)
    {
        batch.Draw(_sprite, Position, null, Color.White, 0, _origin, 1, SpriteEffects.None, 0);

        foreach (SpriteSheet health in _health)
        {
            health.Draw(batch, camera);
        }
    }
}
