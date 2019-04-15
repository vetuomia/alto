static partial class Compiler {
  /// <summary>
  /// An abstract base class for all language elements.
  /// </summary>
  private abstract class LanguageElement {
    /// <summary>
    /// The token that produced this element.
    /// </summary>
    public Token Token { get; set; }

    /// <summary>
    /// The scope where this element was defined in.
    /// </summary>
    public LexicalScope Scope { get; set; }

    /// <summary>
    /// Validates the language element semantics.
    /// </summary>
    public abstract void Validate();

    /// <summary>
    /// Emits the code and data.
    /// </summary>
    /// <param name="emitter">The emitter.</param>
    /// <param name="exits">The non-exceptional exit targets.</param>
    public abstract void Emit(Emitter emitter, Exits exits);
  }
}
