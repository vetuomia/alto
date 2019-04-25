using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

/// <summary>
/// The core runtime.
/// </summary>
static class Core {
  /// <summary>
  /// Boolean prototype.
  /// </summary>
  public static readonly Table BooleanProto = new Table {
    // this.toString() -> string
    ["toString"] = new Function((self, args) => self.ToString()),
  };

  /// <summary>
  /// Number prototype.
  /// </summary>
  public static readonly Table NumberProto = new Table {
    // this.isNAN() -> boolean
    ["isNAN"] = Method((double self) => double.IsNaN(self)),

    // this.isFinite() -> boolean
    ["isFinite"] = Method((double self) => double.IsFinite(self)),

    // this.isInfinity() -> boolean
    ["isInfinity"] = Method((double self) => double.IsInfinity(self)),

    // this.toString() -> string
    ["toString"] = new Function((self, args) => self.ToString()),
  };

  /// <summary>
  /// String prototype.
  /// </summary>
  public static readonly Table StringProto = new Table {
    // this.length -> number
    ["length"] = new Property {
      Get = (self) => self.IsString(out var obj) ? obj.Length : default,
    },

    // this.isEmpty() -> boolean
    ["isEmpty"] = Method((string self) => self.Length == 0),

    // this.contains(substring) -> boolean
    ["contains"] = Method((string self, string substring) => self.Contains(substring)),

    // this.startsWith(prefix) -> boolean
    ["startsWith"] = Method((string self, string prefix) => self.StartsWith(prefix)),

    // this.endsWith(suffix) -> boolean
    ["endsWith"] = Method((string self, string suffix) => self.EndsWith(suffix)),

    // this.trim() -> string
    ["trim"] = Method((string self) => self.Trim()),

    // this.trimStart() -> string
    ["trimStart"] = Method((string self) => self.TrimStart()),

    // this.trimEnd() -> string
    ["trimEnd"] = Method((string self) => self.TrimEnd()),

    // this.toLower() -> string
    ["toLower"] = Method((string self) => self.ToLowerInvariant()),

    // this.toUpper() -> string
    ["toUpper"] = Method((string self) => self.ToUpperInvariant()),

    // this.toString() -> string
    ["toString"] = Method((string self) => self),
  };

  /// <summary>
  /// List prototype.
  /// </summary>
  public static readonly Table ListProto = new Table {
    // this.length -> number
    ["length"] = new Property {
      Get = (self) => self.IsList(out var obj) ? obj.Count : default,
    },

    // this.add(arg0, arg1, ..., argN) -> null
    ["add"] = new Function((self, args) => {
      if (self.IsList(out var obj)) {
        obj.AddRange(args);
      }
      return default;
    }),

    // this.forEach(fn: (value, index?) -> void) -> void
    ["forEach"] = new Function((self, args) => {
      var fn = args.Arg(0);

      if (self.IsList(out var obj) && fn != default) {
        for (var i = 0; i < obj.Count; i++) {
          fn.Call(default, new Value[] { obj[i], i });
        }
      }

      return default;
    }),

    // this.toString() -> string
    ["toString"] = new Function((self, args) => self.ToString()),
  };

  /// <summary>
  /// Function prototype.
  /// </summary>
  public static readonly Table FunctionProto = new Table {
    // this.bind(receiver) -> function
    ["bind"] = new Function((self, args) => new Function((_, args2) => self.Call(args.Arg(0), args2))),

    // this.call(receiver, arg0, arg1, ..., argN) -> result
    ["call"] = new Function((self, args) => self.Call(args.Arg(0), args.ArgSlice(1))),

    // this.apply(receiver, argList) -> result
    ["apply"] = new Function((self, args) => self.Apply(args.Arg(0), args.Arg(1))),

    // this.toString() -> string
    ["toString"] = new Function((self, args) => self.ToString()),
  };

  /// <summary>
  /// Exception prototype.
  /// </summary>
  public static readonly Table ExceptionProto = new Table {
    // this.message -> string
    ["message"] = new Property {
      Get = (self) => self.IsException(out var obj) ? obj.Message : default,
    },

    // this.stackTrace -> string
    ["stackTrace"] = new Property {
      Get = (self) => self.IsException(out var obj) ? obj.GetStackTrace() : default,
    },

    // this.value -> any
    ["value"] = new Property {
      Get = (self) => self.IsException(out var obj) ? obj.ToValue() : default,
    },

    // this.toString() -> string
    ["toString"] = new Function((self, args) => self.ToString()),
  };

