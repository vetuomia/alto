using System;

/// <summary>
/// The program shell.
/// </summary>
static class Program {
  /// <summary>
  /// Indicates whether the script is running.
  /// </summary>
  private static bool running;

  /// <summary>
  /// Indicates whether to display the source code instead of the assembly.
  /// </summary>
  private static bool displaySource = true;

  /// <summary>
  /// Indicates whether to print the inspector short help.
  /// </summary>
  private static bool displayHelp = true;

  /// <summary>
  /// The inspector display size.
  /// </summary>
  private static int displaySize = 10;

  /// <summary>
  /// The last seen source map.
  /// </summary>
  private static SourceMap lastSeenSourceMap;

  /// <summary>
  /// Indicates whether to skip until hitting a new source line.
  /// </summary>
  private static bool skipUntilNewSourceLine;

  /// <summary>
  /// The main entry point.
  /// </summary>
  /// <param name="args">The command line arguments.</param>
  public static int Main(string[] args) {
    var path = default(string);

    for (var i = 0; i < args.Length; i++) {
      var arg = args[i];

      if (path == null) {
        if (arg.StartsWith("--")) {
          switch (arg) {
            case "--debug":
              Console.Clear();
              Interpreter.Inspector = Inspect;
              break;

            case "--help":
              PrintUsage();
              return 0;

            case "--test":
              UnitTests.Run();
              return 0;

            default:
              Console.WriteLine($"Invalid option: {arg}");
              return 1;
          }
        } else {
          path = arg;
        }
      } else {
        Console.WriteLine($"Invalid argument: {arg}");
        return 1;
      }
    }

    if (path == null) {
      PrintUsage();
      return 1;
    }

    Console.CancelKeyPress += (s, e) => {
      if (Interpreter.Inspector != null) {
        running = false;
        e.Cancel = true;
      }
    };

    try {
      Loader.Load(path);
      return 0;
    } catch (ParseError error) {
      Console.WriteLine(error);
    } catch (Exception error) {
      Console.WriteLine($"ERROR: {error.Message}");

      if (error.GetStackTrace() is string stackTrace) {
        Console.WriteLine(stackTrace);
      }
    }

    return 1;
  }

  /// <summary>
  /// Inspects the interpreter state.
  /// </summary>
  /// <param name="interpreter">The interpreter.</param>
  /// <param name="exception">The thrown exception, if any.</param>
  private static void Inspect(ref Interpreter interpreter, Exception exception) {
    if (running && exception == null) {
      return;
    }

    if (exception != null) {
      skipUntilNewSourceLine = false;
    }

    if (displayHelp) {
      displayHelp = false;
      Console.WriteLine("Type 'go' to continue, 'quit' to exit, 'help' for more commands");
    }

    var paused = true;
    var sourceMap = interpreter.GetSourceMap();

    var skip =
      skipUntilNewSourceLine && // <- skip wanted
      sourceMap?.Source == lastSeenSourceMap?.Source && // same module
      (sourceMap?.Row == null || sourceMap?.Row == lastSeenSourceMap?.Row); // <- no row or same row

    if (skip) {
      paused = false;
    } else {
      skipUntilNewSourceLine = false;
    }

    while (paused) {
      var command = Prompt(ref interpreter, exception);

      switch (command) {
        case "":
          paused = false;
          running = false;
          skipUntilNewSourceLine = displaySource;
          break;

        case "+":
          displaySize = (displaySize == 1) ? 6 : Math.Min(displaySize + 4, 22);
          break;

        case "-":
          displaySize = (displaySize == 6) ? 1 : Math.Max(1, displaySize - 4);
          break;

        case "g":
        case "go":
          paused = false;
          running = true;
          break;

        case "!":
        case "detach":
          paused = false;
          running = false;
          Interpreter.Inspector = null;
          break;

        case "?":
        case "scope":
          Print(ref interpreter, InspectorDataSource.Scope, false);
          break;

        case "v":
        case "view":
          displaySource = !displaySource;
          break;

        case "c":
        case "code":
          InspectCode(ref interpreter);
          break;

        case "a":
        case "all":
          Print(ref interpreter, InspectorDataSource.All, true);
          break;

        case "d":
        case "data":
          Print(ref interpreter, InspectorDataSource.Data, true);
          break;

        case "p":
        case "parameters":
          Print(ref interpreter, InspectorDataSource.Parameters, true);
          break;

        case "s":
        case "stack":
          Print(ref interpreter, InspectorDataSource.Stack, true);
          break;

        case "l":
        case "closure":
          Print(ref interpreter, InspectorDataSource.Closure, true);
          break;

        case "r":
        case "registers":
          Print(ref interpreter, InspectorDataSource.Registers, true);
          break;

        case "e":
        case "exception":
          InspectException(ref interpreter, exception);
          break;

        case "modules":
          InspectModules();
          break;

        case "memory":
          InspectMemoryUsage();
          break;

        case "clear":
          Console.Clear();
          break;

        case "q":
        case "quit":
          Environment.Exit(0);
          break;

        default:
          PrintInspectorHelp();
          break;
      }

      lastSeenSourceMap = sourceMap;
    }
  }

