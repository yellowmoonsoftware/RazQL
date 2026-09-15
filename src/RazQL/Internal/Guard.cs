using System.Runtime.CompilerServices;

namespace RazQL.Internal;

internal static class Guard
{
    public static void ThrowIf<T>(T argument, Predicate<T> predicate, string message,
        [CallerArgumentExpression("argument")] string? argumentName = null)
    {
        if (predicate(argument)) throw new ArgumentException(message, argumentName);
    }
}
