using System;
using System.Collections.Generic;
using System.Diagnostics;

/// <summary>
/// Virtual machine code interpreter.
/// </summary>
struct Interpreter {
  /// <summary>
  /// Inspector callback.
  /// </summary>
  public static Inspector Inspector;

  /// <summary>
  /// The module.
  /// </summary>
  public readonly Module Module;

  /// <summary>
  /// The receiver, also known as the "this" reference.
  /// </summary>
  public readonly Value Receiver;

  /// <summary>
  /// The function arguments.
  /// </summary>
  public readonly Value[] Arguments;

  /// <summary>
  /// The current closure.
  /// </summary>
  public Closure Closure;

  /// <summary>
  /// The current exception handler.
  /// </summary>
  public ExceptionHandler ExceptionHandler;

  /// <summary>
  /// The stack.
  /// </summary>
  public Value[] Stack;

  /// <summary>
  /// The stack pointer, index of the next free slot at the top of the stack.
  /// </summary>
  public int SP;

  /// <summary>
  /// The instruction pointer, index of the next instruction to execute.
  /// </summary>
  public int IP;

  /// <summary>
  /// Initializes a new instance of the struct.
  /// </summary>
  /// <param name="module">The module.</param>
  /// <param name="receiver">The receiver, also known as the "this" reference.</param>
  /// <param name="arguments">The arguments.</param>
  /// <param name="entry">The entry point into the code.</param>
  /// <param name="closure">The initial closure, or null if none.</param>
  public Interpreter(Module module, Value receiver, Value[] arguments, int entry = 0, Closure closure = null) {
    this.Module = module ?? throw new ArgumentNullException(nameof(module));
    this.Receiver = receiver;
    this.Arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
    this.Closure = closure;
    this.ExceptionHandler = null;
    this.Stack = Array.Empty<Value>();
    this.SP = 0;
    this.IP = entry;
  }