  /// <summary>
  /// Prints the command line usage help.
  /// </summary>
  private static void PrintUsage() {
    Console.WriteLine("Runs an Alto script");
    Console.WriteLine("Usage: alto [options] <file>");
    Console.WriteLine(" ");
    Console.WriteLine("options:");
    Console.WriteLine("  --debug   Enable inspector");
    Console.WriteLine("  --help    Print this help");
    Console.WriteLine("  --test    Runs the self test");
    Console.WriteLine(" ");
  }

  /// <summary>
  /// Prints the inspector help.
  /// </summary>
  private static void PrintInspectorHelp() {
    Console.WriteLine("Inspector commands:");
    Console.WriteLine("  <empty>          Execute the next line");
    Console.WriteLine("  +, -             Adjust the display size");
    Console.WriteLine("  g, go            Continue execution (press Ctrl-C to pause)");
    Console.WriteLine("  !, detach        Detach inspector and continue execution");
    Console.WriteLine("  ?, scope         Print the symbols in current scope");
    Console.WriteLine("  v, view          Toggle between the source and the debug view");
    Console.WriteLine("  c, code          Print the code");
    Console.WriteLine("  a, all           Print all data, arguments and variables");
    Console.WriteLine("  d, data          Print the data");
    Console.WriteLine("  p, parameters    Print the parameters");
    Console.WriteLine("  s, stack         Print the stack");
    Console.WriteLine("  l, closure       Print the closure");
    Console.WriteLine("  e, exception     Print the exception info");
    Console.WriteLine("  r, registers     Print the registers");
    Console.WriteLine("  modules          Print the loaded modules");
    Console.WriteLine("  memory           Print the memory usage");
    Console.WriteLine("  clear            Clear the console");
    Console.WriteLine("  q, quit          Quit the application");
    Console.WriteLine(" ");
    Console.WriteLine("The inspector display at the bottom of the screen shows either");
    Console.WriteLine("the source code or the debug display. When in the source code");
    Console.WriteLine("mode, an arrow at the edge points to the current line of code.");
    Console.WriteLine("");
    Console.WriteLine("When the inspector display is in the debug mode, it shows three");
    Console.WriteLine("columns: the code, the parameters, and the stack. The arrow");
    Console.WriteLine("next to the code points to the next instruction.");
    Console.WriteLine("");
    Console.WriteLine("The prompt displays the current module, with extra '(throw!)'");
    Console.WriteLine("indicating when an exception was thrown.");
    Console.WriteLine(" ");
  }

