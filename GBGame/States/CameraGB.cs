using Microsoft.Xna.Framework;
using MonoGayme.Components;

namespace GBGame.States;

public class CameraGB() : Camera2D(Vector2.Zero)
{
    public int Offset { get; } = 40;
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