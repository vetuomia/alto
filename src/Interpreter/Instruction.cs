using System.Diagnostics;
using System.Text;

/// <summary>
/// Virtual machine instruction, with two parts: the operation code and the
/// operands. The operation code selects the operation to perform (e.g. add
/// two numbers, jump to another address, etc.), the meaning of the operands
/// depend on the selected operation.
/// </summary>
struct Instruction {
  /// <summary>
  /// The number of bits per instruction.
  /// </summary>
  public const int SizeInBits = sizeof(int) * 8;

  /// <summary>
  /// The number of bits reserved for the operation code.
  /// </summary>
  public const int OpcodeBits = 6;

  /// <summary>
  /// The number of bits reserved for the param operand.
  /// </summary>
  public const int ParamBits = 4;

  /// <summary>
  /// The number of bits reserved for the value operand.
  /// </summary>
  public const int ValueBits = SizeInBits - OpcodeBits - ParamBits;

  /// <summary>
  /// The maximum value for the <see cref="Opcode" />.
  /// </summary>
  public const int MaxOpcode = (1 << OpcodeBits) - 1;

  /// <summary>
  /// The minimum value for the <see cref="Opcode" />.
  /// </summary>
  public const int MinOpcode = 0;

  /// <summary>
  /// The maximum value for the <see cref="Param" /> operand.
  /// </summary>
  public const int MaxParam = (1 << ParamBits) - 1;

  /// <summary>
  /// The minimum value for the <see cref="Param" /> operand.
  /// </summary>
  public const int MinParam = 0;

  /// <summary>
  /// The maximum value for the <see cref="Value" /> operand.
  /// </summary>
  public const int MaxValue = (1 << (ValueBits - 1)) - 1;

  /// <summary>
  /// The minimum value for the <see cref="Value" /> operand.
  /// </summary>
  public const int MinValue = -(1 << (ValueBits - 1));

  /// <summary>
  /// The operation code and the operands, encoded as an integer.
  /// </summary>
  [DebuggerBrowsable(DebuggerBrowsableState.Never)]
  private readonly int bits;

  /// <summary>
  /// The operation code.
  /// </summary>
  public Opcode Opcode => (Opcode)(this.bits & MaxOpcode);

  /// <summary>
  /// The param operand.
  /// </summary>
  public int Param => (this.bits >> OpcodeBits) & MaxParam;

  /// <summary>
  /// The value operand.
  /// </summary>
  public int Value => this.bits >> (OpcodeBits + ParamBits);

  /// <summary>
  /// Initializes a new instance of the struct.
  /// </summary>
  /// <param name="opcode">The operation code.</param>
  /// <param name="param">The param operand.</param>
  /// <param name="value">The value operand.</param>
  public Instruction(Opcode opcode, int param, int value) =>
    this.bits = (value << (OpcodeBits + ParamBits))
      | ((param & MaxParam) << OpcodeBits)
      | ((int)opcode & MaxOpcode);

  /// <summary>
  /// Determines whether the param is withing the allowed range.
  /// </summary>
  /// <param name="param">The param.</param>
  public static bool InParamRange(int param) => MinParam <= param && param <= MaxParam;

  /// <summary>
  /// Determines whether the value is withing the allowed range.
  /// </summary>
  /// <param name="value">The value.</param>
  public static bool InValueRange(int value) => MinValue <= value && value <= MaxValue;

  /// <summary>
  /// Returns a string representation of the instruction.
  /// </summary>
  public override string ToString() {
    var sb = new StringBuilder();

    if (AssemblyAttribute.TryGet(this.Opcode, this.Param, this.Value, out var info)) {
      sb.Append(info.Mnemonic.PadRight(10));

      if (info.Param == Operand.Required || (info.Param == Operand.Optional && this.Param != info.DefaultParam)) {
        sb.Append(" ").Append(this.Param);
      }

      if (info.Value == Operand.Required || (info.Value == Operand.Optional && this.Value != info.DefaultValue)) {
        sb.Append(" ").Append(this.Value);
      }
    } else {
      sb.Append(this.Opcode)
        .Append(" p: ").Append(this.Param)
        .Append(" v: ").Append(this.Value);
    }

    return sb.ToString().PadRight(14);
  }
}
