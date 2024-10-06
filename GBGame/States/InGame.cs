using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using MonoGayme.States;
using System.Collections.Generic;
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
using System.Linq;
using System.Collections.Immutable;
using System.IO;

namespace GBGame.States;

public class InGame(GameWindow windowData) : State(windowData)
{
    public required ControlBindings ControlBindings { private get; init; }
    public required InGameOptions InGameOptions {  private get; init; }
    public required WindowOptions WindowOptions { private get; init; }

    public int ScoreMultiplier = 1;

    public bool SkipFrame = false;

    const int BatSpawnerHeight = -8;
    const int BatSpawnerWidth = 40;

    ImmutableArray<GridManager.GroundTile> groundTiles = [];

    private readonly Color BackDrop = new Color(232, 240, 223);

    private Camera2D _camera = new(Vector2.Zero);
    private float _cameraOffset = 40;

    public EntityController Controller = new();
    private EntityController _enemyController = new();

    private Inventory _inventory = new Inventory();

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

    private Player _player = null!;
    private RectCollider _playerCollider = null!;
    private Jump _playerJump = null!;

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

    private RectCollider _centreCollider = null!;

    private readonly int _baseXP = 14;
    private int _toLevelUp;

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
            pbat.LockOn(_player);

            _enemyController.AddEntity(pbat);
            
