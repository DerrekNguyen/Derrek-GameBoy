using System;
using System.Threading;

public static class Common
{
    public static bool BIT(byte a, int n)
    {
        return (a & (1 << n)) != 0;
    }

    public static byte BIT_SET(byte a, int n, bool on)
    {
        return on ? (byte)(a | (1 << n)) : (byte)(a & ~(1 << n));
    }

    public static bool BETWEEN<T>(T a, T b, T c) where T : IComparable<T>
    {
        return a.CompareTo(b) >= 0 && a.CompareTo(c) <= 0;
    }
}