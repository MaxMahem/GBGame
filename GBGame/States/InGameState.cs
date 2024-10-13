using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using MonoGayme.States;
using System;
using MonoGayme.Components;
using GBGame.Entities;
using GBGame.Components;
using GBGame.Items;
using MonoGayme.Utilities;
using Microsoft.Xna.Framework.Input;
using GBGame.Entities.Enemies;
using MonoGayme.Controllers;
using MonoGayme.Components.Colliders;
using MonoGayme.Entities;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace GBGame.States;

public class InGameState(GameWindow windowData) : State(windowData)
{
    public required ControlBindings ControlBindings { private get; init; }
    public required InGameOptions InGameOptions {  private get; init; }
    public required WindowOptions WindowOptions { private get; init; }

    public required CameraGB CameraGB { get; init; }

    public required Player Player { get; init; }

    public required ControlCentre ControlCentre { private get; init; }

    public int ScoreMultiplier = 1;

    public bool SkipFrame = false;

    const int BatSpawnerHeight = -8;
    const int BatSpawnerWidth = 40;

    private readonly Color BackDrop = new(232, 240, 223);

    public EntityController Controller { get; } = new();
    private EntityController _enemyController = new();

    private Inventory _inventory = new();

    public required Pause Pause { private get; init; }

    private AnimatedSpriteSheet _sheet = null!;

    public Bomb Bomb = null!;

    private Vector2 _shakeOffset;
    private bool _shaking = false;
    private float _intensity = 0;
    private float _shakeDuration = 0.2f;
    private float _shakeMagnitude = 3f;

    private Shapes _shapes = null!;

    private bool _striking = false;
    private RectCollider _strikeCollider = new();

    private readonly Color _levelColour = new(176, 192, 160);
    private readonly Color _xpColour = new(96, 112, 80);

    private SpriteFont _font = windowData.Content.Load<SpriteFont>("Sprites/Fonts/File");
    private Texture2D _island = windowData.Content.Load<Texture2D>("Sprites/BackGround/Island");
    private Texture2D _starSprite = windowData.Content.Load<Texture2D>("Sprites/UI/LevelStar");

    private Timer _batTimer = new(5, true, false);
    private Timer _difficultyTimer = new(30, true, false);
    private readonly float _maxDecrease = 3f;
    private bool _canTry = false;
    private bool _canSpawn = false;

    private void ShakeCamera(GameTime time)
    { 
        if (_shaking)
        {
            _intensity -= (float)time.ElapsedGameTime.TotalSeconds / _shakeDuration;
            if (_intensity <= 0) 
            {
                _shaking = false;
                _shakeOffset = Vector2.Zero;

                if(Bomb.Exploded)
                    Bomb.Exploded = false;

                return;
            }

            float rot = Random.Shared.NextSingle() * MathF.Tau;
            _shakeOffset = new Vector2(MathF.Cos(rot), MathF.Sin(rot)) * _shakeMagnitude * _intensity;
        }
    }
    
    private void StartShake(float intensity, float magnitude)
    { 
        _shaking = true;

        _shakeMagnitude = magnitude;
        _intensity = intensity;
    }

    private void HandleInventoryInput()
    { 
        if (InputManager.IsKeyPressed(ControlBindings.KeyboardInventoryUp) || InputManager.IsGamePadPressed(ControlBindings.ControllerInventoryUp))
            _inventory.ActiveItemIndex--;

        if (InputManager.IsKeyPressed(ControlBindings.KeyboardInventoryDown) || InputManager.IsGamePadPressed(ControlBindings.ControllerInventoryDown))
            _inventory.ActiveItemIndex++;

        if (InputManager.IsKeyPressed(ControlBindings.KeyboardAction) || InputManager.IsGamePadPressed(ControlBindings.ControllerAction))
            _inventory.UseActive();
    }

    private void AddBat(Vector2 position)
    {
        if (Random.Shared.Next(0, 3) == 1)
        {
            ProjectileBat pbat = new ProjectileBat(WindowData, position); 
            pbat.LockOn(Player);

            _enemyController.AddEntity(pbat);
            
            return;
        }

        NormalBat bat = new NormalBat(WindowData, position); 
        bat.LockOn(Player);

        _enemyController.AddEntity(bat);

    }

    public required GridManager GridManager { private get; init; }

