using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

/// <summary>
/// Symbolic assembler for the virtual machine.
/// </summary>
sealed class Assembler {
  /// <summary>
  /// Assembles the source code into a module.
  ///
  /// The assembly language looks like this:
  ///
  /// ```txt
  /// ; This is a comment. Comments can appear anywhere and extend to the
  /// ; end of the line.
  ///
  /// ; These are globals in the data section.
  ///
  /// global  pi          3.14
  /// global  greeting    'Hello world!'
  ///
  /// ; Globals are used for storing doubles, strings and other values that
  /// ; do not fit in the operands. They can also be used as shared global
  /// ; variables.
  ///
  /// ldglob  greeting
  ///
  /// ; These are imports.
  ///
  /// import  Console   'std/console'
  /// import  Util      'util'
  ///
  /// ; They are globals in the data section, but their values are resolved
  /// ; before the module is run. Otherwise, they're exactly same as the other
  /// ; globals.
  ///
  /// ldglob  Console
  ///
  /// ; These are numeric constants that will be embedded in the code.
  ///
  /// const   a   0
  /// const   b   1
  ///
  /// ; Constants are useful when working with local variables and such. There
  /// ; are two predefined constants: false = 0 and true = 1.
  ///
  /// ldvar   a
  ///
  /// ; This is a code label. Labels end with a colon.
  ///
  /// begin:
  ///
  /// ; Labels are used for jumps and defining functions.
  ///
  /// jump    begin
  /// ```
  /// </summary>
  /// <param name="sourceText">The assembly source text.</param>
  public static Module Assemble(string sourceText) {
    var directive = new Regex(@"^(\S+)\s+(\S+)\s+(.+)$");
    var instructions = 0;
    var data = new List();
    var symbols = new Dictionary<string, int>() { ["false"] = 0, ["true"] = 1 };
    var source = new Regex(@"\n|\r\n?").Split(sourceText);

    for (var row = 0; row < source.Length; row++) {
      // Helper for defining symbols
      void define(string name, int value) {
        if (!symbols.TryAdd(name, value)) {
          throw new ParseError($"Duplicate symbol on line {row}: {name}", "", source, row);
        }
      }

      // Remove comments
      if (source[row].IndexOf(';') is int start && start >= 0) {
        source[row] = source[row].Substring(0, start);
      }

      // Remove extra whitespace
      source[row] = source[row].Trim();

      // Resolve globals, constants and labels
      if (source[row].Length > 0) {
        var line = source[row];

        if (line.StartsWith("global")) {
          if (directive.Match(line) is Match match && match.Success) {
            source[row] = string.Empty;

            var name = match.Groups[2].Value;
            var value = match.Groups[3].Value;

            define(name, data.Count);

            if (value == "null") {
              data.Add(default);
            } else if (value == "true") {
              data.Add(true);
            } else if (value == "false") {
              data.Add(false);
            } else if (value.StartsWith('\'') && value.EndsWith('\'')) {
              data.Add(value.Substring(1, value.Length - 2));
            } else if (double.TryParse(value, out var number)) {
              data.Add(number);
            } else {
              throw new ParseError($"Invalid global value: {value}", "", source, row);
            }
          } else {
            throw new ParseError($"Invalid global declaration: {line}", "", source, row);
          }
        } else if (line.StartsWith("import")) {
          if (directive.Match(line) is Match match && match.Success) {
            source[row] = string.Empty;

            var name = match.Groups[2].Value;
            var value = match.Groups[3].Value;

            define(name, data.Count);

            if (value.StartsWith('\'') && value.EndsWith('\'')) {
              data.Add(new Import(value.Substring(1, value.Length - 2)));
            } else {
              throw new ParseError($"Invalid import name: {value}", "", source, row);
            }
          } else {
            throw new ParseError($"Invalid import declaration: {line}", "", source, row);
          }
        } else if (line.StartsWith("const")) {
          if (directive.Match(line) is Match match && match.Success) {
            source[row] = string.Empty;

            var name = match.Groups[2].Value;
            var value = match.Groups[3].Value;

            if (int.TryParse(value, out var number)) {
              define(name, number);
            } else {
              throw new ParseError($"Invalid contant value: {value}", "", source, row);
            }
          } else {
            throw new ParseError($"Invalid constant declaration: {line}", "", source, row);
          }
        } else if (line.IndexOf(':') is int found && found >= 0) {
          var name = line.Substring(0, found).TrimEnd();
          source[row] = line.Substring(found + 1).TrimStart();
          define(name, instructions);
        } else {
          instructions++;
        }
      }
    }

    var code = new List<Instruction>();

    // Write the code
    for (var row = 0; row < source.Length; row++) {
      if (source[row].Length > 0) {
        var line = source[row];
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var mnemonic = parts[0];
        var operands = parts.Length - 1;

        // Helper function for resolving the symbols
        int resolve(string text) {
          if (int.TryParse(text, out var number)) {
            return number;
          } else if (symbols.TryGetValue(text, out var value)) {
            return value;
          } else {
            throw new ParseError($"Unknown symbol: {text}", "", source, row);
          }
        }

        if (AssemblyAttribute.TryGet(mnemonic, out var info)) {
          var opcode = info.Opcode;
          var scope = info.DefaultParam;
          var value = info.DefaultValue;

          if (operands < info.MinOperands) {
            throw new ParseError($"Expected at least {info.MinOperands} operand(s): {line}", "", source, row);
          }

          if (operands > info.MaxOperands) {
            throw new ParseError($"Expected at most {info.MaxOperands} operand(s): {line}", "", source, row);
          }

          switch (operands) {
            case 1 when info.MaxOperands == 1:
            case 1 when info.MaxOperands == 2 && info.ParamIsOptional:
              value = resolve(parts[1]);
              break;

            case 1 when info.MaxOperands == 2 && info.ValueIsOptional:
              scope = resolve(parts[1]);
              break;

            case 2 when info.MaxOperands == 2:
              scope = resolve(parts[1]);
              value = resolve(parts[2]);
              break;
          }

          code.Add(new Instruction(opcode, scope, value));
        } else {
          throw new ParseError($"Unknown mnemonic: {mnemonic}", "", source, row);
        }
      }
    }

    return new Module(code.ToArray(), data.ToArray());
  }
}