  /// <summary>
  /// Runs the code.
  /// </summary>
  /// <returns>The result, or null if the code did not produce a result.</returns>
  public Value Run() {
    while (0 <= this.IP && this.IP < this.Module.Code.Length) {
      Inspector?.Invoke(ref this);

      var inst = this.Module.Code[this.IP++];

      switch (inst.Opcode) {
        case Opcode.Null: {
            this.Push(default);
            break;
          }

        case Opcode.Boolean: {
            this.Push(inst.Value != 0);
            break;
          }

        case Opcode.Number: {
            this.Push(inst.Value);
            break;
          }

        case Opcode.Swap: {
            this.Swap();
            break;
          }

        case Opcode.Copy: {
            this.Copy(inst.Value);
            break;
          }

        case Opcode.Drop: {
            this.Drop(inst.Value);
            break;
          }

        case Opcode.List: {
            var list = this.PopList(inst.Value);
            this.Push(list);
            break;
          }

        case Opcode.Table: {
            var table = this.PopTable(inst.Value);
            this.Push(table);
            break;
          }

        case Opcode.EnterClosure: {
            this.Closure = new Closure(this.Closure, this.PopArray(inst.Value));
            break;
          }

        case Opcode.LeaveClosure: {
            this.Closure = this.Closure?.Parent;
            break;
          }

        case Opcode.Function: {
            var entry = inst.Value;
            var scope = inst.Param != 0 ? this.Closure : null;
            var value = this.Function(entry, scope);
            this.Push(value);
            break;
          }

        case Opcode.LoadReceiver: {
            this.Push(this.Receiver);
            break;
          }

        case Opcode.LoadArgument: {
            var value = this.GetArgument(inst.Value);
            this.Push(value);
            break;
          }

        case Opcode.LoadArgumentList: {
            var list = this.GetArgumentList(inst.Value);
            this.Push(list);
            break;
          }

        case Opcode.LoadGlobal: {
            var value = this.GetGlobal(inst.Value);
            this.Push(value);
            break;
          }

        case Opcode.LoadVariable: {
            var array = this.GetScope(inst.Param);
            var value = array[inst.Value];
            this.Push(value);
            break;
          }

        case Opcode.StoreVariable: {
            var array = this.GetScope(inst.Param);
            var value = this.GetTop();
            array[inst.Value] = value;
            break;
          }

        case Opcode.LoadElement: {
            var key = this.Pop();
            var obj = this.Pop();
            var value = obj.Get(key);
            this.Push(value);
            break;
          }

        case Opcode.StoreElement: {
            var value = this.Pop();
            var key = this.Pop();
            var obj = this.Pop();
            obj.Set(key, value);
            this.Push(value);
            break;
          }

        case Opcode.Equal: {
            var b = this.Pop();
            var a = this.Pop();
            this.Push(a == b);
            break;
          }

        case Opcode.Less: {
            var b = this.PopNumber();
            var a = this.PopNumber();
            this.Push(a < b);
            break;
          }

        case Opcode.LessOrEqual: {
            var b = this.PopNumber();
            var a = this.PopNumber();
            this.Push(a <= b);
            break;
          }

        case Opcode.Greater: {
            var b = this.PopNumber();
            var a = this.PopNumber();
            this.Push(a > b);
            break;
          }

        case Opcode.GreaterOrEqual: {
            var b = this.PopNumber();
            var a = this.PopNumber();
            this.Push(a >= b);
            break;
          }

        case Opcode.Negate: {
            var value = this.PopNumber();
            this.Push(-value);
            break;
          }

        case Opcode.Add: {
            var b = this.PopNumber();
            var a = this.PopNumber();
            this.Push(a + b);
            break;
          }

        case Opcode.Subtract: {
            var b = this.PopNumber();
            var a = this.PopNumber();
            this.Push(a - b);
            break;
          }

        case Opcode.Multiply: {
            var b = this.PopNumber();
            var a = this.PopNumber();
            this.Push(a * b);
            break;
          }

        case Opcode.Divide: {
            var b = this.PopNumber();
            var a = this.PopNumber();
            this.Push(a / b);
            break;
          }

        case Opcode.Remainder: {
            var b = this.PopNumber();
            var a = this.PopNumber();
            this.Push(a % b);
            break;
          }

        case Opcode.Not: {
            var value = this.PopBoolean();
            this.Push(!value);
            break;
          }

        case Opcode.And: {
            var b = this.PopBoolean();
            var a = this.PopBoolean();
            this.Push(a & b);
            break;
          }

        case Opcode.Or: {
            var b = this.PopBoolean();
            var a = this.PopBoolean();
            this.Push(a | b);
            break;
          }

        case Opcode.Xor: {
            var b = this.PopBoolean();
            var a = this.PopBoolean();
            this.Push(a ^ b);
            break;
          }

        case Opcode.Jump: {
            this.IP = inst.Value;
            break;
          }

        case Opcode.ConditionalJump: {
            var value = this.PopBoolean();
            var match = inst.Param != 0;

            if (value == match) {
              this.IP = inst.Value;
            }
            break;
          }

        case Opcode.ConditionalAnd: {
            var condition = this.GetTop().ToBoolean();

            if (!condition) {
              this.IP = inst.Value;
            } else {
              this.Drop(1);
            }
            break;
          }

        case Opcode.ConditionalOr: {
            var condition = this.GetTop().ToBoolean();

            if (condition) {
              this.IP = inst.Value;
            } else {
              this.Drop(1);
            }
            break;
          }

        case Opcode.Throw: {
            var exception = this.Pop().ToException();
            this.AddStackTrace(exception);

            Inspector?.Invoke(ref this, exception);

            if (this.TryHandleException()) {
              this.Push(exception);
            } else {
              throw exception;
            }
            break;
          }

        case Opcode.EnterTry: {
            this.ExceptionHandler = new ExceptionHandler(this.ExceptionHandler, inst.Value, this.SP, this.Closure);
            break;
          }

        case Opcode.LeaveTry: {
            Debug.Assert(ExceptionHandler != null);
            this.ExceptionHandler = this.ExceptionHandler?.Parent;
            this.IP = inst.Value;
            break;
          }

        case Opcode.EnterFinally: {
            this.Push(this.IP);
            this.IP = inst.Value;
            break;
          }

        case Opcode.LeaveFinally: {
            this.IP = (int)this.PopNumber();
            break;
          }

        case Opcode.Call: {
            try {
              var args = this.PopArray(inst.Value);
              var self = this.Pop();
              var func = this.Pop();
              var result = func.Call(self, args);
              this.Push(result);
            } catch (Exception exception) {
              this.AddStackTrace(exception);
              Inspector?.Invoke(ref this, exception);

              if (this.TryHandleException()) {
                this.Push(exception);
              } else {
                throw;
              }
            }
            break;
          }

        case Opcode.Apply: {
            try {
              var args = this.Pop();
              var self = this.Pop();
              var func = this.Pop();
              var result = func.Apply(self, args);
              this.Push(result);
            } catch (Exception exception) {
              this.AddStackTrace(exception);
              Inspector?.Invoke(ref this, exception);

              if (this.TryHandleException()) {
                this.Push(exception);
              } else {
                throw;
              }
            }
            break;
          }

        case Opcode.Return: {
            var value = this.Pop();
            return value;
          }

        default: {
            Debug.Assert(false, $"Not implemented: {inst}");
            break;
          }
      }
    }

    return default;
  }