    public override void LoadContent()
    {
        _strikeCollider.Bounds = new Rectangle();

        Player.Position.Y = GridManager.GroundLine;
        Controller.AddEntity(Player);
        Controller.AddEntity(ControlCentre);

        _sheet = new AnimatedSpriteSheet(WindowData.Content.Load<Texture2D>("Sprites/SpriteSheets/Strike"), new Vector2(6, 1), 0.02f);
        _sheet.OnSheetFinished = () => _striking = false;
        
        _inventory.LoadContent(WindowData);
        _inventory.AddItem(new Sword(WindowData, _sheet, Player));

        Bomb = new Bomb(WindowData, Player);
        _inventory.AddItem(Bomb);

        Bomb.Sheet.OnSheetFinished = () => { 
            Bomb.CanPlace = true;
            Bomb.Exploded = true;

            StartShake(1, 3);
            _shakeOffset = Vector2.Zero;
        };

        _enemyController.OnEntityUpdate = (device, time, entity) => {
            RectCollider? rect = entity.Components.GetComponent<RectCollider>("PlayerStriker");
            if (rect is null) return;

            RectCollider? playerHitter = entity.Components.GetComponent<RectCollider>("PlayerHitter");
            if (playerHitter is not null)
            {
                if (Player.Components.GetComponent<RectCollider>()!.Collides(playerHitter))
                {
                    Player.ApplyKnockBack(playerHitter);
                    StartShake(1, 2);
                }
            }

            if (_striking && rect.Collides(_strikeCollider))

            {
                Vector2 distance = rect.GetCentre() - _strikeCollider.GetCentre();

                Vector2 dir = Vector2.Normalize(distance);
                entity.Velocity += 2 * dir;

                rect.Enabled = false;

                // Basically "iframes" except it uses actual time.
                Timer? immunityTimer = entity.Components.GetComponent<Timer>("ImmunityTimer");
                if (immunityTimer is not null)
                {
                    if (!immunityTimer.Enabled) immunityTimer.Start();
                }

                StartShake(0.6f, 2);

                Health? health = entity.Components.GetComponent<Health>();
                if (health is null) return;

                health.HealthPoints--;
                if (health.HealthPoints <= 0) 
                { 
                    Player.CalculateXP(entity);
                    _enemyController.QueueRemove(entity);
                }

                return;
            }

            if (Bomb.Exploded && rect.Collides(Bomb.KillRadius))
            {
                Player.CalculateXP(entity);
                _enemyController.QueueRemove(entity);

                return;
            }
        };

        _batTimer.OnTimeOut = () => {
            // 1/7 chance to be able to START trying to spawn 3 bats at once.
            if (_canTry && Random.Shared.Next(0, 7) == 1)
                _canSpawn = true;

            int minPosition = (int)(Player.Position.X - BatSpawnerWidth);
            int width = minPosition + (BatSpawnerWidth * 2);
            Vector2 batPosition = new Vector2(Random.Shared.Next(minPosition, width), BatSpawnerHeight);

            AddBat(batPosition);

            // 1/4 chance to spawn two bats with one on the opposite side
            if (Random.Shared.Next(0, 4) == 1)
            {
                Vector2 secondBatPosition = batPosition with { X = 2 * Player.Position.X - batPosition. X }; 
                AddBat(secondBatPosition);

                // 1/5 chance to spawn 3 bats
                if (_canSpawn && Random.Shared.Next(0, 5) == 1)
                {
                    AddBat(secondBatPosition with { X = 2 * Player.Position.X - secondBatPosition.X });
                }
            }
        };

        _difficultyTimer.OnTimeOut = () => {
            _batTimer.Time -= 0.5f;

            if (_batTimer.Time <= _maxDecrease)
            {
                _difficultyTimer.Stop();
                _canTry = true;
            }
        };

        _shapes = new Shapes(windowData.GraphicsDevice);
    }

    public override void Update(GameTime time)
    {
        if (SkipFrame)
        {
            SkipFrame = false;
            return;
        }

        if (Pause.Paused)
        {
            Pause.Update();
            return;
        }

        // idek
        ControlCentre.Colliding = ControlCentre.Collider.Collides(Player.Components.GetComponent<RectCollider>()!);
        ControlCentre.Update(time);

        // Update controllers.
        Controller.UpdateEntities(windowData.GraphicsDevice, time);
        _enemyController.UpdateEntities(windowData.GraphicsDevice, time);

        // Hardcoded ground checking (we don't need anything more complicated.)
        if (Player.Position.Y > GridManager.GroundLine) 
        {
            Player.Velocity.Y = 0;
            Player.Position.Y = GridManager.GroundLine;

            Player.Components.GetComponent<Jump>()?.ResetCount();
            Player.IsOnFloor = true;
        }

        if (Player.Position.Y < GridManager.GroundLine && Player.IsOnFloor) { Player.IsOnFloor = false; }

        // Keep the camera position between the game sizes, so the Player doesn't see outside the map.
        CameraGB.X = Math.Clamp(MathF.Floor(Player.Position.X - CameraGB.Offset + _shakeOffset.X), 0, WindowOptions.RenderBounds.Width - InGameOptions.FieldSize.Y);
        CameraGB.Y = _shakeOffset.Y;

        if (!SkipFrame){ HandleInventoryInput(); }

        if (!_sheet.Finished) _sheet.CycleAnimation(time);
        if (!Bomb.Sheet.Finished) Bomb.Sheet.CycleAnimation(time);

        ShakeCamera(time);

        _batTimer.Cycle(time);
        _difficultyTimer.Cycle(time);
    }
   
