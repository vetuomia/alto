/// <summary>
/// Operand information.
/// </summary>
enum Operand {
  /// <summary>
  /// The operand is not allowed.
  /// </summary>
  Forbidden = 0,

  /// <summary>
  /// The operand is optional.
  /// </summary>
  Optional,

  /// <summary>
  /// The operand is required.
  /// </summary>
  Required,
}
