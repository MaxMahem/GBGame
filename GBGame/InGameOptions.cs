using Microsoft.Xna.Framework;

namespace GBGame;

public class InGameOptions
{
    public Point FieldSize { get; set; } = new(288, 144);

    public int GroundTileRows { get; set; } = 2;

    public Point TileSize { get; set; } = new(8, 8);

    public string GroundSpriteFolder { get; set; } = "Sprites/Ground/";
    public string GrassSpriteFolder { get; set; } = "Sprites/Grass";
}