    public override void Draw(GameTime time, SpriteBatch batch)
    {
        WindowData.GraphicsDevice.Clear(BackDrop);

        batch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: CameraGB.Transform);
        { 
            // Draw the background
            batch.Draw(_island, CameraGB.ScreenToWorld(new Vector2(0, -10)), Color.White * 0.4f);

            foreach (GridManager.GroundTile tile in GridManager.GroundTiles) 
                batch.Draw(tile.Sprite, tile.Point.ToVector2(), Color.White);

            _enemyController.DrawEntities(batch, time);
            Controller.DrawEntities(batch, time); 

            if (!_sheet.Finished)
            {
                Vector2 strikePosition = new Vector2(
                    Player.FacingRight ? Player.Position.X + 4 : Player.Position.X - 12,
                    Player.Position.Y - 4
                );

                _strikeCollider.Bounds.X = (int)strikePosition.X + 2;
                _strikeCollider.Bounds.Y = (int)strikePosition.Y;
                _strikeCollider.Bounds.Width = 4;
                _strikeCollider.Bounds.Height = 8;

                _striking = true;

                _sheet.Draw(batch, strikePosition, !Player.FacingRight);
            }

            if(!Bomb.Sheet.Finished) Bomb.Draw(batch);

            _inventory.Draw(batch, CameraGB);

            // Draw the player XP
            batch.DrawString(_font, $"{Player.XP} - {Player.ToLevelUp}", CameraGB.ScreenToWorld(new Vector2(1, 25)), _xpColour);

            // Draw the player level
            batch.Draw(_starSprite, CameraGB.ScreenToWorld(new Vector2(0, 34)), Color.White);
            batch.DrawString(_font, $"{Player.Level}", CameraGB.ScreenToWorld(new Vector2(10, 33)), _levelColour);

            ControlCentre.Draw(batch, time);

            if (Pause.Paused) Pause.Draw(batch, CameraGB);
        } 
        batch.End();
    }
}

public record class GlobalSpawnRate(TimeSpan InitialRate, TimeSpan MinRate, TimeSpan TickDelta, TimeSpan DeltaRate) : IObservable<TimeSpan>
{
    public IDisposable Subscribe(IObserver<TimeSpan> observer) => Observable.Interval(DeltaRate)
        .Scan(InitialRate, (rate, _) => rate - TickDelta)
        .TakeWhile(rate => rate >= MinRate)
        .Publish().RefCount().Subscribe(observer);
}

abstract record class Spawner<T>(IObservable<TimeSpan> SpawnRate, Point Size) : IObservable<IEnumerable<T>>
{
    public abstract IDisposable Subscribe(IObserver<IEnumerable<T>> observer);

    protected abstract IEnumerable<T> Spawn();
}

/*public record BatSpawner(IObservable<TimeSpan> SpawnRate, Point Size, double PrimaryOdds, double SecondaryOdds, double TeritaryOdds)
    : Spawner<Point>(SpawnRate, Size)
{
    public override IDisposable Subscribe(IObserver<IEnumerable<Point>> observer)
        => SpawnRate.Select(Observable.Interval)
                    .Select(_ => Spawn()).Subscribe(observer);

    protected override IEnumerable<Point> Spawn()
    {
        if (Random.Shared.Next() > PrimaryOdds) {

            // Calculate positions based on player and bat spawner parameters
            int minPosition = State.playerPosition.X - Size.X;
            int width = minPosition + (Size.X * 2);

            Point batPosition = new(Random.Shared.Next(minPosition, width), Size.Y);

            yield return batPosition;

            if (Random.Shared.Next() > SecondaryOdds) {
                Point secondBatPosition = batPosition with { X = 2 * State.PlayerPosition.X - batPosition.X };
                yield return new(secondBatPosition);

                if (Random.Shared.Next() > TeritaryOdds) {
                    Point thirdBatPosition = batPosition with { X = 2 * State.PlayerPosition.X - secondBatPosition.X };
                    yield return new(thirdBatPosition);
                }
            }
        }
    }
}
*/