  /// <summary>
  /// Gets the source map for the code address.
  /// </summary>
  /// <param name="address">The code address, or null for the current address.</param>
  /// <returns>The source map, or null if not available.</returns>
  public SourceMap GetSourceMap(int? address = null) => this.Module.SourceMap?[address ?? this.IP];

  /// <summary>
  /// Gets the inspector data.
  /// </summary>
  /// <param name="dataSources">The data sources to include.</param>
  /// <param name="includeUnnamed">Indicates whether to include unnamed data.</param>
  /// <returns>The selected inspector data.</returns>
  public IEnumerable<InspectorData> GetInspectorData(InspectorDataSource dataSources, bool includeUnnamed = false) {
    bool Included(InspectorDataSource s) => (dataSources & s) == s;

    var sourceMap = this.GetSourceMap();

    if (Included(InspectorDataSource.Data)) {
      var src = sourceMap?.Globals;
      var data = this.Module.Data;

      for (var i = 0; i < data.Length; i++) {
        var name = default(string);

        if (src != null && src.FirstOrDefault(s => s.Index == i) is SourceMap.Global found) {
          name = found.Name;
        } else if (includeUnnamed) {
          name = $"data[{i}]";
        }

        if (name != null) {
          yield return new InspectorData(InspectorDataSource.Data, name, data[i]);
        }
      }
    }

    if (Included(InspectorDataSource.Receiver)) {
      yield return new InspectorData(InspectorDataSource.Receiver, "this", this.Receiver);
    }

    if (Included(InspectorDataSource.Arguments)) {
      var src = sourceMap?.Parameters;
      var args = this.Arguments;

      for (var i = 0; i < args.Length; i++) {
        var name = default(string);

        if (src != null && src.FirstOrDefault(s => s.Index == i) is SourceMap.Parameter found) {
          name = found.Name;
        } else if (src != null && src.FirstOrDefault(n => n.IsRestParameter) is SourceMap.Parameter rest && rest.Index <= i) {
          name = $"{rest.Name}[{i - rest.Index}]";
        } else if (includeUnnamed) {
          name = $"args[{i}]";
        }

        if (name != null) {
          yield return new InspectorData(InspectorDataSource.Arguments, name, args[i]);
        }
      }
    }

    if (Included(InspectorDataSource.Stack)) {
      var src = sourceMap?.Variables;
      var stack = this.Stack;

      for (var i = 0; i < this.SP; i++) {
        var name = default(string);

        if (src != null && src.FirstOrDefault(s => s.Scope == 0 && s.Index == i) is SourceMap.Variable found) {
          name = found.Name;
        } else if (includeUnnamed) {
          name = $"stack[{i}]";
        }

        if (name != null) {
          yield return new InspectorData(InspectorDataSource.Stack, name, stack[i]);
        }
      }
    }

    if (Included(InspectorDataSource.Closure)) {
      var src = sourceMap?.Variables;
      var scope = 1;

      for (var c = this.Closure; c != null; c = c.Parent, scope++) {
        var values = c.Values;

        for (var i = 0; i < values.Length; i++) {
          var name = default(string);

          if (src != null && src.FirstOrDefault(s => s.Scope == scope && s.Index == i) is SourceMap.Variable found) {
            name = found.Name;
          } else if (includeUnnamed) {
            name = $"closure[{scope}][{i}]";
          }

          if (name != null) {
            yield return new InspectorData(InspectorDataSource.Closure, name, values[i]);
          }
        }
      }
    }

    if (Included(InspectorDataSource.Registers)) {
      yield return new InspectorData(InspectorDataSource.Registers, "IP", this.IP);
      yield return new InspectorData(InspectorDataSource.Registers, "SP", this.SP);
    }
  }