            return;
        }

        NormalBat bat = new NormalBat(WindowData, position); 
        bat.LockOn(_player);

        _enemyController.AddEntity(bat);

    }

    private void CalculateXP(Entity entity)
    { 
        XPDropper? dropper = entity.Components.GetComponent<XPDropper>();
        if (dropper is null) return;

        GameWindow window = (GameWindow)WindowData;
        _player.XP += dropper.XP * InGameOptions.XPMultiplier;
        if (_player.XP >= _toLevelUp)
        {
            _player.Level++;

            if (_player.XP - _toLevelUp > 0)
                _player.XP -= _toLevelUp;
            else
                _player.XP = 0;

            // Double XP every level
            _toLevelUp *= 2;

            window.ControlCentre.SkillPoints++;
            window.ControlCentre.ChooseSkills();
        }
    }

    public required GridManager GridManager { private get; init; }

    public override void LoadContent()
    {
        _toLevelUp = _baseXP;

        _strikeCollider.Bounds = new Rectangle();

        GameWindow window = (GameWindow)WindowData;

        groundTiles = GridManager.GenerateTiles();

        _player = new Player(WindowData, ControlBindings, _camera);
        this._player.Position.Y = GridManager.GroundLine;
        Controller.AddEntity(_player);

        window.ControlCentre.LoadContent();
        _centreCollider = window.ControlCentre.Components.GetComponent<RectCollider>()!;

        _playerCollider = _player.Components.GetComponent<RectCollider>()!;
        _playerJump = _player.Components.GetComponent<Jump>()!;

        _sheet = new AnimatedSpriteSheet(WindowData.Content.Load<Texture2D>("Sprites/SpriteSheets/Strike"), new Vector2(6, 1), 0.02f);
        _sheet.OnSheetFinished = () => _striking = false;
        
        _inventory.LoadContent(WindowData);
        _inventory.AddItem(new Sword(WindowData, _sheet, _player));

        Bomb = new Bomb(WindowData, _player);
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
                if (_playerCollider.Collides(playerHitter))
                {
                    _player.ApplyKnockBack(playerHitter);
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
                    CalculateXP(entity);
                    _enemyController.QueueRemove(entity);
                }

                return;
            }

            if (Bomb.Exploded && rect.Collides(Bomb.KillRadius))
            {
                CalculateXP(entity);
                _enemyController.QueueRemove(entity);

                return;
            }
        };

        _batTimer.OnTimeOut = () => {
            // 1/7 chance to be able to START trying to spawn 3 bats at once.
            if (_canTry && Random.Shared.Next(0, 7) == 1)
                _canSpawn = true;

            int minPosition = (int)(_player.Position.X - BatSpawnerWidth);
            int width = minPosition + (BatSpawnerWidth * 2);
            Vector2 batPosition = new Vector2(Random.Shared.Next(minPosition, width), BatSpawnerHeight);

            AddBat(batPosition);

            // 1/4 chance to spawn two bats with one on the opposite side
            if (Random.Shared.Next(0, 4) == 1)
            {
                Vector2 secondBatPosition = batPosition with { X = 2 * _player.Position.X - batPosition. X }; 
                AddBat(secondBatPosition);

                // 1/5 chance to spawn 3 bats
                if (_canSpawn && Random.Shared.Next(0, 5) == 1)
                {
                    AddBat(secondBatPosition with { X = 2 * _player.Position.X - secondBatPosition.X });
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

        _shapes = new Shapes(window.GraphicsDevice);
    }

    public override void Update(GameTime time)
    {
        if (SkipFrame)
        {
            SkipFrame = false;
            return;
        }

        if (!windowData.ControlCentre.Interacting && (InputManager.IsGamePadPressed(Buttons.Start) || InputManager.IsKeyPressed(Keys.Escape)))
            Pause.Paused = !Pause.Paused;

        if (Pause.Paused)
        {
            Pause.Update();
            return;
        }

        // idek
        windowData.ControlCentre.CanInteract = _centreCollider.Collides(_playerCollider);
        windowData.ControlCentre.Update(time);

        if (windowData.ControlCentre.Interacting) { return; }

        // Update controllers.
        Controller.UpdateEntities(WindowData.GraphicsDevice, time);
        _enemyController.UpdateEntities(WindowData.GraphicsDevice, time);

        // Hardcoded ground checking (we don't need anything more complicated.)
        if (_player.Position.Y > GridManager.GroundLine) 
        {
            _player.Velocity.Y = 0;
            _player.Position.Y = GridManager.GroundLine;

            _playerJump.Count = _playerJump.BaseCount;

            _player.IsOnFloor = true;
        }

        if (_player.Position.Y < GridManager.GroundLine && _player.IsOnFloor)
        {
            _player.IsOnFloor = false;
        }

        // Keep the camera position between the game sizes, so the _player doesn't see outside the map.
        _camera.X = Math.Clamp(MathF.Floor(_player.Position.X - _cameraOffset + _shakeOffset.X), 0, WindowOptions.RenderBounds.Width - InGameOptions.FieldSize.Y);
        _camera.Y = _shakeOffset.Y;

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

        batch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: _camera.Transform);
        { 
            // Draw the background
            batch.Draw(_island, _camera.ScreenToWorld(new Vector2(0, -10)), Color.White * 0.4f);

            foreach (GridManager.GroundTile tile in this.groundTiles) 
                batch.Draw(tile.Sprite, tile.Point.ToVector2(), Color.White);

            _enemyController.DrawEntities(batch, time);
            Controller.DrawEntities(batch, time); 

            if (!_sheet.Finished)
            {
                Vector2 strikePosition = new Vector2(
                    _player.FacingRight ? _player.Position.X + 4 : _player.Position.X - 12,
                    _player.Position.Y - 4
                );

                _strikeCollider.Bounds.X = (int)strikePosition.X + 2;
                _strikeCollider.Bounds.Y = (int)strikePosition.Y;
                _strikeCollider.Bounds.Width = 4;
                _strikeCollider.Bounds.Height = 8;

                _striking = true;

                _sheet.Draw(batch, strikePosition, !_player.FacingRight);
            }

            if(!Bomb.Sheet.Finished) Bomb.Draw(batch);

            _inventory.Draw(batch, _camera);

            // Draw the player XP
            batch.DrawString(_font, $"{_player.XP} - {_toLevelUp}", _camera.ScreenToWorld(new Vector2(1, 25)), _xpColour);

            // Draw the player level
            batch.Draw(_starSprite, _camera.ScreenToWorld(new Vector2(0, 34)), Color.White);
            batch.DrawString(_font, $"{_player.Level}", _camera.ScreenToWorld(new Vector2(10, 33)), _levelColour);

            windowData.ControlCentre.Draw(batch, time);

            if (Pause.Paused) Pause.Draw(batch, _camera);
        } 
        batch.End();
    }
}

