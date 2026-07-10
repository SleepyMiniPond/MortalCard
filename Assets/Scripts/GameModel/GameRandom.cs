using System;
using System.Collections.Generic;

namespace MortalGame.GameModel
{

public interface IGameRandom
{
    int Range(int minInclusive, int maxExclusive);
    void ShuffleInPlace<T>(IList<T> items);
}

public class GameRandom : IGameRandom
{
    private readonly Random _random;

    public GameRandom(int seed)
    {
        _random = new Random(seed);
    }

    public int Range(int minInclusive, int maxExclusive)
    {
        return _random.Next(minInclusive, maxExclusive);
    }

    public void ShuffleInPlace<T>(IList<T> items)
    {
        for (int i = items.Count - 1; i > 0; i--)
        {
            var randomIndex = Range(0, i + 1);
            (items[i], items[randomIndex]) = (items[randomIndex], items[i]);
        }
    }
}

}
