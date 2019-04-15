using System;
using System.Collections.Generic;
using System.Diagnostics;

static partial class Compiler {
  /// <summary>
  /// An abstract base class for all expression types.
  /// </summary>
  private abstract class Expression : LanguageElement { }

  /// <summary>
  /// Receiver expression.
  /// </summary>
  private sealed class ReceiverExpression : Expression {
    /// <summary>
    /// Validates the language element semantics.
    /// </summary>
    public override void Validate() { }

    /// <summary>
    /// Emits the code and data.
    /// </summary>
    /// <param name="emitter">The emitter.</param>
    /// <param name="exits">The non-exceptional exit targets.</param>
    public override void Emit(Emitter emitter, Exits exits) => emitter.Emit(Opcode.LoadReceiver);
  }

  /// <summary>
  /// List construction expression.
  /// </summary>
  private sealed class ListExpression : Expression {
    /// <summary>
    /// The list values.
    /// </summary>
    public List<Expression> Values { get; } = new List<Expression>();

    /// <summary>
    /// Validates the language element semantics.
    /// </summary>
    public override void Validate() {
      this.Values.ForEach(item => item.Validate());
    }

    /// <summary>
    /// Emits the code and data.
    /// </summary>
    /// <param name="emitter">The emitter.</param>
    /// <param name="exits">The non-exceptional exit targets.</param>
    public override void Emit(Emitter emitter, Exits exits) {
      foreach (var expression in this.Values) {
        emitter.Emit(expression, exits);
      }

      emitter.Emit(Opcode.List, value: this.Values.Count);
    }
  }

  /// <summary>
  /// Table construction expression.
  /// </summary>
  private sealed class TableExpression : Expression {
    /// <summary>
    /// The table key-value pairs.
    /// </summary>
    public List<(Expression Key, Expression Value)> Values { get; } = new List<(Expression Key, Expression Value)>();

    /// <summary>
    /// Validates the language element semantics.
    /// </summary>
    public override void Validate() {
      this.Values.ForEach(pair => {
        pair.Key.Validate();
        pair.Value.Validate();
      });
    }

    /// <summary>
    /// Emits the code and data.
    /// </summary>
    /// <param name="emitter">The emitter.</param>
    /// <param name="exits">The non-exceptional exit targets.</param>
    public override void Emit(Emitter emitter, Exits exits) {
      foreach (var pair in this.Values) {
        emitter.Emit(pair.Key, exits);
        emitter.Emit(pair.Value, exits);
      }

      emitter.Emit(Opcode.Table, value: this.Values.Count);
    }
  }

  /// <summary>
  /// Throw expression.
  /// </summary>
  private sealed class ThrowExpression : Expression {
    /// <summary>
    /// The thrown value.
    /// </summary>
    public Expression Value { get; set; }

    /// <summary>
    /// Validates the language element semantics.
    /// </summary>
    public override void Validate() {
      this.Value.Validate();
    }

    /// <summary>
    /// Emits the code and data.
    /// </summary>
    /// <param name="emitter">The emitter.</param>
    /// <param name="exits">The non-exceptional exit targets.</param>
    public override void Emit(Emitter emitter, Exits exits) {
      emitter.Emit(this.Value, exits);
      emitter.Emit(Opcode.Throw);
    }
  }

  /// <summary>
  /// Function expression.
  /// </summary>
  private sealed class FunctionExpression : Expression {
    /// <summary>
    /// The function name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The function parameters.
    /// </summary>
    public List<Identifier> Parameters { get; } = new List<Identifier>();

    /// <summary>
    /// The function body.
    /// </summary>
    public Block Body { get; set; }

    /// <summary>
    /// Validates the language element semantics.
    /// </summary>
    public override void Validate() {
      this.Parameters.ForEach(item => item.Validate());
      this.Body.Validate();
    }

