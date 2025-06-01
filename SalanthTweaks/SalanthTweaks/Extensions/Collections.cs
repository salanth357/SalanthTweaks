using System.Collections.Generic;

namespace SalanthTweaks.Extensions;

public static class Collections
{
    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        foreach (var element in source)
            action(element);
    }

}

