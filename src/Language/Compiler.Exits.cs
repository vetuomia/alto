static partial class Compiler {
  /// <summary>
  /// The non-exceptional exit targets.
  /// </summary>
  sealed class Exits {
    /// <summary>
    /// The continue target.
    /// </summary>
    public Emitter Continue { get; set; }

    /// <summary>
    /// The break target.
    /// </summary>
    public Emitter Break { get; set; }

    /// <summary>
    /// The return target.
    /// </summary>
    public Emitter Return { get; set; }

    /// <summary>
    /// Initializes a new instance of the class.
    /// </summary>
    /// <param name="returnTarget">The return target.</param>
    public Exits(Emitter returnTarget) => this.Return = returnTarget;

    /// <summary>
    /// Initializes a new instance of the class.
    /// </summary>
    /// <param name="previous">The previous targets.</param>
    public Exits(Exits previous) {
      this.Continue = previous.Continue;
      this.Break = previous.Break;
      this.Return = previous.Return;
    }
  }
}