    /// <summary>
    /// Emits the code and data.
    /// </summary>
    /// <param name="emitter">The emitter.</param>
    /// <param name="exits">The non-exceptional exit targets.</param>
    public override void Emit(Emitter emitter, Exits exits) {
      var body = emitter.Function(this.Name).Emit(this.Body, null); // <- Do not pass exits from this scope to the function

      if (this.Body.Scope.ContainsClosureReferences) {
        emitter.Emit(Opcode.Function, param: 1, target: body);
      } else {
        emitter.Emit(Opcode.Function, param: 0, target: body);
      }
    }
  }

  /// <summary>
  /// Function call expression.
  /// </summary>
  private sealed class FunctionCallExpression : Expression {
    /// <summary>
    /// The function.
    /// </summary>
    public Expression Function { get; set; }

    /// <summary>
    /// The function arguments.
    /// </summary>
    public List<Expression> Arguments { get; } = new List<Expression>();

    /// <summary>
    /// Validates the language element semantics.
    /// </summary>
    public override void Validate() {
      this.Function.Validate();
      this.Arguments.ForEach(item => item.Validate());
    }

    /// <summary>
    /// Emits the code and data.
    /// </summary>
    /// <param name="emitter">The emitter.</param>
    /// <param name="exits">The non-exceptional exit targets.</param>
    public override void Emit(Emitter emitter, Exits exits) {
      if (this.Function is MemberAccessExpression expression) {
        emitter.Emit(expression.Container, exits);
        emitter.Emit(Opcode.Copy, value: 1);
        emitter.Emit(expression.Selector, exits);
        emitter.Emit(Opcode.LoadElement);
        emitter.Emit(Opcode.Swap);
      } else {
        emitter.Emit(this.Function, exits);
        emitter.Emit(Opcode.Null); // <- null receiver
      }

      foreach (var argument in this.Arguments) {
        emitter.Emit(argument, exits);
      }

      emitter.Emit(Opcode.Call, value: this.Arguments.Count);
    }
  }

  /// <summary>
  /// Unary expression.
  /// </summary>
  private sealed class UnaryExpression : Expression {
    /// <summary>
    /// The operator.
    /// </summary>
    public string Operator => this.Token.Text;

    /// <summary>
    /// The right hand side.
    /// </summary>
    public Expression Right { get; set; }

    /// <summary>
    /// Validates the language element semantics.
    /// </summary>
    public override void Validate() {
      this.Right.Validate();
    }

    /// <summary>
    /// Emits the code and data.
    /// </summary>
    /// <param name="emitter">The emitter.</param>
    /// <param name="exits">The non-exceptional exit targets.</param>
    public override void Emit(Emitter emitter, Exits exits) {
      emitter.Emit(this.Right, exits);

      switch (this.Operator) {
        case "+": break; // Do nothing
        case "-": emitter.Emit(Opcode.Negate); break;
        case "!": emitter.Emit(Opcode.Not); break;
        default: Debug.Fail("Unexpected operator"); break;
      }
    }
  }

  /// <summary>
  /// Binary expression.
  /// </summary>
  private sealed class BinaryExpression : Expression {
    /// <summary>
    /// The operator.
    /// </summary>
    public string Operator => this.Token.Text;

    /// <summary>
    /// The left hand side.
    /// </summary>
    public Expression Left { get; set; }

    /// <summary>
    /// The right hand side.
    /// </summary>
    public Expression Right { get; set; }

    /// <summary>
    /// Validates the language element semantics.
    /// </summary>
    public override void Validate() {
      this.Left.Validate();
      this.Right.Validate();
    }

    /// <summary>
    /// Emits the code and data.
    /// </summary>
    /// <param name="emitter">The emitter.</param>
    /// <param name="exits">The non-exceptional exit targets.</param>
    public override void Emit(Emitter emitter, Exits exits) {
      emitter.Emit(this.Left, exits);
      emitter.Emit(this.Right, exits);

      switch (this.Operator) {
        case "+": emitter.Emit(Opcode.Add); break;
        case "-": emitter.Emit(Opcode.Subtract); break;
        case "*": emitter.Emit(Opcode.Multiply); break;
        case "/": emitter.Emit(Opcode.Divide); break;
        case "%": emitter.Emit(Opcode.Remainder); break;
        case "&": emitter.Emit(Opcode.And); break;
        case "|": emitter.Emit(Opcode.Or); break;
        case "^": emitter.Emit(Opcode.Xor); break;
        case "==": emitter.Emit(Opcode.Equal); break;
        case "!=": emitter.Emit(Opcode.Equal).Emit(Opcode.Not); break;
        case "<": emitter.Emit(Opcode.Less); break;
        case "<=": emitter.Emit(Opcode.LessOrEqual); break;
        case ">": emitter.Emit(Opcode.Greater); break;
        case ">=": emitter.Emit(Opcode.GreaterOrEqual); break;
        default: Debug.Fail("Unexpected operator"); break;
      }
    }
  }

