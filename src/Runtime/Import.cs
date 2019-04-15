using System;

/// <summary>
/// Import reference, referring to an external dependency.
/// </summary>
sealed class Import {
  /// <summary>
  /// The import name.
  /// </summary>
  public string Name { get; }

  /// <summary>
  /// The resolved value.
  /// </summary>
  public Value Value { get; set; }

  /// <summary>
  /// Initializes a new instance of the class.
  /// </summary>
  /// <param name="name">The import name.</param>
  public Import(string name) => this.Name = name ?? throw new ArgumentNullException(nameof(name));
}
