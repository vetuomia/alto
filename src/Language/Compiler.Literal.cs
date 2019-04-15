static partial class Compiler {
  /// <summary>
  /// An abstract base class for all literal expressions.
  /// </summary>
  private abstract class Literal : Expression { }

  /// <summary>
  /// Null literal.
  /// </summary>
  private sealed class NullLiteral : Literal {
    /// <summary>
    /// Validates the language element semantics.
    /// </summary>
    public override void Validate() { }

    /// <summary>
    /// Emits the code and data.
    /// </summary>
    /// <param name="emitter">The emitter.</param>
    /// <param name="exits">The non-exceptional exit targets.</param>
    public override void Emit(Emitter emitter, Exits exits) => emitter.Emit(Opcode.Null);
  }

  /// <summary>
  /// Boolean literal.
  /// </summary>
  private sealed class BooleanLiteral : Literal {
    /// <summary>
    /// The literal value.
    /// </summary>
    public bool Value { get; set; }

    /// <summary>
    /// Validates the language element semantics.
    /// </summary>
    public override void Validate() { }

    /// <summary>
    /// Emits the code and data.
    /// </summary>
    /// <param name="emitter">The emitter.</param>
    /// <param name="exits">The non-exceptional exit targets.</param>
    public override void Emit(Emitter emitter, Exits exits) => emitter.Emit(Opcode.Boolean, value: this.Value ? 1 : 0);
  }

  /// <summary>
  /// Number literal.
  /// </summary>
  private sealed class NumberLiteral : Literal {
    /// <summary>
    /// The literal value.
    /// </summary>
    public double Value => this.Token.NumberValue;

    /// <summary>
    /// Validates the language element semantics.
    /// </summary>
    public override void Validate() { }

    /// <summary>
    /// Emits the code and data.
    /// </summary>
    /// <param name="emitter">The emitter.</param>
    /// <param name="exits">The non-exceptional exit targets.</param>
    public override void Emit(Emitter emitter, Exits exits) {
      var integer = (int)this.Value;

      if (integer == this.Value && Instruction.InValueRange(integer)) {
        emitter.Emit(Opcode.Number, value: integer);
      } else {
        emitter.Emit(Opcode.LoadGlobal, value: emitter.GetOrAdd(this.Value));
      }
    }
  }

  /// <summary>
  /// String literal.
  /// </summary>
  private sealed class StringLiteral : Literal {
    /// <summary>
    /// The literal value.
    /// </summary>
    public string Value => this.Token.StringValue ?? this.Token.Text;

    /// <summary>
    /// Validates the language element semantics.
    /// </summary>
    public override void Validate() { }

    /// <summary>
    /// Emits the code and data.
    /// </summary>
    /// <param name="emitter">The emitter.</param>
    /// <param name="exits">The non-exceptional exit targets.</param>
    public override void Emit(Emitter emitter, Exits exits) => emitter.Emit(Opcode.LoadGlobal, value: emitter.GetOrAdd(this.Value));
  }
}
