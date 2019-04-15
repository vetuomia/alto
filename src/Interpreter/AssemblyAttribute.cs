using System;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// Describes the assembly representation of an instruction.
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
sealed class AssemblyAttribute : Attribute {
  /// <summary>
  /// Maps opcodes to assembly metadata.
  /// </summary>
  private static readonly System.Linq.ILookup<Opcode, AssemblyAttribute> OpcodeToInfo;

  /// <summary>
  /// Maps mnemonics to assembly metadata.
  /// </summary>
  private static readonly Dictionary<string, AssemblyAttribute> MnemonicToInfo;

  /// <summary>
  /// The operation mnemonic.
  /// </summary>
  public string Mnemonic { get; }

  /// <summary>
  /// The operation code.
  /// </summary>
  public Opcode Opcode { get; set; }

  /// <summary>
  /// The Param operand requirement.
  /// </summary>
  public Operand Param { get; set; }

  /// <summary>
  /// The Value operand requirement.
  /// </summary>
  public Operand Value { get; set; }

  /// <summary>
  /// The default Param operand value.
  /// </summary>
  public int DefaultParam { get; set; }

  /// <summary>
  /// The default Value operand value.
  /// </summary>
  public int DefaultValue { get; set; }

  /// <summary>
  /// Indicates whether the Param operand is optional.
  /// </summary>
  public bool ParamIsOptional => this.Param == Operand.Optional;

  /// <summary>
  /// Indicates whether the Value operand is optional.
  /// </summary>
  public bool ValueIsOptional => this.Value == Operand.Optional;

  /// <summary>
  /// The required minimum number of operands.
  /// </summary>
  public int MinOperands => (this.Param == Operand.Required ? 1 : 0) + (this.Value == Operand.Required ? 1 : 0);

  /// <summary>
  /// The allowed maximum number of operands.
  /// </summary>
  public int MaxOperands => (this.Param != 0 ? 1 : 0) + (this.Value != 0 ? 1 : 0);

  /// <summary>
  /// Initializes the class.
  /// </summary>
  static AssemblyAttribute() {
    var type = typeof(Opcode);
    var opcodes = (from Opcode opcode in Enum.GetValues(type)
                   let name = Enum.GetName(type, opcode)
                   from  info in type.GetField(name).GetCustomAttributes().Cast<AssemblyAttribute>()
                   select (opcode, info)).ToArray();

    foreach (var item in opcodes) {
      item.info.Opcode = item.opcode;
    }

    OpcodeToInfo = opcodes.ToLookup(i => i.opcode, i => i.info);
    MnemonicToInfo = opcodes.ToDictionary(i => i.info.Mnemonic, i => i.info);
  }

  /// <summary>
  /// Initializes a new instance of the class.
  /// </summary>
  /// <param name="mnemonic">The operation mnemonic.</param>
  public AssemblyAttribute(string mnemonic) => this.Mnemonic = mnemonic;

  /// <summary>
  /// Attempts to get the assembly metadata for the given mnemonic.
  /// </summary>
  /// <param name="mnemonic">The mnemonic.</param>
  /// <param name="info">The output variable for the assembly metadata.</param>
  public static bool TryGet(string mnemonic, out AssemblyAttribute info) {
    return MnemonicToInfo.TryGetValue(mnemonic, out info);
  }

  /// <summary>
  /// Attempts to get the assembly metadata for the given instruction.
  /// </summary>
  /// <param name="opcode">The opcode.</param>
  /// <param name="param">The param.</param>
  /// <param name="value">The value.</param>
  /// <param name="info">The output variable for the assembly metadata.</param>
  public static bool TryGet(Opcode opcode, int param, int value, out AssemblyAttribute info) {
    bool When(bool a, bool b) => !a || b;

    if (OpcodeToInfo.Contains(opcode)) {
      var found = from option in OpcodeToInfo[opcode]
                  where When(option.Param == Operand.Forbidden, param == option.DefaultParam)
                  where When(option.Value == Operand.Forbidden, value == option.DefaultValue)
                  select option;

      info = found.FirstOrDefault();
      return info != null;
    }

    info = default;
    return false;
  }
}
