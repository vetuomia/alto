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

    // this.contains(substring) -> boolean
    ["contains"] = new Function((self, args) => self.IsString(out var obj) && args.Arg(0).IsString(out var substring) ? obj.Contains(substring) : false),

    // this.startsWith(prefix) -> boolean
    ["startsWith"] = new Function((self, args) => self.IsString(out var obj) && args.Arg(0).IsString(out var prefix) ? obj.StartsWith(prefix) : false),

    // this.endsWith(suffix) -> boolean
    ["endsWith"] = new Function((self, args) => self.IsString(out var obj) && args.Arg(0).IsString(out var suffix) ? obj.EndsWith(suffix) : false),

    // this.trim() -> string
    ["trim"] = new Function((self, args) => self.IsString(out var obj) ? obj.Trim() : default),

    // this.trimStart() -> string
    ["trimStart"] = new Function((self, args) => self.IsString(out var obj) ? obj.TrimStart() : default),

    // this.trimEnd() -> string
    ["trimEnd"] = new Function((self, args) => self.IsString(out var obj) ? obj.TrimEnd() : default),

    // this.toLower() -> string
    ["toLower"] = new Function((self, args) => self.IsString(out var obj) ? obj.ToLowerInvariant() : default),

    // this.toUpper() -> string
    ["toUpper"] = new Function((self, args) => self.IsString(out var obj) ? obj.ToUpperInvariant() : default),

    // this.toString() -> string
    ["toString"] = new Function((self, args) => self.ToString()),
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
    },

    ["Number"] = new Table {
      ["proto"] = NumberProto,

      // NAN -> number
      ["NAN"] = new Property {
        Value = double.NaN,
      },

      // INFINITY -> number
      ["INFINITY"] = new Property {
        Value = double.PositiveInfinity,
      },

      ["parse"] = new Function((_, args) => {
        if (args.Arg(0).IsString(out var text) && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)) {
          return number;
        }
        return default;
      }),
    },

    ["String"] = new Table {
      ["proto"] = StringProto,

      ["format"] = new Function((_, args) => {
        var format = args.Arg(0).ToString();
        var values = args.ArgSlice(1).Cast<object>().ToArray();
        return string.Format(CultureInfo.InvariantCulture, format, values);
      }),
    },

    ["List"] = new Table {
      ["proto"] = ListProto,
    },

    ["Function"] = new Table {
      ["proto"] = FunctionProto,
    },

    ["Exception"] = new Table {
      ["proto"] = ExceptionProto,
    },

    ["Table"] = new Table {
    },

    ["Console"] = new Table {
      ["beep"] = new Function((_, args) => {
        Console.Beep();
        return default;
      }),

      ["readKey"] = new Function((_, args) => {
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

      ["readLine"] = new Function((_, args) => Console.ReadLine()),

      ["format"] = new Function((_, args) => {
        var format = args.Arg(0).ToString();
        var values = args.ArgSlice(1).Cast<object>().ToArray();
        Console.Write(string.Format(CultureInfo.InvariantCulture, format, values));
        return default;
      }),

      ["formatLine"] = new Function((_, args) => {
        var format = args.Arg(0).ToString();
        var values = args.ArgSlice(1).Cast<object>().ToArray();
        Console.WriteLine(string.Format(CultureInfo.InvariantCulture, format, values));
        return default;
      }),

      ["write"] = new Function((_, args) => {
        foreach (var arg in args) {
          Console.Write(arg.ToString());
        }
        return default;
      }),

      ["writeLine"] = new Function((_, args) => {
        foreach (var arg in args) {
          Console.Write(arg.ToString());
        }
        Console.WriteLine();
        return default;
      }),
    },

    ["Math"] = new Table {
      ["PI"] = new Property {
        Value = Math.PI,
      },
    },
  };

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
    } else if (self.Get(".call").IsFunction(out var dotCall)) {
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
    } else if (self.Get(".call").IsFunction(out var dotCall)) {
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
}