  /// <summary>
  /// Returns an argument or null if the argument is undefined.
  /// </summary>
  /// <param name="index">The argument index.</param>
  private Value GetArgument(int index) => this.Arguments.Arg(index);

  /// <summary>
  /// Returns an argument list, starting from the given index.
  /// </summary>
  /// <param name="index">The starting index in the arguments.</param>
  private List GetArgumentList(int index) {
    Debug.Assert(index >= 0);

    var count = this.Arguments.Length - index;

    if (count > 0) {
      var list = new List(count);

      for (var i = 0; i < count; i++) {
        list.Add(this.Arguments[index + i]);
      }

      return list;
    } else {
      return new List();
    }
  }

  /// <summary>
  /// Returns the top value from the stack.
  /// </summary>
  private Value GetTop() {
    Debug.Assert(this.SP > 0);
    return this.Stack[this.SP - 1];
  }

  /// <summary>
  /// Pushes a value on the top of the stack.
  /// </summary>
  /// <param name="value">The value.</param>
  private void Push(Value value) {
    const int InitialStackSize = 8;

    if (this.Stack.Length == this.SP) {
      Array.Resize(ref this.Stack, Math.Max(this.SP * 2, InitialStackSize));
    }

    this.Stack[this.SP++] = value;
  }

  /// <summary>
  /// Removes a value from the top of the stack.
  /// </summary>
  private Value Pop() {
    Debug.Assert(this.SP > 0);
    return this.Stack[--this.SP];
  }

  /// <summary>
  /// Removes a value from the top of the stack and converts it to a boolean.
  /// </summary>
  private bool PopBoolean() => this.Pop().ToBoolean();

  /// <summary>
  /// Removes a value from the top of the stack and converts it to a number.
  /// </summary>
  private double PopNumber() => this.Pop().ToNumber();

  /// <summary>
  /// Removes values from the top of the stack and returns them as an array.
  /// </summary>
  /// <param name="count">The number of values to remove.</param>
  private Value[] PopArray(int count) {
    this.Drop(count);

    var array = new Value[count];

    for (var i = 0; i < count; i++) {
      array[i] = this.Stack[this.SP + i];
    }

    return array;
  }

  /// <summary>
  /// Removes values from the top of the stack and returns them as a list.
  /// </summary>
  /// <param name="count">The number of values to remove.</param>
  private List PopList(int count) {
    this.Drop(count);

    var list = new List(count);

    for (var i = 0; i < count; i++) {
      list.Add(this.Stack[SP + i]);
    }

    return list;
  }