public class GridManager(GameWindow game, InGameOptions inGameOptions)
{
    public record struct GroundTile(Texture2D Sprite, Point Point);

    public int GroundLine { get; } = (int) (inGameOptions.FieldSize.Y - (inGameOptions.TileSize.Y * 2.5));

    readonly ImmutableArray<Texture2D> groundTextures = game.LoadDirectory<Texture2D>("Sprites/Ground/").ToImmutableArray();
    readonly ImmutableArray<Texture2D> grassTextures = game.LoadDirectory<Texture2D>("Sprites/Grass/").ToImmutableArray();

    public ImmutableArray<GroundTile> GenerateTiles()
    {
        if (!this.groundTextures.ValidateSize(inGameOptions.TileSize) || !this.grassTextures.ValidateSize(inGameOptions.TileSize)) 
        { 
            throw new InvalidOperationException("Invalid Tile Size."); 
        }

        var (xCount, yCount) = int.DivRem(inGameOptions.FieldSize.X, inGameOptions.TileSize.X) is { Quotient: int xc, Remainder: 0 }
                            && int.DivRem(inGameOptions.FieldSize.Y, inGameOptions.TileSize.Y) is { Quotient: int yc, Remainder: 0 }
                            && yc >= inGameOptions.GroundTileRows ? (xc, int.Min(yc, inGameOptions.GroundTileRows)) 
                            : throw new InvalidOperationException($"Tiles bounds must divide evenly into game field bounds.");

        Point groundOffset = new(0, inGameOptions.FieldSize.Y - (inGameOptions.TileSize.Y * inGameOptions.GroundTileRows));

        var surfaceTiles = from iteration in Enumerable.Range(0, xCount)
                           let groundTexture = Random.Shared.Pick(this.groundTextures.AsSpan()[..^1])
                           let groundX = groundOffset.X + (iteration * inGameOptions.TileSize.X)
                           select new GroundTile(groundTexture, new(groundX, groundOffset.Y));

        var undergroundTiles = from iterationX in Enumerable.Range(0, xCount)
                               let undergroundX = groundOffset.X + (iterationX * inGameOptions.TileSize.X)
                               from iterationY in Enumerable.Range(1, yCount - 1)
                               let undergroundY = groundOffset.Y + (iterationY * inGameOptions.TileSize.Y)
                               select new GroundTile(this.groundTextures[^1], new(undergroundX, undergroundY));

        var grassTiles = from iteration in Enumerable.Range(0, Random.Shared.Next(xCount / 2, xCount))
                         let grassX = Random.Shared.Next(0, xCount) * inGameOptions.TileSize.X
                         let grassTexture = Random.Shared.Pick(this.grassTextures.AsSpan())
                         select new GroundTile(grassTexture, new(grassX, groundOffset.Y - inGameOptions.TileSize.Y));

        return [.. surfaceTiles, .. undergroundTiles, .. grassTiles.DistinctBy(tile => tile.Point.X)];
    }
}

public static class RandomHelper
{
    public static T Pick<T>(this Random random, ReadOnlySpan<T> span) => span[random.Next(0, span.Length - 1)];
}

public static class GameHelper 
{
    public static IEnumerable<T> LoadDirectory<T>(this Game game, string subDirectory)
    {
        Directory.SetCurrentDirectory(game.Content.RootDirectory);        
        IEnumerable<T> resources = Directory.EnumerateFiles(subDirectory).Select(path => path[..^4])
                                            .Select(game.Content.Load<T>);
        Directory.SetCurrentDirectory("../");
        return resources;
    }

    public static bool ValidateSize(this IEnumerable<Texture2D> textures, Point expectedSize) 
        => !textures.Any(textures => textures.Bounds.Size != expectedSize);

}