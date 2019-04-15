using System.Collections.Generic;
using System.Text;

static partial class Compiler {
  /// <summary>
  /// Slot kind.
  /// </summary>
  private enum SlotKind {
    /// <summary>
    /// No kind.
    /// </summary>
    None = 0,

    /// <summary>
    /// Slot is an import.
    /// </summary>
    Import,

    /// <summary>
    /// Slot is a variable.
    /// </summary>
    Variable,

    /// <summary>
    /// Slot is a parameter.
    /// </summary>
    Parameter,
  }

  /// <summary>
  /// Slot value source.
  /// </summary>
  private enum SlotSource {
    /// <summary>
    /// No source.
    /// </summary>
    None = 0,

    /// <summary>
    /// Slot is sourced from an argument.
    /// </summary>
    Argument,

    /// <summary>
    /// Slot is sourced from an argument slice.
    /// </summary>
    ArgumentSlice,
  }

  /// <summary>
  /// Slot storage location.
  /// </summary>
  private enum SlotStorage {
    /// <summary>
    /// No storage.
    /// </summary>
    None = 0,

    /// <summary>
    /// Slot is stored in a global.
    /// </summary>
    Global,

    /// <summary>
    /// Slot is stored in a local variable.
    /// </summary>
    Local,

    /// <summary>
    /// Slot is stored in a closure variable.
    /// </summary>
    Closure,
  }

  /// <summary>
  /// Storage slot that defines how imports, variables and parameters are stored
  /// and accessed.
  /// </summary>
  private sealed class Slot {
    /// <summary>
    /// The scope where the slot is declared.
    /// </summary>
    public LexicalScope Scope { get; set; }

    /// <summary>
    /// The scope that contains the closure holding this slot.
    /// </summary>
    public LexicalScope ClosureScope { get; set; }

    /// <summary>
    /// The references to the slot.
    /// </summary>
    public List<LanguageElement> References { get; } = new List<LanguageElement>();

    /// <summary>
    /// The slot name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The slot kind.
    /// </summary>
    public SlotKind Kind { get; set; }

    /// <summary>
    /// The slot value source.
    /// </summary>
    public SlotSource Source { get; set; }

    /// <summary>
    /// The slot index in the value source.
    /// </summary>
    public int? SourceIndex { get; set; }

    /// <summary>
    /// The slot storage location.
    /// </summary>
    /// <value></value>
    public SlotStorage Storage { get; set; }

    /// <summary>
    /// The slot index in the storage location.
    /// </summary>
    /// <value></value>
    public int? StorageIndex { get; set; }

    /// <summary>
    /// Indicates whether the slot is read-only.
    /// </summary>
    public bool ReadOnly { get; set; }

    /// <summary>
    /// Returns a string representation of the slot.
    /// </summary>
    public override string ToString() {
      var sb = new StringBuilder();

      sb.Append(this.Name);

      if (this.Storage != SlotStorage.None) {
        sb.Append(" = ").Append(this.Storage).Append("(").Append(this.StorageIndex?.ToString() ?? "null").Append(")");
      }

      if (this.Source != SlotSource.None) {
        sb.Append(" <- ").Append(this.Source).Append("(").Append(this.SourceIndex?.ToString() ?? "null").Append(")");
      }

      return sb.ToString();
    }
  }
}
