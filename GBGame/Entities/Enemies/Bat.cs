using System;
using GBGame.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGayme.Components;
using MonoGayme.Components.Colliders;
using MonoGayme.Entities;
using MonoGayme.Utilities;

namespace GBGame.Entities.Enemies;

public class Bat(Game windowData, Vector2 pos, int zIndex = 0) : Entity(windowData, zIndex){
    private Texture2D _sprite = null!;
    
    private bool _locked = false;
    private Entity? _lockedEntity;

    private float _speed = 0.6f;

    private RectCollider _rectCollider;
    private Timer _immunityTimer;

    public void Lock(Entity entity) {
        _lockedEntity = entity;
        _locked = true;
    }

    public override void LoadContent()
    {
        _sprite = WindowData.Content.Load<Texture2D>("Sprites/Ground/Ground_4");
        Position = pos;

        Components.AddComponent(new RectCollider());
        Components.AddComponent(new Timer(1, true, false, "ImmunityTimer"));
        Components.AddComponent(new Health(2));

        _rectCollider = Components.GetComponent<RectCollider>()!;

        _immunityTimer = Components.GetComponent<Timer>("ImmunityTimer")!;
        _immunityTimer.OnTimeOut = () => { _rectCollider.Enabled = true; };
    }

    public override void Update(GameTime time)
    {
        if (_locked) 
        {
            if (_lockedEntity is null) 
            {
                Console.Error.WriteLine("How does this even happen?");
                return;
            }
            
            Vector2 dir = _lockedEntity.Position with { Y = _lockedEntity.Position.Y - 4 } - Position;
            dir.Normalize();

            Vector2 target = dir * _speed;
            Velocity = MathUtility.MoveTowards(Velocity, target, 0.05f);
        }

        Position += Velocity;
        _rectCollider.Bounds = new Rectangle((int)Position.X, (int)Position.Y, 8, 8);

        _immunityTimer.Cycle(time);
    }

    public override void Draw(SpriteBatch batch, GameTime time)
    {
        batch.Draw(_sprite, Position, Color.White);
    }
}