  /// <summary>
  /// Prints the prompt and the inspector display.
  /// </summary>
  /// <param name="interpeter">The interpreter.</param>
  /// <param name="exception">The thrown exception.</param>
  /// <returns>The entered command.</returns>
  private static string Prompt(ref Interpreter interpreter, Exception exception) {
    var command = string.Empty;

    var x = Console.CursorLeft;
    var y = Console.CursorTop;
    var w = Console.BufferWidth;
    var h = Console.BufferHeight;
    var s = Math.Max(0, y - h + displaySize);
    var displayRows = displaySize - 1;

    // shift output
    {
      Console.SetCursorPosition(0, h - 1);

      for (var i = 0; i < s; i++) {
        Console.WriteLine(" ");
      }
    }

    // print display
    if (displayRows > 0) {
      int CalculateStart(int index, int length) => Math.Max(0, Math.Min(index - (displayRows / 2), length - displayRows));

      Console.SetCursorPosition(0, h - displayRows);

      var src = interpreter.GetSourceMap();

      if (src != null && displaySource) {
        var row = src?.Row ?? lastSeenSourceMap?.Row ?? 0;
        var code = src.Source;

        var scope = w > 100
          ? interpreter
            .GetInspectorData(InspectorDataSource.Parameters | InspectorDataSource.Variables)
            .Take(displayRows)
            .ToArray()
          : null;

        var start = CalculateStart(row, code.Length);
        var format = "{0," + Digits(code.Length - 1) + "} {1}";

        for (var i = 0; i < displayRows; i++) {
          var c = start + i;
          if (c < code.Length) {
            Console.SetCursorPosition(0, h - displayRows + i);

            if (c == row) {
              Console.Write('>');
            } else {
              Console.Write(' ');
            }

            Console.Write(format, c, code[c]);
          }

          if (scope != null && i < scope.Length) {
            Console.SetCursorPosition(80, h - displayRows + i);
            Console.Write(scope[i].Name);
            Console.Write(": ");
            Console.Write(ToString(scope[i].Value));
          }
        }
      } else {
        var code = interpreter.Module.Code;

        var args = w > 50
          ? interpreter
            .GetInspectorData(InspectorDataSource.Parameters, true)
            .Take(displayRows)
            .ToArray()
          : null;

        var vars = w > 100
          ? interpreter
            .GetInspectorData(InspectorDataSource.Variables, true)
            .Take(displayRows)
            .ToArray()
          : null;

        var start = CalculateStart(interpreter.IP, code.Length);
        var format = "{0," + Digits(code.Length - 1) + "} {1}";

        for (var i = 0; i < displayRows; i++) {
          var c = start + i;
          if (c < code.Length) {
            Console.SetCursorPosition(0, h - displayRows + i);

            if (c == interpreter.IP) {
              Console.Write('>');
            } else {
              Console.Write(' ');
            }

            Console.Write(format, c, code[c]);
          }

          if (args != null && i < args.Length) {
            Console.SetCursorPosition(20, h - displayRows + i);
            Console.Write("{0}: {1}", args[i].Name, ToString(args[i].Value, 30));
          }

          if (vars != null && i < vars.Length) {
            Console.SetCursorPosition(65, h - displayRows + i);
            Console.Write("{0}: {1}", vars[i].Name, ToString(vars[i].Value, 30));
          }
        }
      }
    }

    // read command
    {
      Console.SetCursorPosition(0, h - displaySize);

      Console.Write(interpreter.Module.Name);

      var function = interpreter.GetSourceMap()?.Function;
      if (function != null) {
        Console.Write(" ");
        Console.Write(function);
      }

      if (exception == null) {
        Console.Write(": ");
      } else {
        Console.Write(" (throw!): ");
      }

      command = Console.ReadLine().Trim();
    }

    // clear display
    {
      var format = "{0," + (displaySize * w - 1) + "}";

      Console.SetCursorPosition(0, h - displaySize);
      Console.Write(format, ' ');
    }

    Console.SetCursorPosition(x, y - s);
    return command;
  }

