using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

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
sealed partial class Module {
  /// <summary>
  /// The root directory.
  /// </summary>
  public static string RootDirectory { get; } = Environment.CurrentDirectory;

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
  /// Loads and executes a module.
  /// </summary>
  /// <param name="path">The module path.</param>
  /// <returns>The module exports.</returns>
  public static Table Load(string path) {
    var fullPath = System.IO.Path.GetFullPath(path);

    if (!fullPath.StartsWith(RootDirectory)) {
      throw new IOException($"ERROR: '{fullPath}' is outside the root directory");
    }

    if (!Modules.TryGetValue(fullPath, out var exports)) {
      var sourceText = File.ReadAllText(fullPath);
      var fileName = fullPath.Substring(RootDirectory.Length + 1);
      var module = Compiler.Compile(sourceText, fileName);
      module.Name = fileName;
      module.Path = fullPath;
      module.Importing += Resolve;
      Modules.Add(fullPath, exports = module.Exports);
      module.Main();
    }

    return exports;
  }

  /// <summary>
  /// Resolves a module dependency.
  /// </summary>
  /// <param name="source">The source module.</param>
  /// <param name="import">The module import.</param>
  public static void Resolve(Module source, Import import) {
    if (import.Value.IsNull) {
      var name = import.Name.Replace('\\', '/');
      var path = System.IO.Path.IsPathRooted(name)
        ? RootDirectory + name
        : System.IO.Path.Combine(System.IO.Path.GetDirectoryName(source.Path), name);

      import.Value = Load(path);
    }
  }

  /// <summary>
  /// Runs the module main function.
  /// </summary>
  /// <param name="arguments">The function arguments.</param>
  /// <returns>The result, or null if the function did not produce a result.</returns>
  public Value Main(params Value[] arguments) {
    for (var i = 0; i < this.Data.Length; i++) {
      if (this.Data[i].IsImport(out var import)) {
        if (Modules.TryGetValue(import.Name, out var exports)) {
          import.Value = exports;
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
