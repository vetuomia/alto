using System;

/// <summary>
/// The generic extension methods.
/// </summary>
static partial class Extensions {
  /// <summary>
  /// Data key for a value wrapped in an exception.
  /// </summary>
  private const string ValueDataKey = ".value";

  /// <summary>
  /// Data key for a custom stack trace in an exception.
  /// </summary>
  private const string StackTraceDataKey = ".stackTrace";

  /// <summary>
  /// Converts a value to an exception.
  /// </summary>
  /// <param name="self">The value.</param>
  /// <returns>The exception.</returns>
  public static Exception ValueToException(this Value self) => self.IsException(out var exception) ? exception : new Exception(self.ToString()) { Data = { [ValueDataKey] = self } };

  /// <summary>
  /// Converts an exception to a value.
  /// </summary>
  /// <param name="self">The exception.</param>
  /// <returns>The value.</returns>
  public static Value ExceptionToValue(this Exception self) => (self.Data[ValueDataKey] is Value value) ? value : self;

  /// <summary>
  /// Gets the custom stack trace.
  /// </summary>
  /// <param name="self">The exception.</param>
  /// <returns>The custom stack trace or null if not set.</returns>
  public static string GetStackTrace(this Exception self) => self.Data[StackTraceDataKey] as string;

  /// <summary>
  /// Sets the custom stack trace.
  /// </summary>
  /// <param name="self">The exception.</param>
  /// <param name="stackTrace">The custom stack trace.</param>
  public static void SetStackTrace(this Exception self, string stackTrace) => self.Data[StackTraceDataKey] = stackTrace;
}
