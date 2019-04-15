/// <summary>
/// Closure is a data structure that holds the values of the variables that have
/// been captured from an outside scope, e.g. parent function. These variables
/// would normally be unreachable and would disappear when the outer function
/// returns.
///
/// Multiple functions can share the same closure, allowing them to share the
/// captured variables. The closures form a chain (or a tree) where each node
/// holds a reference to the parent closure. This allows deep nesting where even
/// the innermost function can still access the variables by walking the chain.
/// </summary>
sealed class Closure {
  /// <summary>
  /// The parent closure.
  /// </summary>
  public Closure Parent { get; }

  /// <summary>
  /// The values of the captured variables.
  /// </summary>
  public Value[] Values { get; }

  /// <summary>
  /// Initializes a new instance of the class.
  /// </summary>
  /// <param name="parent">The parent closure.</param>
  /// <param name="values">The values of the captured variables.</param>
  public Closure(Closure parent, Value[] values) {
    this.Parent = parent;
    this.Values = values;
  }
}