  /// <summary>
  /// The core modules.
  /// </summary>
  public static readonly Dictionary<string, Table> Modules = new Dictionary<string, Table> {
    ["Boolean"] = new Table {
      ["proto"] = BooleanProto,

      // Boolean(value) -> boolean
      [Callable] = Static((Value[] args) => args.Arg(0).ToBoolean()),

      // parse(text) -> boolean | null
      ["parse"] = Static((string text) => bool.TryParse(text, out var result) ? result : default),
    },

    ["Number"] = new Table {
      ["proto"] = NumberProto,

      // Number(value) -> number
      [Callable] = Static((Value[] args) => args.Arg(0).ToNumber()),

      // NAN -> number
      ["NAN"] = new Property { Value = double.NaN },

      // INFINITY -> number
      ["INFINITY"] = new Property { Value = double.PositiveInfinity },

      // parse(text) -> number | null
      ["parse"] = Static((string text) => double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : default),
    },

    ["String"] = new Table {
      ["proto"] = StringProto,

      // String(...values) -> string
      [Callable] = Static((Value[] args) => string.Concat(args.Select(i => i.ToString()))),

      // format(format, ...values) -> string
      ["format"] = Static((Value[] args) => {
        var format = args.Arg(0).ToString();
        var values = args.ArgSlice(1).Cast<object>().ToArray();
        return string.Format(CultureInfo.InvariantCulture, format, values);
      }),
    },

    ["List"] = new Table {
      ["proto"] = ListProto,

      // List(...values) -> List
      [Callable] = Static((Value[] args) => new List(args)),
    },

    ["Function"] = new Table {
      ["proto"] = FunctionProto,
    },

    ["Exception"] = new Table {
      ["proto"] = ExceptionProto,

      // Exception(value) -> Exception
      [Callable] = Static((Value[] args) => args.Arg(0).ToException()),
    },

    ["Table"] = new Table {
      // keys(obj) -> List
      ["keys"] = Static((Value[] args) => args.Arg(0).IsTable(out var obj) ? new List(obj.Keys) : new List()),
    },

    ["Console"] = new Table {
      // beep()
      ["beep"] = Static(Console.Beep),

      // readkey(intercept?) -> {
      //   key:     string,
      //   keyCode: number,
      //   keyChar: string,
      //   alt:     boolean,
      //   shift:   control,
      //  } | null
      ["readKey"] = Static((Value[] args) => {
        var key = Console.ReadKey(args.Arg(0).ToBoolean());
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
      ["readLine"] = Static(() => Console.ReadLine()),

      // format(format, ...values)
      ["format"] = Static((Value[] args) => {
        var format = args.Arg(0).ToString();
        var values = args.ArgSlice(1).Cast<object>().ToArray();
        Console.Write(string.Format(CultureInfo.InvariantCulture, format, values));
      }),

      // formatLine(format, ...values)
      ["formatLine"] = Static((Value[] args) => {
        var format = args.Arg(0).ToString();
        var values = args.ArgSlice(1).Cast<object>().ToArray();
        Console.WriteLine(string.Format(CultureInfo.InvariantCulture, format, values));
      }),

      // write(...values)
      ["write"] = Static((Value[] args) => {
        foreach (var arg in args) {
          Console.Write(arg.ToString());
        }
      }),

      // writeLine(...values)
      ["writeLine"] = Static((Value[] args) => {
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
  /// Key for callable objects.
  /// </summary>
  private const string Callable = ".call";

  /// <summary>
  /// Data key for a value wrapped in an exception.
  /// </summary>
  private const string ValueDataKey = ".value";

  /// <summary>
  /// Data key for a custom stack trace in an exception.
  /// </summary>
  private const string StackTraceDataKey = ".stackTrace";

  /// <summary>
  /// Gets an argument value.
  /// </summary>
  /// <param name="arguments">The arguments.</param>
  /// <param name="index">The argument index.</param>
  public static Value Arg(this Value[] arguments, int index) {
    Debug.Assert(0 <= index);
    return (index < arguments.Length) ? arguments[index] : default;
  }

  /// <summary>
  /// Gets an argument array slice.
  /// </summary>
  /// <param name="arguments">The arguments.</param>
  /// <param name="start">The starting index.</param>
  public static Value[] ArgSlice(this Value[] arguments, int start) {
    Debug.Assert(0 <= start);

    var count = arguments.Length - start;

    if (count > 0) {
      var slice = new Value[count];
      Array.Copy(arguments, start, slice, 0, count);
      return slice;
    } else {
      return Array.Empty<Value>();
    }
  }

  /// <summary>
  /// Converts an exception to a value.
  /// </summary>
  /// <param name="self">The exception.</param>
  /// <returns>The value.</returns>
  public static Value ToValue(this Exception self) => (self.Data[ValueDataKey] is Value value) ? value : self;

  /// <summary>
  /// Converts a value to an exception.
  /// </summary>
  /// <param name="self">The value.</param>
  /// <returns>The exception.</returns>
  public static Exception ToException(this Value self) => self.IsException(out var exception) ? exception : new Exception(self.ToString()) { Data = { [ValueDataKey] = self } };

  /// <summary>
  /// Gets the custom stack trace.
  /// </summary>
  /// <param name="self">The exception.</param>
  /// <returns>The custom stack trace or null if not set.</returns>
  public static string GetStackTrace(this Exception self) => self.Data[StackTraceDataKey] as string;

  /// <summary>
  /// Sets the custom stack trace.
  /// </summary>
  /// <param name="self">The exception.</param>
  /// <param name="stackTrace">The custom stack trace.</param>
  public static void SetStackTrace(this Exception self, string stackTrace) => self.Data[StackTraceDataKey] = stackTrace;

  /// <summary>
  /// Gets a member or an element from the value.
  /// </summary>
  /// <param name="self">The object.</param>
  /// <param name="key">The member or element key.</param>
  public static Value Get(this Value self, Value key) {
    Table proto = null;

    if (self.IsTable(out var table)) {
      if (table.TryGetValue(key, out var value)) {
        if (value.IsProperty(out var property)) {
          if (property.Get != null) {
            return property.Get(self); // Get property value
          }
          return property.Value;
        }
        return value; // Value found
      }
      return default; // No value
    } else if (self.IsList(out var list)) {
      if (key.IsNumber(out var number)) {
        var index = (int)number;
        if (0 <= index && index < list.Count) {
          return list[index]; // Value found
        }
        return default; // Out of bounds
      }
      proto = ListProto;
    } else if (self.IsException(out _)) {
      proto = ExceptionProto;
    } else if (self.IsFunction(out _)) {
      proto = FunctionProto;
    } else if (self.IsString(out _)) {
      proto = StringProto;
    } else if (self.IsNumber(out _)) {
      proto = NumberProto;
    } else if (self.IsBoolean(out _)) {
      proto = BooleanProto;
    }

    if (proto != null) {
      if (proto.TryGetValue(key, out var value)) {
        if (value.IsProperty(out var property)) {
          if (property.Get != null) {
            return property.Get(self); // Get property value
          }
          return default; // Has property, no getter
        }
        return value; // Value found
      }
    }

    return default; // No value
  }

  /// <summary>
  /// Sets a member or an element for the value.
  /// </summary>
  /// <param name="self">The object.</param>
  /// <param name="key">The member or element key.</param>
  /// <param name="value">The member or element value.</param>
  public static void Set(this Value self, Value key, Value value) {
    Table proto = null;

    if (self.IsTable(out var table)) {
      if (table.TryGetValue(key, out var found) && found.IsProperty(out var property)) {
        if (property.Set != null) {
          property.Set(self, value);
        } else {
          return; // Has property, no setter.
        }
      } else {
        table[key] = value;
      }
      return;
    } else if (self.IsList(out var list)) {
      if (key.IsNumber(out var number)) {
        var index = (int)number;
        if (0 <= index && index < list.Count) {
          list[index] = value;
        }
        return;
      }
      proto = ListProto;
    } else if (self.IsException(out _)) {
      proto = ExceptionProto;
    } else if (self.IsFunction(out _)) {
      proto = FunctionProto;
    } else if (self.IsString(out _)) {
      proto = StringProto;
    } else if (self.IsNumber(out _)) {
      proto = NumberProto;
    } else if (self.IsBoolean(out _)) {
      proto = BooleanProto;
    }

    if (proto != null) {
      if (proto.TryGetValue(key, out var found) && found.IsProperty(out var property)) {
        if (property.Set != null) {
          property.Set(self, value);
        }
      }
    }
  }

  /// <summary>
  /// Calls a function.
  /// </summary>
  /// <param name="self">The object.</param>
  /// <param name="receiver">The receiver.</param>
  /// <param name="arguments">The arguments.</param>
  public static Value Call(this Value self, Value receiver, Value[] arguments) {
    if (self.IsFunction(out var function)) {
      return function(receiver, arguments);
    } else if (self.Get(Callable).IsFunction(out var dotCall)) {
      return dotCall(receiver, arguments);
    } else {
      throw new Exception($"'{self}' is not a function");
    }
  }

  /// <summary>
  /// Calls a function with an argument list.
  /// </summary>
  /// <param name="self">The object.</param>
  /// <param name="receiver">The receiver.</param>
  /// <param name="argumentList">The argument list.</param>
  public static Value Apply(this Value self, Value receiver, Value argumentList) {
    Function target;

    if (self.IsFunction(out var function)) {
      target = function;
    } else if (self.Get(Callable).IsFunction(out var dotCall)) {
      target = dotCall;
    } else {
      throw new Exception($"'{self}' is not a function");
    }

    if (argumentList.IsList(out var arguments)) {
      return target(receiver, arguments.ToArray());
    } else {
      throw new Exception($"'{argumentList}' is not a list");
    }
  }

  /// <summary>
  /// Wraps a method delegate into a <see cref="Function" /> instance.
  /// </summary>
  /// <param name="fn">The function delegate.</param>
  /// <returns>The <see cref="Function" /> instance.</returns>
  private static Function Method(Func<double, bool> fn) => (self, args) => self.IsNumber(out var obj) ? fn(obj) : false;

  /// <summary>
  /// Wraps a method delegate into a <see cref="Function" /> instance.
  /// </summary>
  /// <param name="fn">The function delegate.</param>
  /// <returns>The <see cref="Function" /> instance.</returns>
  private static Function Method(Func<string, bool> fn) => (self, args) => self.IsString(out var obj) ? fn(obj) : false;

  /// <summary>
  /// Wraps a method delegate into a <see cref="Function" /> instance.
  /// </summary>
  /// <param name="fn">The function delegate.</param>
  /// <returns>The <see cref="Function" /> instance.</returns>
  private static Function Method(Func<string, string, bool> fn) => (self, args) => self.IsString(out var obj) && args.Arg(0).IsString(out var arg0) ? fn(obj, arg0) : false;

  /// <summary>
  /// Wraps a method delegate into a <see cref="Function" /> instance.
  /// </summary>
  /// <param name="fn">The function delegate.</param>
  /// <returns>The <see cref="Function" /> instance.</returns>
  private static Function Method(Func<string, Value> fn) => (self, args) => self.IsString(out var obj) ? fn(obj) : default;

  /// <summary>
  /// Wraps a static delegate into a <see cref="Function" /> instance.
  /// </summary>
  /// <param name="fn">The function delegate.</param>
  /// <returns>The <see cref="Function" /> instance.</returns>
  private static Function Static(Action fn) => (_, args) => { fn(); return default; };

  /// <summary>
  /// Wraps a static delegate into a <see cref="Function" /> instance.
  /// </summary>
  /// <param name="fn">The function delegate.</param>
  /// <returns>The <see cref="Function" /> instance.</returns>
  private static Function Static(Action<Value[]> fn) => (_, args) => { fn(args); return default; };

  /// <summary>
  /// Wraps a static delegate into a <see cref="Function" /> instance.
  /// </summary>
  /// <param name="fn">The function delegate.</param>
  /// <returns>The <see cref="Function" /> instance.</returns>
  private static Function Static(Func<Value> fn) => (_, args) => fn();

  /// <summary>
  /// Wraps a static delegate into a <see cref="Function" /> instance.
  /// </summary>
  /// <param name="fn">The function delegate.</param>
  /// <returns>The <see cref="Function" /> instance.</returns>
  private static Function Static(Func<Value[], Value> fn) => (_, args) => fn(args);

  /// <summary>
  /// Wraps a static delegate into a <see cref="Function" /> instance.
  /// </summary>
  /// <param name="fn">The function delegate.</param>
  /// <returns>The <see cref="Function" /> instance.</returns>
  private static Function Static(Func<string, Value> fn) => (_, args) => args.Arg(0).IsString(out var obj) ? fn(obj) : default;
}