  /// <summary>
  /// Inspects the module code.
  /// </summary>
  /// <param name="interpeter">The interpreter.</param>
  private static void InspectCode(ref Interpreter interpreter) {
    var code = interpreter.Module.Code;
    var format = "{0," + Digits(code.Length - 1) + "} {1}";

    SourceMap last = null;

    for (var i = 0; i < code.Length; i++) {
      if (i == interpreter.IP) {
        Console.Write('>');
      } else {
        Console.Write(' ');
      }

      Console.Write(format, i, code[i].ToString());

      var src = interpreter.GetSourceMap(i);

      if (src?.Row != null && src?.Row != last?.Row) {
        Console.Write(" {0}", src.Source[src.Row.Value]);
        last = src;
      }

      Console.WriteLine();
    }
  }

  /// <summary>
  /// Prints the inspector data.
  /// </summary>
  /// <param name="interpeter">The interpreter.</param>
  /// <param name="dataSources">The data sources.</param>
  /// <param name="includeUnnamed">Indicates whether to include the unnamed data.</param>
  private static void Print(ref Interpreter interpreter, InspectorDataSource dataSources, bool includeUnnamed) {
    foreach (var item in interpreter.GetInspectorData(dataSources, includeUnnamed)) {
      Console.Write(item.Name);
      Console.Write(": ");
      Console.WriteLine(ToString(item.Value));
    }
  }

  /// <summary>
  /// Inspects the exception.
  /// </summary>
  /// <param name="interpeter">The interpreter.</param>
  /// <param name="exception">The exception.</param>
  private static void InspectException(ref Interpreter interpreter, Exception exception) {
    if (exception != null) {
      Console.WriteLine("Exception thrown: {0}", exception.Message);
      Console.WriteLine(" ");
    }

    for (var e = interpreter.ExceptionHandler; e != null; e = e.Parent) {
      Console.WriteLine("Exception handler | IP: {0,4} SP: {1}", e.IP, e.SP);
    }
  }

  /// <summary>
  /// Inspects the loaded modules.
  /// </summary>
  private static void InspectModules() {
    var modules = from module in Loader.Modules.Values
                  orderby module.Name
                  select module;

    foreach (var module in modules) {
      Console.WriteLine(module.Name);

      var exports = from export in module.Exports
                    orderby export.Key.ToString()
                    select export;

      foreach (var export in exports) {
        Console.WriteLine("  {0}: {1}", export.Key, export.Value);
      }
    }
  }

  /// <summary>
  /// Inspects the memory usage.
  /// </summary>
  private static void InspectMemoryUsage() {
    Console.WriteLine($"Memory used: {GC.GetTotalMemory(true)} bytes");
  }

  /// <summary>
  /// Converts a value to a string, possibly cutting off the end.
  /// </summary>
  /// <param name="value">The value.</param>
  /// <param name="maxLength">The maximum length.</param>
  private static string ToString(Value value, int maxLength = 0) {
    if (maxLength == 0) {
      maxLength = Console.BufferWidth - Console.CursorLeft;
    }

    maxLength = Math.Max(5, maxLength);

    var isString = value.IsString(out var str);
    string text;

    if (isString) {
      maxLength -= 2;
      text = str;
    } else {
      text = value.ToString();
    }

    if (text.Length > maxLength) {
      text = text.Substring(0, maxLength - 1) + "…";
    }

    if (isString) {
      text = $"'{text}'";
    }

    return text;
  }

  /// <summary>
  /// A quick way to calculate the number of digits in an integer.
  /// </summary>
  /// <param name="number">The number.</param>
  /// <returns>The number of digits in an integer.</returns>
  private static int Digits(int number) {
    if (number < 10) return 1;
    if (number < 100) return 2;
    if (number < 1000) return 3;
    if (number < 10000) return 4;
    if (number < 100000) return 5;
    if (number < 1000000) return 6;
    return 7;
  }
}
