using System;
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
readonly struct Value : IEquatable<Value>, IFormattable {
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
  public double ToNumber() {
    if (this.IsNumber(out var number)) {
      return number;
    } else {
      return double.NaN;
    }
  }

  /// <summary>
  /// Converts the value to a string.
  /// </summary>
  public override string ToString() => this.ToString(null, CultureInfo.InvariantCulture);

  /// <summary>
  /// Converts the value to a string.
  /// </summary>
  /// <param name="format">The format string.</param>
  /// <param name="formatProvider">The format provider.</param>
  public string ToString(string format, IFormatProvider formatProvider) {
    if (this.IsNull) {
      return "null";
    }

    if (this.IsBoolean(out var boolean)) {
      return boolean ? "true" : "false";
    }

    if (this.IsNumber(out var number)) {
      return number.ToString(format, formatProvider);
    }

    if (this.IsException(out var exception)) {
      return exception.Message;
    }

    return this.obj.ToString();
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
