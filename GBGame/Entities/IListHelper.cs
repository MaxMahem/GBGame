using System;
using System.Collections.Generic;
using System.Linq;

namespace GBGame.Entities;

public static class IListHelper
{
    public static void Shuffle<T>(this IList<T> list)
    {
        var swapIndexes = Enumerable.Range(0, list.Count)
            .Select(i => (i,  Random.Shared.Next(0, list.Count - i)));
        foreach (var (index, randomIndex) in swapIndexes) {
            (list[index], list[randomIndex]) = (list[randomIndex], list[index]);
        }
    }
}