  /// <summary>
  /// Conditional binary expression.
  /// </summary>
  private sealed class ConditionalBinaryExpression : Expression {
    /// <summary>
    /// The operator.
    /// </summary>
    public string Operator => this.Token.Text;

    /// <summary>
    /// The left hand side.
    /// </summary>
    public Expression Left { get; set; }

    /// <summary>
    /// The right hand side.
    /// </summary>
    public Expression Right { get; set; }

    /// <summary>
    /// Validates the language element semantics.
    /// </summary>
    public override void Validate() {
      this.Left.Validate();
      this.Right.Validate();
    }

    /// <summary>
    /// Emits the code and data.
    /// </summary>
    /// <param name="emitter">The emitter.</param>
    /// <param name="exits">The non-exceptional exit targets.</param>
    public override void Emit(Emitter emitter, Exits exits) {
      var section1 = emitter.Section();
      var end = emitter.Section();

      section1.Emit(this.Left, exits);

      switch (this.Operator) {
        case "&&": section1.Emit(Opcode.ConditionalAnd, target: end); break;
        case "||": section1.Emit(Opcode.ConditionalOr, target: end); break;
        default: Debug.Fail("Unexpected operator"); break;
      }

      section1.Emit(this.Right, exits);
    }
  }

  /// <summary>
  /// Conditional ternary expression.
  /// </summary>
  private sealed class ConditionalTernaryExpression : Expression {
    /// <summary>
    /// The condition.
    /// </summary>
    public Expression Condition { get; set; }

    /// <summary>
    /// The true branch.
    /// </summary>
    public Expression True { get; set; }

    /// <summary>
    /// The false branch.
    /// </summary>
    public Expression False { get; set; }

    /// <summary>
    /// Validates the language element semantics.
    /// </summary>
    public override void Validate() {
      this.Condition.Validate();
      this.True.Validate();
      this.False.Validate();
    }

    /// <summary>
    /// Emits the code and data.
    /// </summary>
    /// <param name="emitter">The emitter.</param>
    /// <param name="exits">The non-exceptional exit targets.</param>
    public override void Emit(Emitter emitter, Exits exits) {
      var section1 = emitter.Section(); // <- condition
      var section2 = emitter.Section(); // <- true branch
      var section3 = emitter.Section(); // <- false branch
      var end = emitter.Section();

      section1.Emit(this.Condition, exits);
      section1.Emit(Opcode.ConditionalJump, param: 0, target: section3);

      section2.Emit(this.True, exits);
      section2.Emit(Opcode.Jump, target: end);

      section3.Emit(this.False, exits);
    }
  }

  /// <summary>
  /// Assignment expression.
  /// </summary>
  private sealed class AssignmentExpression : Expression {
    /// <summary>
    /// The operator.
    /// </summary>
    public string Operator => this.Token.Text;

    /// <summary>
    /// The target.
    /// </summary>
    public AssignableExpression Target { get; set; }

    /// <summary>
    /// The value.
    /// </summary>
    public Expression Value { get; set; }

    /// <summary>
    /// Validates the language element semantics.
    /// </summary>
    public override void Validate() {
      this.Target.Validate();
      this.Value.Validate();

      if (this.Target is Identifier identifier && identifier.Slot.ReadOnly) {
        throw this.Token.Error($"'{identifier.Name}' is read-only");
      }
    }

