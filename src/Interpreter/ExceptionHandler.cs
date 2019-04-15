/// <summary>
/// Exception handler chain for keeping track of the exception handlers.
/// </summary>
sealed class ExceptionHandler {
  /// <summary>
  /// The parent handler.
  /// </summary>
  public ExceptionHandler Parent { get; }

  /// <summary>
  /// The handler address.
  /// </summary>
  public int IP { get; }

  /// <summary>
  /// The stack unwind point.
  /// </summary>
  public int SP { get; }

  /// <summary>
  /// The closure unwind point.
  /// </summary>
  public Closure Closure { get; }

  /// <summary>
  /// Initializes a new instance of the class.
  /// </summary>
  /// <param name="parent">The parent handler.</param>
  /// <param name="ip">The handler address.</param>
  /// <param name="sp">The stack unwind point.</param>
  /// <param name="closure">The closure unwind point.</param>
  public ExceptionHandler(ExceptionHandler parent, int ip, int sp, Closure closure) {
    this.Parent = parent;
    this.IP = ip;
    this.SP = sp;
    this.Closure = closure;
  }
}
