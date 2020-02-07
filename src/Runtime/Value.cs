using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

/// <summary>
/// Tagged union for holding different types of values.
///
/// Value can hold one of these:
/// - null
/// - boolean
/// - number
/// - string
/// - list
/// - table
/// - function
/// - import
/// - property
/// - exception
/// </summary>
readonly struct Value : IEquatable<Value> {
  /// <summary>
  /// Key for callable values.
  /// </summary>
  public const string Callable = ".call";

  /// <summary>
  /// Boolean prototype.
  /// </summary>
  public static readonly Table BooleanProto = new Table {
  };

  /// <summary>
  /// Number prototype.
  /// </summary>
  public static readonly Table NumberProto = new Table {
    // this.isNAN() -> boolean
    ["isNAN"] = Fn((double self) => double.IsNaN(self)),

    // this.isFinite() -> boolean
    ["isFinite"] = Fn((double self) => double.IsFinite(self)),

    // this.isInfinity() -> boolean
    ["isInfinity"] = Fn((double self) => double.IsInfinity(self)),
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
    ["isEmpty"] = Fn((string self) => self.Length == 0),

    // this.contains(substring) -> boolean
    ["contains"] = Fn((string self, string substring) => self.Contains(substring)),

    // this.startsWith(prefix) -> boolean
    ["startsWith"] = Fn((string self, string prefix) => self.StartsWith(prefix)),

    // this.endsWith(suffix) -> boolean
    ["endsWith"] = Fn((string self, string suffix) => self.EndsWith(suffix)),

    // this.trim() -> string
    ["trim"] = Fn((string self) => self.Trim()),

    // this.trimStart() -> string
    ["trimStart"] = Fn((string self) => self.TrimStart()),

    // this.trimEnd() -> string
    ["trimEnd"] = Fn((string self) => self.TrimEnd()),

    // this.toLower() -> string
    ["toLower"] = Fn((string self) => self.ToLowerInvariant()),

    // this.toUpper() -> string
    ["toUpper"] = Fn((string self) => self.ToUpperInvariant()),
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
      var fn = Value.At(args, 0);

      if (self.IsList(out var obj) && fn != default) {
        for (var i = 0; i < obj.Count; i++) {
          fn.Call(default, new Value[] { obj[i], i });
        }
      }

      return default;
    }),
  };

  /// <summary>
  /// Function prototype.
  /// </summary>
  public static readonly Table FunctionProto = new Table {
    // this.bind(receiver) -> function
    ["bind"] = new Function((self, args) => new Function((_, args2) => self.Call(Value.At(args, 0), args2))),

    // this.call(receiver, arg0, arg1, ..., argN) -> result
    ["call"] = new Function((self, args) => self.Call(Value.At(args, 0), Value.SliceAt(args, 1))),

    // this.apply(receiver, argList) -> result
    ["apply"] = new Function((self, args) => self.Apply(Value.At(args, 0), Value.At(args, 1))),
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
      Get = (self) => self.IsException(out var obj) ? obj.ExceptionToValue() : default,
    },
  };

  /// <summary>
  /// Marker object that indicates that this object holds a boolean.
  /// </summary>
  private static readonly object TypeIsBoolean = new object();

  /// <summary>
  /// Marker object that indicates that this object holds a double.
  /// </summary>
  private static readonly object TypeIsDouble = new object();

  /// <summary>
  /// Union that holds the value types.
  /// </summary>
  private readonly Union val;

  /// <summary>
  /// Reference to the held object or a marker object.
  /// </summary>
  private readonly object obj;

  /// <summary>
  /// Initializes a new instance of the struct.
  /// </summary>
  /// <param name="value">The value.</param>
  public Value(bool value) : this() {
    this.val.Boolean = value;
    this.obj = TypeIsBoolean;
  }

  /// <summary>
  /// Initializes a new instance of the struct.
  /// </summary>
  /// <param name="value">The value.</param>
  public Value(double value) : this() {
    this.val.Double = value;
    this.obj = TypeIsDouble;
  }

  /// <summary>
  /// Initializes a new instance of the struct.
  /// </summary>
  /// <param name="value">The value.</param>
  public Value(string value) : this() => this.obj = value;

  /// <summary>
  /// Initializes a new instance of the struct.
  /// </summary>
  /// <param name="value">The value.</param>
  public Value(List value) : this() => this.obj = value;

  /// <summary>
  /// Initializes a new instance of the struct.
  /// </summary>
  /// <param name="value">The value.</param>
  public Value(Table value) : this() => this.obj = value;

  /// <summary>
  /// Initializes a new instance of the struct.
  /// </summary>
  /// <param name="value">The value.</param>
  public Value(Function value) : this() => this.obj = value;

  /// <summary>
  /// Initializes a new instance of the struct.
  /// </summary>
  /// <param name="value">The value.</param>
  public Value(Import value) : this() => this.obj = value;

  /// <summary>
  /// Initializes a new instance of the struct.
  /// </summary>
  /// <param name="value">The value.</param>
  public Value(Property value) : this() => this.obj = value;

  /// <summary>
  /// Initializes a new instance of the struct.
  /// </summary>
  /// <param name="value">The value.</param>
  public Value(Exception value) : this() => this.obj = value;

  /// <summary>
  /// Determines whether the value is null.
  /// </summary>
  public bool IsNull => this.obj == null;

  /// <summary>
  /// Returns the value at the given index.
  /// </summary>
  /// <param name="values">The values array.</param>
  /// <param name="index">The array index.</param>
  /// <returns>The value or default if the index is out of bounds.</returns>
  public static Value At(Value[] values, int index) {
    Debug.Assert(0 <= index);
    return values != null && index < values.Length ? values[index] : default;
  }

  /// <summary>
  /// Returns the slice starting at the given index.
  /// </summary>
  /// <param name="values">The values array.</param>
  /// <param name="index">The array index.</param>
  /// <returns>The slice or an empty array if the index is out of bounds.</returns>
  public static Value[] SliceAt(Value[] values, int index) {
    Debug.Assert(0 <= index);

    if (values != null) {
      var count = values.Length - index;

      if (count > 0) {
        var slice = new Value[count];
        Array.Copy(values, index, slice, 0, count);
        return slice;
      }
    }

    return Array.Empty<Value>();
  }

  /// <summary>
  /// Determines whether the value is a boolean.
  /// </summary>
  /// <param name="value">Output parameter for the value.</param>
  public bool IsBoolean(out bool value) {
    value = this.val.Boolean;
    return (this.obj == TypeIsBoolean);
  }

  /// <summary>
  /// Determines whether the value is a number.
  /// </summary>
  /// <param name="value">Output parameter for the value.</param>
  public bool IsNumber(out double value) {
    value = this.val.Double;
    return (this.obj == TypeIsDouble);
  }

  /// <summary>
  /// Determines whether the value is a string.
  /// </summary>
  /// <param name="value">Output parameter for the value.</param>
  public bool IsString(out string value) => (value = this.obj as string) != null;

  /// <summary>
  /// Determines whether the value is a list.
  /// </summary>
  /// <param name="value">Output parameter for the value.</param>
  public bool IsList(out List value) => (value = this.obj as List) != null;

  /// <summary>
  /// Determines whether the value is a table.
  /// </summary>
  /// <param name="value">Output parameter for the value.</param>
  public bool IsTable(out Table value) => (value = this.obj as Table) != null;

  /// <summary>
  /// Determines whether the value is a function.
  /// </summary>
  /// <param name="value">Output parameter for the value.</param>
  public bool IsFunction(out Function value) => (value = this.obj as Function) != null;

  /// <summary>
  /// Determines whether the value is an import.
  /// </summary>
  /// <param name="value">Output parameter for the value.</param>
  public bool IsImport(out Import value) => (value = this.obj as Import) != null;

  /// <summary>
  /// Determines whether the value is a property.
  /// </summary>
  /// <param name="value">Output parameter for the value.</param>
  public bool IsProperty(out Property value) => (value = this.obj as Property) != null;

  /// <summary>
  /// Determines whether the value is an exception.
  /// </summary>
  /// <param name="value">Output parameter for the value.</param>
  public bool IsException(out Exception value) => (value = this.obj as Exception) != null;

  /// <summary>
  /// Determines whether the values are equal. The comparison is exact bitwise
  /// check for value types, so comparing two NaN values produces true.
  /// </summary>
  /// <param name="other">The other value.</param>
  public bool Equals(Value other) => this.val.AllBits == other.val.AllBits && object.Equals(this.obj, other.obj);

  /// <summary>
  /// Determines whether the values are equal.
  /// </summary>
  /// <param name="obj">The object to compare.</param>
  public override bool Equals(object obj) => obj is Value other && this.Equals(other);

  /// <summary>
  /// Returns the hash code for the value.
  /// </summary>
  public override int GetHashCode() => HashCode.Combine(this.val.AllBits, this.obj);

  /// <summary>
  /// Gets a member or an element from the value.
  /// </summary>
  /// <param name="key">The member or element key.</param>
  public Value Get(Value key) {
    Table proto = null;

    if (this.IsTable(out var table)) {
      if (table.TryGetValue(key, out var value)) {
        if (value.IsProperty(out var property)) {
          if (property.Get != null) {
            return property.Get(this); // Get property value
          }
          return property.Value;
        }
        return value; // Value found
      }
      return default; // No value
    } else if (this.IsList(out var list)) {
      if (key.IsNumber(out var number)) {
        var index = (int)number;
        if (0 <= index && index < list.Count) {
          return list[index]; // Value found
        }
        return default; // Out of bounds
      }
      proto = ListProto;
    } else if (this.IsException(out _)) {
      proto = ExceptionProto;
    } else if (this.IsFunction(out _)) {
      proto = FunctionProto;
    } else if (this.IsString(out _)) {
      proto = StringProto;
    } else if (this.IsNumber(out _)) {
      proto = NumberProto;
    } else if (this.IsBoolean(out _)) {
      proto = BooleanProto;
    }

    if (proto != null) {
      if (proto.TryGetValue(key, out var value)) {
        if (value.IsProperty(out var property)) {
          if (property.Get != null) {
            return property.Get(this);
          }
          return default;
        }
        return value;
      }
    }

    return default;
  }

  /// <summary>
  /// Sets a member or an element for the value.
  /// </summary>
  /// <param name="key">The member or element key.</param>
  /// <param name="value">The member or element value.</param>
  public void Set(Value key, Value value) {
    Table proto = null;

    if (this.IsTable(out var table)) {
      if (table.TryGetValue(key, out var found) && found.IsProperty(out var property)) {
        if (property.Set != null) {
          property.Set(this, value);
        } else {
          return;
        }
      } else {
        table[key] = value;
      }
      return;
    } else if (this.IsList(out var list)) {
      if (key.IsNumber(out var number)) {
        var index = (int)number;
        if (0 <= index && index < list.Count) {
          list[index] = value;
        }
        return;
      }
      proto = ListProto;
    } else if (this.IsException(out _)) {
      proto = ExceptionProto;
    } else if (this.IsFunction(out _)) {
      proto = FunctionProto;
    } else if (this.IsString(out _)) {
      proto = StringProto;
    } else if (this.IsNumber(out _)) {
      proto = NumberProto;
    } else if (this.IsBoolean(out _)) {
      proto = BooleanProto;
    }

    if (proto != null) {
      if (proto.TryGetValue(key, out var found) && found.IsProperty(out var property)) {
        if (property.Set != null) {
          property.Set(this, value);
        }
      }
    }
  }

  /// <summary>
  /// Calls a function.
  /// </summary>
  /// <param name="receiver">The receiver.</param>
  /// <param name="arguments">The arguments.</param>
  public Value Call(Value receiver, Value[] arguments) {
    if (this.IsFunction(out var function)) {
      return function(receiver, arguments);
    } else if (this.Get(Callable).IsFunction(out var dotCall)) {
      return dotCall(receiver, arguments);
    } else {
      throw new Exception($"'{this}' is not a function");
    }
  }

  /// <summary>
  /// Calls a function with an argument list.
  /// </summary>
  /// <param name="receiver">The receiver.</param>
  /// <param name="argumentList">The argument list.</param>
  public Value Apply(Value receiver, Value argumentList) {
    Function target;

    if (this.IsFunction(out var function)) {
      target = function;
    } else if (this.Get(Callable).IsFunction(out var dotCall)) {
      target = dotCall;
    } else {
      throw new Exception($"'{this}' is not a function");
    }

    if (argumentList.IsList(out var arguments)) {
      return target(receiver, arguments.ToArray());
    } else {
      throw new Exception($"'{argumentList}' is not a list");
    }
  }

  /// <summary>
  /// Converts the value to a boolean.
  ///
  /// Null becomes false, allowing easy null check:
  /// ```
  /// if (obj) {
  ///   // obj != null
  /// }
  /// ```
  ///
  /// Zero and NaN become false, allowing easy empty check:
  /// ```
  /// if (list.size) {
  ///   // list is not empty
  /// }
  /// ```
  /// </summary>
  /// <returns>The boolean.</returns>
  public bool ToBoolean() {
    if (this.IsNull) {
      return false;
    }

    if (this.IsBoolean(out var boolean)) {
      return boolean;
    }

    if (this.IsNumber(out var number)) {
      return number < 0 || 0 < number; // NaN -> false
    }

    return true;
  }

  /// <summary>
  /// Converts the value to a number.
  ///
  /// Anything that is not a number becomes NaN.
  /// ```
  /// var a = 2 + false; // a = NaN
  /// ```
  /// </summary>
  /// <returns>The number.</returns>
  public double ToNumber() {
    if (this.IsNumber(out var number)) {
      return number;
    } else {
      return double.NaN;
    }
  }

  /// <summary>
  /// Returns a debug string representation of the object.
  /// </summary>
  public override string ToString() {
    string CallToString(Value value, Table proto) {
      if (proto.TryGetValue("toString", out var toString) && toString.Call(value, Array.Empty<Value>()).IsString(out var result)) {
        return result;
      } else {
        return null;
      }
    }

    if (this.IsNull) {
      return "null";
    }

    if (this.IsBoolean(out var boolean)) {
      return CallToString(this, BooleanProto) ?? (boolean ? "true" : "false");
    }

    if (this.IsNumber(out var number)) {
      return CallToString(this, NumberProto) ?? number.ToString(null, CultureInfo.InvariantCulture);
    }

    if (this.IsString(out var str)) {
      return CallToString(this, StringProto) ?? str;
    }

    if (this.IsList(out var list)) {
      return CallToString(this, ListProto) ?? $"List({list.Count})";
    }

    if (this.IsTable(out var table)) {
      return CallToString(this, table) ?? $"Table({table.Count})";
    }

    if (this.IsFunction(out _)) {
      return CallToString(this, FunctionProto) ?? "Function";
    }

    if (this.IsException(out var exception)) {
      return CallToString(this, ExceptionProto) ?? exception.Message;
    }

    if (this.IsImport(out var import)) {
      return $"import '{import.Name}'";
    }

    if (this.IsProperty(out _)) {
      return "Property";
    }

    Debug.Fail("Unknown value type");
    return string.Empty;
  }

  /// <summary>
  /// Implicit cast to struct.
  /// </summary>
  /// <param name="value">The value.</param>
  public static implicit operator Value(bool value) => new Value(value);

  /// <summary>
  /// Implicit cast to struct.
  /// </summary>
  /// <param name="value">The value.</param>
  public static implicit operator Value(double value) => new Value(value);

  /// <summary>
  /// Implicit cast to struct.
  /// </summary>
  /// <param name="value">The value.</param>
  public static implicit operator Value(string value) => new Value(value);

  /// <summary>
  /// Implicit cast to struct.
  /// </summary>
  /// <param name="value">The value.</param>
  public static implicit operator Value(List value) => new Value(value);

  /// <summary>
  /// Implicit cast to struct.
  /// </summary>
  /// <param name="value">The value.</param>
  public static implicit operator Value(Table value) => new Value(value);

  /// <summary>
  /// Implicit cast to struct.
  /// </summary>
  /// <param name="value">The value.</param>
  public static implicit operator Value(Function value) => new Value(value);

  /// <summary>
  /// Implicit cast to struct.
  /// </summary>
  /// <param name="value">The value.</param>
  public static implicit operator Value(Import value) => new Value(value);

  /// <summary>
  /// Implicit cast to struct.
  /// </summary>
  /// <param name="value">The value.</param>
  public static implicit operator Value(Property value) => new Value(value);

  /// <summary>
  /// Implicit cast to struct.
  /// </summary>
  /// <param name="value">The value.</param>
  public static implicit operator Value(Exception value) => new Value(value);

  /// <summary>
  /// The == operator.
  ///
  /// The implementation differs from Equals() on how NaN values are handled.
  /// When comparing two NaN values, Equals() returns true, == returns false.
  /// </summary>
  /// <param name="a">The left operand.</param>
  /// <param name="b">The right operand.</param>
  public static bool operator ==(Value a, Value b) => a.val.Double == b.val.Double && object.Equals(a.obj, b.obj);

  /// <summary>
  /// The != operator.
  /// </summary>
  /// <param name="a">The left operand.</param>
  /// <param name="b">The right operand.</param>
  public static bool operator !=(Value a, Value b) => !(a == b);

  /// <summary>
  /// Wraps a method delegate into a <see cref="Function" /> instance.
  /// </summary>
  /// <param name="fn">The function delegate.</param>
  /// <returns>The <see cref="Function" /> instance.</returns>
  private static Function Fn(Func<bool, Value> fn) => (self, args) => self.IsBoolean(out var v) ? fn(v) : default;

  /// <summary>
  /// Wraps a method delegate into a <see cref="Function" /> instance.
  /// </summary>
  /// <param name="fn">The function delegate.</param>
  /// <returns>The <see cref="Function" /> instance.</returns>
  private static Function Fn(Func<double, Value> fn) => (self, args) => self.IsNumber(out var v) ? fn(v) : default;

  /// <summary>
  /// Wraps a method delegate into a <see cref="Function" /> instance.
  /// </summary>
  /// <param name="fn">The function delegate.</param>
  /// <returns>The <see cref="Function" /> instance.</returns>
  private static Function Fn(Func<string, string, bool> fn) => (self, args) => self.IsString(out var v) && Value.At(args, 0).IsString(out var arg0) ? fn(v, arg0) : false;

  /// <summary>
  /// Wraps a method delegate into a <see cref="Function" /> instance.
  /// </summary>
  /// <param name="fn">The function delegate.</param>
  /// <returns>The <see cref="Function" /> instance.</returns>
  private static Function Fn(Func<string, Value> fn) => (self, args) => self.IsString(out var v) ? fn(v) : default;

  /// <summary>
  /// Wraps a method delegate into a <see cref="Function" /> instance.
  /// </summary>
  /// <param name="fn">The function delegate.</param>
  /// <returns>The <see cref="Function" /> instance.</returns>
  private static Function Fn(Func<List, Value> fn) => (self, args) => self.IsList(out var v) ? fn(v) : default;

  /// <summary>
  /// Wraps a method delegate into a <see cref="Function" /> instance.
  /// </summary>
  /// <param name="fn">The function delegate.</param>
  /// <returns>The <see cref="Function" /> instance.</returns>
  private static Function Fn(Func<Function, Value> fn) => (self, args) => self.IsFunction(out var v) ? fn(v) : default;

  /// <summary>
  /// Wraps a method delegate into a <see cref="Function" /> instance.
  /// </summary>
  /// <param name="fn">The function delegate.</param>
  /// <returns>The <see cref="Function" /> instance.</returns>
  private static Function Fn(Func<Exception, Value> fn) => (self, args) => self.IsException(out var v) ? fn(v) : default;

  /// <summary>
  /// Union type for holding the value types.
  /// </summary>
  [StructLayout(LayoutKind.Explicit)]
  private struct Union {
    /// <summary>
    /// The boolean value.
    /// </summary>
    [FieldOffset(0)] public bool Boolean;

    /// <summary>
    /// The double value.
    /// </summary>
    [FieldOffset(0)] public double Double;

    /// <summary>
    /// All the bits in the union, used when comparing the values.
    /// </summary>
    [FieldOffset(0)] public ulong AllBits;
  }
}
