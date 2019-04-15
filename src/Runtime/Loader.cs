using System;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Module loader.
/// </summary>
static class Loader {
  /// <summary>
  /// The loaded modules.
  /// </summary>
  public static Dictionary<string, Module> Modules { get; } = new Dictionary<string, Module>();

  /// <summary>
  /// The root directory.
  /// </summary>
  public static string RootDirectory { get; } = Environment.CurrentDirectory;

  /// <summary>
  /// Loads and executes a module.
  /// </summary>
  /// <param name="path">The module path.</param>
  /// <returns>The module.</returns>
  public static Module Load(string path) {
    var fullPath = Path.GetFullPath(path);

    if (!fullPath.StartsWith(RootDirectory)) {
      throw new IOException($"ERROR: '{fullPath}' is outside the root directory");
    }

    if (!Modules.TryGetValue(fullPath, out var module)) {
      var sourceText = File.ReadAllText(fullPath);
      var fileName = fullPath.Substring(RootDirectory.Length + 1);
      module = Compiler.Compile(sourceText, fileName);
      module.Name = fileName;
      module.Path = fullPath;
      module.Importing += Resolve;
      Modules.Add(fullPath, module);
      module.Main();
    }

    return module;
  }

  /// <summary>
  /// Resolves a module dependency.
  /// </summary>
  /// <param name="source">The source module.</param>
  /// <param name="import">The module import.</param>
  public static void Resolve(Module source, Import import) {
    if (import.Value.IsNull) {
      var name = import.Name.Replace('\\', '/');
      var path = Path.IsPathRooted(name)
        ? RootDirectory + name
        : Path.Combine(Path.GetDirectoryName(source.Path), name);

      var module = Load(path);
      import.Value = module.Exports;
    }
  }
}
