using System;
using System.Collections.Generic;

/// <summary>
/// Workaround for CoreRT linker warnings caused by certain LINQ operations.
/// </summary>
static class CoreRTLinq {
  /// <summary>
  /// Forward to the real Linq implementation.
  /// </summary>
  public static bool Any<T>(this IEnumerable<T> sequence) => System.Linq.Enumerable.Any(sequence);

  /// <summary>
  /// Forward to the real Linq implementation.
  /// </summary>
  public static bool Any<T>(this IEnumerable<T> sequence, Func<T, bool> predicate) => System.Linq.Enumerable.Any(sequence, predicate);

  /// <summary>
  /// Forward to the real Linq implementation.
  /// </summary>
  public static IEnumerable<T> Cast<T>(this System.Collections.IEnumerable sequence) => System.Linq.Enumerable.Cast<T>(sequence);

  /// <summary>
  /// Forward to the real Linq implementation.
  /// </summary>
  public static T ElementAt<T>(this IEnumerable<T> sequence, int index) => System.Linq.Enumerable.ElementAt(sequence, index);

  /// <summary>
  /// Forward to the real Linq implementation.
  /// </summary>
  public static T FirstOrDefault<T>(this IEnumerable<T> sequence) => System.Linq.Enumerable.FirstOrDefault(sequence);

  /// <summary>
  /// Forward to the real Linq implementation.
  /// </summary>
  public static T FirstOrDefault<T>(this IEnumerable<T> sequence, Func<T, bool> predicate) => System.Linq.Enumerable.FirstOrDefault(sequence, predicate);

  /// <summary>
  /// Workaround for a linker warning in CoreRT. Something in the real Select produces:
  /// ```
  /// ld : warning : PIE disabled. Absolute addressing (perhaps -mdynamic-no-pic) not allowed
  /// in code signed PIE, but used in ... To fix this warning, don't compile with -mdynamic-no-pic
  /// or link with -Wl,-no_pie
  /// ```
  /// </summary>
  public static IEnumerable<R> Select<T, R>(this IEnumerable<T> sequence, Func<T, R> projection) {
    foreach (var item in sequence) {
      yield return projection(item);
    }
  }

  /// <summary>
  /// Forward to the real Linq implementation.
  /// </summary>
  public static IEnumerable<R> SelectMany<T, C, R>(this IEnumerable<T> sequence, Func<T, IEnumerable<C>> collectionSelector, Func<T, C, R> resultSelector) => System.Linq.Enumerable.SelectMany(sequence, collectionSelector, resultSelector);

  /// <summary>
  /// Forward to the real Linq implementation.
  /// </summary>
  public static IEnumerable<TElement> OrderBy<TElement, TKey>(this IEnumerable<TElement> sequence, Func<TElement, TKey> keySelector) => System.Linq.Enumerable.OrderBy(sequence, keySelector);

  /// <summary>
  /// Forward to the real Linq implementation.
  /// </summary>
  public static IEnumerable<T> Take<T>(this IEnumerable<T> sequence, int count) => System.Linq.Enumerable.Take<T>(sequence, count);

  /// <summary>
  /// Forward to the real Linq implementation.
  /// </summary>
  public static T[] ToArray<T>(this IEnumerable<T> sequence) => System.Linq.Enumerable.ToArray(sequence);

  /// <summary>
  /// Forward to the real Linq implementation.
  /// </summary>
  public static System.Linq.ILookup<TKey, TValue> ToLookup<T, TKey, TValue>(this IEnumerable<T> sequence, Func<T, TKey> keySelector, Func<T, TValue> valueSelector) => System.Linq.Enumerable.ToLookup(sequence, keySelector, valueSelector);

  /// <summary>
  /// Forward to the real Linq implementation.
  /// </summary>
  public static Dictionary<TKey, TValue> ToDictionary<T, TKey, TValue>(this IEnumerable<T> sequence, Func<T, TKey> keySelector, Func<T, TValue> valueSelector) => System.Linq.Enumerable.ToDictionary(sequence, keySelector, valueSelector);

  /// <summary>
  /// Forward to the real Linq implementation.
  /// </summary>
  public static IEnumerable<T> Where<T>(this IEnumerable<T> sequence, Func<T, bool> predicate) => System.Linq.Enumerable.Where(sequence, predicate);
}
