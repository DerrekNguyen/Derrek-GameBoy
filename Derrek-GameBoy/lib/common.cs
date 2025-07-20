using System;
using System.Runtime.CompilerServices;
using System.Threading;

public static class Common
{
   /// <summary>
   /// Check if the 'n'th bit in byte 'a' is set (1)
   /// </summary>
   /// <param name="a">Byte variable</param>
   /// <param name="n">Index of the bit</param>
   /// <returns></returns>
   public static bool BIT(byte a, int n)
   {
      return (a & (1 << n)) != 0;
   }

   /// <summary>
   /// Set or Clear the 'n'th bit in byte 'a', depending on flag 'on'
   /// </summary>
   /// <param name="a">Byte variable</param>
   /// <param name="n">Index of the bit</param>
   /// <param name="on">Flag</param>
   /// <returns></returns>
   public static byte BIT_SET(byte a, int n, bool on)
   {
      return on ? (byte)(a | (1 << n)) : (byte)(a & ~(1 << n));
   }

   /// <summary>
   /// Check whether a is between b and c
   /// </summary>
   /// <typeparam name="T">the type of a, b and c</typeparam>
   /// <param name="a">target variable</param>
   /// <param name="b">lower limit</param>
   /// <param name="c">upper limit</param>
   /// <returns></returns>
   public static bool BETWEEN<T>(T a, T b, T c) where T : IComparable<T>
   {
      return a.CompareTo(b) >= 0 && a.CompareTo(c) <= 0;
   }

   public static void NO_IMPL(
      [CallerMemberName] string memberName = "",
      [CallerFilePath] string file = "",
      [CallerLineNumber] int line = 0)
   {
      throw new NotImplementedException($"NOT IMPLEMENTED {memberName} at {file}: {line}");
   }
}