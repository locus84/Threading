using System.Threading;

internal static class Atomic
{
    public static bool SwapIfSame<T>(ref T original, T toSwap, T toCompare) where T : class
    {
        return ReferenceEquals(Interlocked.CompareExchange(ref original, toSwap, toCompare), toCompare);
    }

    public static T Swap<T>(ref T original, T toSwap) where T : class
    {
        return Interlocked.Exchange(ref original, toSwap);
    }
}