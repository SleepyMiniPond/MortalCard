using System;
using System.Collections.Generic;
using System.Linq;
using Optional;
using Optional.Unsafe;
using UnityEngine;

public static class Utility
{
    public class Dictionary<T, V> : System.Collections.Generic.Dictionary<T, V>
    {
        public static readonly Dictionary<T, V> EMPTY = new ();
    }

    public static bool TryGetValue<T>(this Option<T> opt, out T value)
    {
        value = opt.Match(
            v => v,
            () => default);

        return opt.HasValue;
    }

    public static Option<(T1, T2)> Combine<T1, T2>(this Option<T1> first, Option<T2> second)
    {
        return first.Match(
            v1 => second.Match(
                v2 => Option.Some((v1, v2)),
                () => Option.None<(T1, T2)>()),
            () => Option.None<(T1, T2)>());
    }


    public static bool Eval<T>(this SetConditionType conditionType, IEnumerable<T> items, System.Func<T, bool> predicate)
    {
        return conditionType switch
        {
            SetConditionType.AnyInside => items.Any(predicate),
            SetConditionType.AllInside => items.All(predicate),
            SetConditionType.AnyOutside => items.Any(item => !predicate(item)),
            SetConditionType.AllOutside => items.All(item => !predicate(item)),
            _ => false
        };
    }

    public static IEnumerable<T> WrapAsEnumerable<T>(this T item)
    {
        yield return item;
    }

    public static IEnumerable<T> Values<T>(this IEnumerable<Option<T>> source)
    {
        foreach (var option in source)
        {
            if (option.TryGetValue(out var value))
            {
                yield return value;
            }
        }
    }

    public static IEnumerable<U> SelectValue<T, U>(this IEnumerable<T> source, Func<T, U> selector)
    {
        foreach (var item in source)
        {
            var value = selector(item);
            if (value != null)
            {
                yield return value;
            }
        }
    } 
    
    public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
    {
        var buffer = source.ToList();
        for (int i = buffer.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            T temp = buffer[i];
            buffer[i] = buffer[j];
            buffer[j] = temp;
        }

        foreach (T item in buffer)
        {
            yield return item;
        }
    }
}

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}