using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace GBGame.States;

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