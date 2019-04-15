using System.Collections.Generic;

/// <summary>
/// Generic list.
/// </summary>
sealed class List : List<Value> {
  /// <summary>
  /// Initializes a new instance of the class.
  /// </summary>
  public List() { }

  /// <summary>
  /// Initializes a new instance of the class.
  /// </summary>
  /// <param name="capacity">The initial capacity.</param>
  public List(int capacity) : base(capacity) { }
}
