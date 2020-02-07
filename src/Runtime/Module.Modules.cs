using System;
using System.Collections.Generic;
using System.Globalization;

sealed partial class Module {
  /// <summary>
  /// The available modules.
  /// </summary>
  public static readonly Dictionary<string, Table> Modules = new Dictionary<string, Table> {
    ["Boolean"] = new Table {
      ["prototype"] = Value.BooleanProto,

      // Boolean(value) -> boolean
      [Value.Callable] = Fn((Value[] args) => Value.At(args, 0).ToBoolean()),

      // parse(text) -> boolean | null
      ["parse"] = Fn((string text) => bool.TryParse(text, out var result) ? result : default),
    },

    ["Number"] = new Table {
      ["prototype"] = Value.NumberProto,

      // Number(value) -> number
      [Value.Callable] = Fn((Value[] args) => Value.At(args, 0).ToNumber()),

      // NAN -> number
      ["NAN"] = new Property { Value = double.NaN },

      // INFINITY -> number
      ["INFINITY"] = new Property { Value = double.PositiveInfinity },

      // parse(text) -> number | null
      ["parse"] = Fn((string text) => double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : default),
    },

    ["String"] = new Table {
      ["prototype"] = Value.StringProto,

      // String(...values) -> string
      [Value.Callable] = Fn((Value[] args) => string.Concat(args.Select(i => i.ToString()))),

      // format(format, ...values) -> string
      ["format"] = Fn((Value[] args) => {
        var format = Value.At(args, 0).ToString();
        var values = Value.SliceAt(args, 1).Select(i => i.ToString()).ToArray();
        return string.Format(CultureInfo.InvariantCulture, format, values);
      }),
    },

    ["List"] = new Table {
      ["prototype"] = Value.ListProto,

      // List(...values) -> List
      [Value.Callable] = Fn((Value[] args) => new List(args)),
    },

    ["Function"] = new Table {
      ["prototype"] = Value.FunctionProto,
    },

    ["Exception"] = new Table {
      ["prototype"] = Value.ExceptionProto,

      // Exception(value) -> Exception
      [Value.Callable] = Fn((Value[] args) => Value.At(args, 0).ValueToException()),
    },

    ["Table"] = new Table {
      // keys(obj) -> List
      ["keys"] = Fn((Value[] args) => Value.At(args, 0).IsTable(out var obj) ? new List(obj.Keys) : new List()),
    },

    ["Console"] = new Table {
      // beep()
      ["beep"] = Fn(Console.Beep),

      // readkey(intercept?) -> {
      //   key:     string,
      //   keyCode: number,
      //   keyChar: string,
      //   alt:     boolean,
      //   shift:   control,
      //  } | null
      ["readKey"] = Fn((Value[] args) => {
        var key = Console.ReadKey(Value.At(args, 0).ToBoolean());
        return key != null
          ? new Table {
            ["key"] = key.Key.ToString(),
            ["keyCode"] = (int)key.Key,
            ["keyChar"] = key.KeyChar.ToString(),
            ["alt"] = key.Modifiers.HasFlag(ConsoleModifiers.Alt),
            ["shift"] = key.Modifiers.HasFlag(ConsoleModifiers.Shift),
            ["control"] = key.Modifiers.HasFlag(ConsoleModifiers.Control),
          }
          : default;
      }),

      // readLine() -> string
      ["readLine"] = Fn(() => Console.ReadLine()),

      // format(format, ...values)
      ["format"] = Fn((Value[] args) => {
        var format = Value.At(args, 0).ToString();
        var values = Value.SliceAt(args, 1).Select(i => i.ToString()).ToArray();
        Console.Write(string.Format(CultureInfo.InvariantCulture, format, values));
      }),

      // formatLine(format, ...values)
      ["formatLine"] = Fn((Value[] args) => {
        var format = Value.At(args, 0).ToString();
        var values = Value.SliceAt(args, 1).Select(i => i.ToString()).ToArray();
        Console.WriteLine(string.Format(CultureInfo.InvariantCulture, format, values));
      }),

      // write(...values)
      ["write"] = Fn((Value[] args) => {
        foreach (var arg in args) {
          Console.Write(arg.ToString());
        }
      }),

      // writeLine(...values)
      ["writeLine"] = Fn((Value[] args) => {
        foreach (var arg in args) {
          Console.Write(arg.ToString());
        }
        Console.WriteLine();
      }),
    },

    ["Math"] = new Table {
      // PI -> number
      ["PI"] = new Property { Value = Math.PI },
    },
  };

  /// <summary>
  /// Wraps a static delegate into a <see cref="Function" /> instance.
  /// </summary>
  /// <param name="fn">The function delegate.</param>
  /// <returns>The <see cref="Function" /> instance.</returns>
  private static Function Fn(Action fn) => (_, args) => { fn(); return default; };

  /// <summary>
  /// Wraps a static delegate into a <see cref="Function" /> instance.
  /// </summary>
  /// <param name="fn">The function delegate.</param>
  /// <returns>The <see cref="Function" /> instance.</returns>
  private static Function Fn(Action<Value[]> fn) => (_, args) => { fn(args); return default; };

  /// <summary>
  /// Wraps a static delegate into a <see cref="Function" /> instance.
  /// </summary>
  /// <param name="fn">The function delegate.</param>
  /// <returns>The <see cref="Function" /> instance.</returns>
  private static Function Fn(Func<Value> fn) => (_, args) => fn();

  /// <summary>
  /// Wraps a static delegate into a <see cref="Function" /> instance.
  /// </summary>
  /// <param name="fn">The function delegate.</param>
  /// <returns>The <see cref="Function" /> instance.</returns>
  private static Function Fn(Func<Value[], Value> fn) => (_, args) => fn(args);

  /// <summary>
  /// Wraps a static delegate into a <see cref="Function" /> instance.
  /// </summary>
  /// <param name="fn">The function delegate.</param>
  /// <returns>The <see cref="Function" /> instance.</returns>
  private static Function Fn(Func<string, Value> fn) => (_, args) => Value.At(args, 0).IsString(out var obj) ? fn(obj) : default;
}