    /// <summary>
    /// Emits the code and data.
    /// </summary>
    /// <param name="emitter">The emitter.</param>
    /// <param name="exits">The non-exceptional exit targets.</param>
    public override void Emit(Emitter emitter, Exits exits) {
      if (this.Operator == "=") {
        this.Target.EmitStore(emitter, this.EmitValue, exits);
      } else {
        this.Target.EmitLoadAndStore(emitter, this.EmitValue, exits);
      }
    }

    /// <summary>
    /// Emits the value code.
    /// </summary>
    /// <param name="emitter">The emitter.</param>
    /// <param name="exits">The non-exceptional exit targets.</param>
    private void EmitValue(Emitter emitter, Exits exits) {
      switch (this.Operator) {
        case "=": emitter.Emit(this.Value, exits); break;
        case "+=": emitter.Emit(this.Value, exits).Emit(Opcode.Add); break;
        case "-=": emitter.Emit(this.Value, exits).Emit(Opcode.Subtract); break;
        case "*=": emitter.Emit(this.Value, exits).Emit(Opcode.Multiply); break;
        case "/=": emitter.Emit(this.Value, exits).Emit(Opcode.Divide); break;
        case "%=": emitter.Emit(this.Value, exits).Emit(Opcode.Remainder); break;
        case "&=": emitter.Emit(this.Value, exits).Emit(Opcode.And); break;
        case "|=": emitter.Emit(this.Value, exits).Emit(Opcode.Or); break;
        case "^=": emitter.Emit(this.Value, exits).Emit(Opcode.Xor); break;
        default: Debug.Fail("Unexpected operator"); break;
      }
    }
  }

  /// <summary>
  /// An abstract base class for assignable expressions.
  /// </summary>
  private abstract class AssignableExpression : Expression {
    /// <summary>
    /// Emits the store code.
    /// </summary>
    /// <param name="emitter">The emitter.</param>
    /// <param name="emitValue">The action that emits the value expression.</param>
    /// <param name="exits">The non-exceptional exit targets.</param>
    public abstract void EmitStore(Emitter emitter, Action<Emitter, Exits> emitValue, Exits exits);

    /// <summary>
    /// Emits the load and store code.
    /// </summary>
    /// <param name="emitter">The emitter.</param>
    /// <param name="emitValue">The action that emits the value expression.</param>
    /// <param name="exits">The non-exceptional exit targets.</param>
    public abstract void EmitLoadAndStore(Emitter emitter, Action<Emitter, Exits> emitValue, Exits exits);
  }

  /// <summary>
  /// Identifier access expression.
  /// </summary>
  private sealed class Identifier : AssignableExpression {
    /// <summary>
    /// The identifier name.
    /// </summary>
    public string Name => this.Token.Text;

    /// <summary>
    /// The identifier slot.
    /// </summary>
    public Slot Slot { get; }

    /// <summary>
    /// Initializes a new instance of the class.
    /// </summary>
    /// <param name="slot">The identifier slot.</param>
    public Identifier(Slot slot) {
      this.Slot = slot;
      this.Slot.References.Add(this);
    }

    /// <summary>
    /// Validates the language element semantics.
    /// </summary>
    public override void Validate() { }

    /// <summary>
    /// Emits the code and data.
    /// </summary>
    /// <param name="emitter">The emitter.</param>
    /// <param name="exits">The non-exceptional exit targets.</param>
    public override void Emit(Emitter emitter, Exits exits) {
      switch (this.Slot.Storage) {
        case SlotStorage.None:
          switch (this.Slot.Source) {
            case SlotSource.Argument:
              emitter.Emit(Opcode.LoadArgument, value: this.Slot.SourceIndex.Value);
              break;

            case SlotSource.ArgumentSlice:
              emitter.Emit(Opcode.LoadArgumentList, value: this.Slot.SourceIndex.Value);
              break;

            default:
              Debug.Fail("Unexpected slot source");
              break;
          }
          break;

        case SlotStorage.Global:
          emitter.Emit(Opcode.LoadGlobal, value: this.Slot.StorageIndex.Value);
          break;

        case SlotStorage.Local:
        case SlotStorage.Closure:
          var param = this.Scope.GetStepsToClosure(this.Slot);
          var value = this.Slot.StorageIndex.Value;
          emitter.Emit(Opcode.LoadVariable, param: param, value: value);
          break;

        default:
          Debug.Fail("Unexpected slot storage");
          break;
      }
    }

