using System;
using System.Diagnostics;

/// <summary>
/// Module holds both the executable code and the global data needed by the
/// code. Each module has an implicit main function, starting at the address
/// zero.
///
/// Modules can import external dependencies through import references. The
/// imports are stored in the data section and accessed just like any other
/// global data. The imports must be resolved before the code can be run.
/// Modules can export values through their exports table, making the them
/// available for other modules to import.
///
/// Module does not automatically implement any actual import/export resolution
/// logic. The resolution logic must be injected into the module through the
/// <see cref="Module.Importing" /> event.
/// </summary>
sealed class Module {
  /// <summary>
  /// The executable code.
  /// </summary>
  public Instruction[] Code { get; }

  /// <summary>
  /// The global data section.
  /// </summary>
  public Value[] Data { get; }

  /// <summary>
  /// The source map.
  /// </summary>
  public SourceMap[] SourceMap { get; }

  /// <summary>
  /// The module exports.
  /// </summary>
  public Table Exports { get; } = new Table();

  /// <summary>
  /// The module name.
  /// </summary>
  public string Name { get; set; }

  /// <summary>
  /// The module path.
  /// </summary>
  public string Path { get; set; }

  /// <summary>
  /// Occurs when the module is resolving an import.
  /// </summary>
  public event Action<Module, Import> Importing;

  /// <summary>
  /// Initializes a new instance of the class.
  /// </summary>
  /// <param name="code">The executable code.</param>
  /// <param name="data">The global data section.</param>
  /// <param name="sourceMap">The source map.</param>
  public Module(Instruction[] code, Value[] data, SourceMap[] sourceMap = null) {
    this.Code = code ?? throw new ArgumentNullException(nameof(code));
    this.Data = data ?? throw new ArgumentNullException(nameof(data));
    this.SourceMap = sourceMap;
    Debug.Assert(this.SourceMap == null ||this.Code.Length == this.SourceMap.Length);
  }

  /// <summary>
  /// Runs the module main function.
  /// </summary>
  /// <param name="arguments">The function arguments.</param>
  /// <returns>The result, or null if the function did not produce a result.</returns>
  public Value Main(params Value[] arguments) {
    for (var i = 0; i < this.Data.Length; i++) {
      if (this.Data[i].IsImport(out var import)) {
        if (Core.Modules.TryGetValue(import.Name, out var module)) {
          import.Value = module;
        } else {
          this.Importing?.Invoke(this, import);
        }

        if (import.Value.IsNull) {
          throw new Exception($"Could not resolve import: {import.Name}");
        } else {
          this.Data[i] = import.Value;
        }
      }
    }

    return new Interpreter(this, this.Exports, arguments).Run();
  }
}
