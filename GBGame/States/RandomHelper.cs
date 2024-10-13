using System;

namespace GBGame.States;

public static class RandomHelper
{
    public static T Pick<T>(this Random random, ReadOnlySpan<T> span) => span[random.Next(0, span.Length - 1)];
}