    /// <summary>
    /// Emits the store code.
    /// </summary>
    /// <param name="emitter">The emitter.</param>
    /// <param name="emitValue">The action that emits the value expression.</param>
    /// <param name="loadAndStore">Indicates whether to emit a load before the value.</param>
    /// <param name="exits">The non-exceptional exit targets.</param>
    public override void EmitStore(Emitter emitter, Action<Emitter, Exits> emitValue, Exits exits) {
      Debug.Assert(this.Slot.Storage == SlotStorage.Local || this.Slot.Storage == SlotStorage.Closure);

      var param = this.Scope.GetStepsToClosure(this.Slot);
      var value = this.Slot.StorageIndex.Value;

      emitValue(emitter, exits);

      emitter.Emit(Opcode.StoreVariable, param: param, value: value);
    }

    /// <summary>
    /// Emits the load and store code.
    /// </summary>
    /// <param name="emitter">The emitter.</param>
    /// <param name="emitValue">The action that emits the value expression.</param>
    /// <param name="exits">The non-exceptional exit targets.</param>
    public override void EmitLoadAndStore(Emitter emitter, Action<Emitter, Exits> emitValue, Exits exits) {
      Debug.Assert(this.Slot.Storage == SlotStorage.Local || this.Slot.Storage == SlotStorage.Closure);

      var param = this.Scope.GetStepsToClosure(this.Slot);
      var value = this.Slot.StorageIndex.Value;

      emitter.Emit(Opcode.LoadVariable, param: param, value: value);

      emitValue(emitter, exits);

      emitter.Emit(Opcode.StoreVariable, param: param, value: value);
    }
  }

  /// <summary>
  /// Member access expression.
  /// </summary>
  private sealed class MemberAccessExpression : AssignableExpression {
    /// <summary>
    /// The member container.
    /// </summary>
    public Expression Container { get; set; }

    /// <summary>
    /// The member selector.
    /// </summary>
    public Expression Selector { get; set; }

    /// <summary>
    /// Validates the language element semantics.
    /// </summary>
    public override void Validate() {
      this.Container.Validate();
      this.Selector.Validate();
    }

    /// <summary>
    /// Emits the code and data.
    /// </summary>
    /// <param name="emitter">The emitter.</param>
    /// <param name="exits">The non-exceptional exit targets.</param>
    public override void Emit(Emitter emitter, Exits exits) {
      emitter.Emit(this.Container, exits);
      emitter.Emit(this.Selector, exits);
      emitter.Emit(Opcode.LoadElement);
    }

    /// <summary>
    /// Emits the store code.
    /// </summary>
    /// <param name="emitter">The emitter.</param>
    /// <param name="emitValue">The action that emits the value expression.</param>
    /// <param name="exits">The non-exceptional exit targets.</param>
    public override void EmitStore(Emitter emitter, Action<Emitter, Exits> emitValue, Exits exits) {
      emitter.Emit(this.Container, exits);
      emitter.Emit(this.Selector, exits);

      emitValue(emitter, exits);

      emitter.Emit(Opcode.StoreElement);
    }

    /// <summary>
    /// Emits the load and store code.
    /// </summary>
    /// <param name="emitter">The emitter.</param>
    /// <param name="emitValue">The action that emits the value expression.</param>
    /// <param name="exits">The non-exceptional exit targets.</param>
    public override void EmitLoadAndStore(Emitter emitter, Action<Emitter, Exits> emitValue, Exits exits) {
      emitter.Emit(this.Container, exits);
      emitter.Emit(this.Selector, exits);
      emitter.Emit(Opcode.Copy, value: 2);
      emitter.Emit(Opcode.LoadElement);

      emitValue(emitter, exits);

      emitter.Emit(Opcode.StoreElement);
    }
  }
}
