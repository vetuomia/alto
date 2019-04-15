/// <summary>
/// Maps an instruction to the original source code.
/// </summary>
sealed class SourceMap {
  /// <summary>
  /// The source code, split into rows.
  /// </summary>
  public string[] Source { get; }

  /// <summary>
  /// The row in the source code.
  /// </summary>
  public int? Row { get; set; }

  /// <summary>
  /// The column in the source code.
  /// </summary>
  public int? Column { get; set; }

  /// <summary>
  /// The name of the enclosing function.
  /// </summary>
  public string Function { get; set; }

  /// <summary>
  /// The parameters in the current scope.
  /// </summary>
  public Parameter[] Parameters { get; set; }

  /// <summary>
  /// The variables in the current scope.
  /// </summary>
  public Variable[] Variables { get; set; }

  /// <summary>
  /// The global in the current scope.
  /// </summary>
  public Global[] Globals { get; set; }

  /// <summary>
  /// Initializes a new instance of the class.
  /// </summary>
  /// <param name="source">The source code, split into lines.</param>
  public SourceMap(string[] source) {
    this.Source = source;
  }

  /// <summary>
  /// Initializes a new instance of the class.
  /// </summary>
  /// <param name="previous">The previous source map.</param>
  public SourceMap(SourceMap previous) {
    this.Source = previous.Source;
    this.Row = previous.Row;
    this.Column = previous.Column;
    this.Function = previous.Function;
    this.Parameters = previous.Parameters;
    this.Variables = previous.Variables;
    this.Globals = previous.Globals;
  }

  /// <summary>
  /// Maps a parameter to the original source code.
  /// </summary>
  public sealed class Parameter {
    /// <summary>
    /// The parameter index.
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// Indicates whether it's a rest parameter.
    /// </summary>
    public bool IsRestParameter { get; set; }

    /// <summary>
    /// The parameter name.
    /// </summary>
    public string Name { get; set; }
  }

  /// <summary>
  /// Maps a variable to the original source code.
  /// </summary>
  public sealed class Variable {
    /// <summary>
    /// The steps into outer scopes, zero is the local scope.
    /// </summary>
    public int Scope { get; set; }

    /// <summary>
    /// The variable index.
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// The variable name.
    /// </summary>
    public string Name { get; set; }
  }

  /// <summary>
  /// Maps a global to the original source code.
  /// </summary>
  public sealed class Global {
    /// <summary>
    /// The global index.
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// The global name.
    /// </summary>
    public string Name { get; set; }
  }
}
