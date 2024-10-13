using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Linq;
using System.Collections.Immutable;

namespace GBGame.States;

public class GridManager(GameWindow game, InGameOptions inGameOptions)
{
    public record struct GroundTile(Texture2D Sprite, Point Point);

    public int GroundLine { get; } = (int) (inGameOptions.FieldSize.Y - (inGameOptions.TileSize.Y * 2.5));

    public ImmutableArray<GroundTile> GroundTiles { get; } = GenerateTiles(game, inGameOptions);

    public static ImmutableArray<GroundTile> GenerateTiles(GameWindow game, InGameOptions inGameOptions)
    {
        Texture2D[] groundTextures = game.LoadDirectory<Texture2D>(inGameOptions.GroundSpriteFolder).ToArray();
        Texture2D[] grassTextures = game.LoadDirectory<Texture2D>(inGameOptions.GrassSpriteFolder).ToArray();

        if (!groundTextures.ValidateSize(inGameOptions.TileSize) || !grassTextures.ValidateSize(inGameOptions.TileSize)) 
        { 
            throw new InvalidOperationException("Invalid Tile Size."); 
        }

        var (xCount, yCount) = int.DivRem(inGameOptions.FieldSize.X, inGameOptions.TileSize.X) is { Quotient: int xc, Remainder: 0 }
                            && int.DivRem(inGameOptions.FieldSize.Y, inGameOptions.TileSize.Y) is { Quotient: int yc, Remainder: 0 }
                            && yc >= inGameOptions.GroundTileRows ? (xc, int.Min(yc, inGameOptions.GroundTileRows)) 
                            : throw new InvalidOperationException($"Tiles bounds must divide evenly into game field bounds.");

        Point groundOffset = new(0, inGameOptions.FieldSize.Y - (inGameOptions.TileSize.Y * inGameOptions.GroundTileRows));

        var surfaceTiles = from iteration in Enumerable.Range(0, xCount)
                           let groundTexture = Random.Shared.Pick<Texture2D>(groundTextures.AsSpan()[..^1])
                           let groundX = groundOffset.X + (iteration * inGameOptions.TileSize.X)
                           select new GroundTile(groundTexture, new(groundX, groundOffset.Y));

        var undergroundTiles = from iterationX in Enumerable.Range(0, xCount)
                               let undergroundX = groundOffset.X + (iterationX * inGameOptions.TileSize.X)
                               from iterationY in Enumerable.Range(1, yCount - 1)
                               let undergroundY = groundOffset.Y + (iterationY * inGameOptions.TileSize.Y)
                               select new GroundTile(groundTextures[^1], new(undergroundX, undergroundY));

        var grassTiles = from iteration in Enumerable.Range(0, Random.Shared.Next(xCount / 2, xCount))
                         let grassX = Random.Shared.Next(0, xCount) * inGameOptions.TileSize.X
                         let grassTexture = Random.Shared.Pick<Texture2D>(grassTextures.AsSpan())
                         select new GroundTile(grassTexture, new(grassX, groundOffset.Y - inGameOptions.TileSize.Y));

        return [.. surfaceTiles, .. undergroundTiles, .. grassTiles.DistinctBy(tile => tile.Point.X)];
    }
}
