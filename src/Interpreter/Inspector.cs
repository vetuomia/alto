using System;

/// <summary>
/// Inspector delegate.
/// </summary>
/// <param name="interpreter">The interpreter.</param>
/// <param name="exception">The thrown exception, if any.</param>
delegate void Inspector(ref Interpreter interpreter, Exception exception = null);

/// <summary>
/// Inspector data.
/// </summary>
struct InspectorData {
  /// <summary>
  /// The data source.
  /// </summary>
  public InspectorDataSource Source { get; }

  /// <summary>
  /// The name.
  /// </summary>
  public string Name { get; }

  /// <summary>
  /// The value.
  /// </summary>
  public Value Value { get; }

  /// <summary>
  /// Initializes a new instance of the struct.
  /// </summary>
  /// <param name="source">The data source.</param>
  /// <param name="name">The name.</param>
  /// <param name="value">The value.</param>
  public InspectorData(InspectorDataSource source, string name, Value value) {
    this.Source = source;
    this.Name = name;
    this.Value = value;
  }
}

/// <summary>
/// Inspector data sources.
/// </summary>
[Flags]
enum InspectorDataSource {
  /// <summary>
  /// The module data.
  /// </summary>
  Data = 1 << 0,

  /// <summary>
  /// The function receiver.
  /// </summary>
  Receiver = 1 << 1,

  /// <summary>
  /// The function arguments.
  /// </summary>
  Arguments = 1 << 2,

  /// <summary>
  /// The interpreter stack.
  /// </summary>
  Stack = 1 << 3,

  /// <summary>
  /// The interpreter closure.
  /// </summary>
  Closure = 1 << 4,

  /// <summary>
  /// The interpreter registers.
  /// </summary>
  Registers = 1 << 5,

  /// <summary>
  /// The function parameters (receiver and arguments).
  /// </summary>
  Parameters = Receiver | Arguments,

  /// <summary>
  /// The variables in scope (stack and closure).
  /// </summary>
  Variables = Stack | Closure,

  /// <summary>
  /// The symbols in scope (data, parameters and variables).
  /// </summary>
  Scope = Data | Parameters | Variables,

  /// <summary>
  /// All data sources.
  /// </summary>
  All = -1,
}