  /// <summary>
  /// Removes key-value pairs from the top of the stack and returns them as a table.
  /// </summary>
  /// <param name="count">The number of key-value pairs to remove.</param>
  private Table PopTable(int count) {
    this.Drop(2 * count);

    var table = new Table(count);

    for (var i = 0; i < count; i++) {
      var n = this.SP + (i * 2);
      table.Add(this.Stack[n], this.Stack[n + 1]);
    }

    return table;
  }

  /// <summary>
  /// Swaps the top two values in the stack.
  /// </summary>
  private void Swap() {
    Debug.Assert(this.SP >= 2);
    var a = this.Stack[this.SP - 1];
    var b = this.Stack[this.SP - 2];
    this.Stack[this.SP - 1] = b;
    this.Stack[this.SP - 2] = a;
  }

  /// <summary>
  /// Copies values from the stack on top of the stack.
  /// </summary>
  /// <param name="count">The number of values to copy.</param>
  private void Copy(int count) {
    Debug.Assert(count >= 0);
    Debug.Assert(this.SP >= count);

    for (int i = 0, j = this.SP - count; i < count; i++, j++) {
      Push(this.Stack[j]);
    }
  }

  /// <summary>
  /// Discards values from the top of the stack.
  /// </summary>
  /// <param name="count">The number of values to discard.</param>
  private void Drop(int count) {
    Debug.Assert(count >= 0);
    Debug.Assert(this.SP >= count);
    this.SP -= count;
  }

  /// <summary>
  /// Returns the global value from the data section.
  /// </summary>
  /// <param name="index">The global index in the data section.</param>
  private Value GetGlobal(int index) {
    Debug.Assert(index < this.Module.Data.Length);
    return this.Module.Data[index];
  }

  /// <summary>
  /// Returns the variable scope.
  /// </summary>
  /// <param name="steps">The number of steps to take into the outer scopes.</param>
  private Value[] GetScope(int steps) {
    Debug.Assert(steps >= 0);

    var vars = this.Stack;
    var next = this.Closure;

    for (var i = 0; i < steps; i++) {
      vars = next.Values;
      next = next.Parent;
    }

    return vars;
  }

  /// <summary>
  /// Returns a new function.
  /// </summary>
  /// <param name="entry">The function entry point.</param>
  /// <param name="closure">The function closure, if any.</param>
  private Function Function(int entry, Closure closure) {
    var module = this.Module; // <- local variable to avoid capturing anything extra
    return (receiver, arguments) => new Interpreter(module, receiver, arguments, entry, closure).Run();
  }

  /// <summary>
  /// Adds a custom stack trace to an exception.
  /// </summary>
  /// <param name="exception">The exception.</param>
  private Exception AddStackTrace(Exception exception) {
    var stackTrace = exception.GetStackTrace() ?? string.Empty;

    if (stackTrace.Length > 0) {
      stackTrace += "\n";
    }

    stackTrace += "   at ";

    if (this.GetSourceMap() is SourceMap src) {
      if (src.Function != null) {
        stackTrace += $"{src.Function} in {this.Module.Name}";
      } else {
        stackTrace += this.Module.Name;
      }

      if (src.Row.HasValue) {
        stackTrace += $":line {src.Row.Value + 1}";
      }
    } else {
      stackTrace += this.Module.Name;
    }

    exception.SetStackTrace(stackTrace);
    return exception;
  }

  /// <summary>
  /// Attempts to handle an exception.
  /// </summary>
  /// <returns>Indicates whether the exception will be handled.</returns>
  private bool TryHandleException() {
    if (this.ExceptionHandler != null) {
      Debug.Assert(this.ExceptionHandler.SP <= this.SP);

      this.IP = this.ExceptionHandler.IP;
      this.SP = this.ExceptionHandler.SP;
      this.ExceptionHandler = this.ExceptionHandler.Parent;
      return true;
    } else {
      return false;
    }
  }
}
