/// <summary>
/// Property accessor. Defines the functions that are called when a property
/// is accessed from code.
/// </summary>
sealed class Property {
  /// <summary>
  /// Property getter delegate.
  /// </summary>
  /// <param name="obj">The container.</param>
  /// <returns>The property value.</returns>
  public delegate Value Getter(Value obj);

  /// <summary>
  /// Property setter delegate.
  /// </summary>
  /// <param name="obj">The container.</param>
  /// <param name="value">The property value.</param>
  public delegate void Setter(Value obj, Value value);

  /// <summary>
  /// The getter function.
  /// </summary>
  public Getter Get { get; set; }

  /// <summary>
  /// The setter function.
  /// </summary>
  public Setter Set { get; set; }
}
