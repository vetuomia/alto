using System.Collections.Generic;

/// <summary>
/// Generic hash table.
/// </summary>
sealed class Table : Dictionary<Value, Value> {
  /// <summary>
  /// Initializes a new instance of the class.
  /// </summary>
  public Table() { }

  /// <summary>
  /// Initializes a new instance of the class.
  /// </summary>
  /// <param name="capacity">The initial capacity.</param>
  public Table(int capacity) : base(capacity) { }
}
