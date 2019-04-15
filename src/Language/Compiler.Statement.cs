using System.Collections.Generic;
using System.Diagnostics;

static partial class Compiler {
  /// <summary>
  /// An abstract base class for statements.
  /// </summary>
  private abstract class Statement : LanguageElement { }

  /// <summary>
  /// An abstract base class for simple statements.
  /// </summary>
  private abstract class SimpleStatement : Statement { }

  /// <summary>
  /// Expression statement.
  /// </summary>
  private sealed class ExpressionStatement : SimpleStatement {
    /// <summary>
    /// The expression.
    /// </summary>
    public Expression Expression { get; set; }

    /// <summary>
    /// Validates the language element semantics.
    /// </summary>
    public override void Validate() {
      this.Expression.Validate();
    }

    /// <summary>
    /// Emits the code and data.
    /// </summary>
    /// <param name="emitter">The emitter.</param>
    /// <param name="exits">The non-exceptional exit targets.</param>
    public override void Emit(Emitter emitter, Exits exits) => emitter.Emit(this.Expression, exits).Emit(Opcode.Drop, value: 1);
  }

  /// <summary>
  /// Declaration statement.
  /// </summary>
  private sealed class DeclarationStatement : SimpleStatement {
    /// <summary>
    /// The identifier being declared.
    /// </summary>
    public Identifier Identifier { get; set; }

    /// <summary>
    /// The initial value.
    /// </summary>
    public Expression Value { get; set; }

    /// <summary>
    /// Validates the language element semantics.
    /// </summary>
    public override void Validate() {
      this.Identifier.Validate();
      this.Value.Validate();
    }

    /// <summary>
    /// Emits the code and data.
    /// </summary>
    /// <param name="emitter">The emitter.</param>
    /// <param name="exits">The non-exceptional exit targets.</param>
    public override void Emit(Emitter emitter, Exits exits) {
      this.Identifier.EmitStore(emitter, this.Value.Emit, exits);
      emitter.Emit(Opcode.Drop, value: 1);
    }
  }

  /// <summary>
  /// Import statement.
  /// </summary>
  private sealed class ImportStatement : Statement {
    /// <summary>
    /// The identifier being imported.
    /// </summary>
    public Identifier Identifier { get; set; }

    /// <summary>
    /// The source module name.
    /// </summary>
    public string Source { get; set; }

    /// <summary>
    /// Validates the language element semantics.
    /// </summary>
    public override void Validate() {
      this.Identifier.Validate();

      if (this.Scope.Scope != null) {
        throw this.Token.Error("Imports can be declared only in the global scope");
      }
    }

    /// <summary>
    /// Emits the code and data.
    /// </summary>
    /// <param name="emitter">The emitter.</param>
    /// <param name="exits">The non-exceptional exit targets.</param>
    public override void Emit(Emitter emitter, Exits exits) {
      emitter.Set(this.Identifier.Slot.StorageIndex.Value, new Import(this.Source));
    }
  }

  /// <summary>
  /// Export statement.
  /// </summary>
  private sealed class ExportStatement : Statement {
    /// <summary>
    /// The identifier being exported.
    /// </summary>
    public Identifier Identifier { get; set; }

    /// <summary>
    /// The initial value.
    /// </summary>
    public Expression Value { get; set; }

    /// <summary>
    /// Validates the language element semantics.
    /// </summary>
    public override void Validate() {
      this.Identifier.Validate();
      this.Value.Validate();

      if (this.Scope.Scope != null) {
        throw this.Token.Error("Exports can be declared only in the global scope");
      }
    }

    /// <summary>
    /// Emits the code and data.
    /// </summary>
    /// <param name="emitter">The emitter.</param>
    /// <param name="exits">The non-exceptional exit targets.</param>
    public override void Emit(Emitter emitter, Exits exits) {
      emitter.Emit(Opcode.LoadReceiver);
      emitter.Emit(Opcode.LoadGlobal, value: emitter.GetOrAdd(this.Identifier.Name));
      this.Identifier.EmitStore(emitter, this.Value.Emit, exits);
      emitter.Emit(Opcode.StoreElement);
      emitter.Emit(Opcode.Drop, value: 1);
    }
  }

  /// <summary>
  /// Block statement.
  /// </summary>
  private sealed class Block : Statement {
    /// <summary>
    /// The nested statements.
    /// </summary>
    public List<Statement> Statements { get; } = new List<Statement>();

    /// <summary>
    /// Validates the language element semantics.
    /// </summary>
    public override void Validate() {
      this.Statements.ForEach(item => item.Validate());
    }

    /// <summary>
    /// Emits the code and data.
    /// </summary>
    /// <param name="emitter">The emitter.</param>
    /// <param name="exits">The non-exceptional exit targets.</param>
    public override void Emit(Emitter emitter, Exits exits) {
      emitter.Emit(this.Scope, exits);

      foreach (var statement in this.Statements) {
        emitter.Emit(statement, exits);
      }
    }
  }

  /// <summary>
  /// If statement.
  /// </summary>
  private sealed class IfStatement : Statement {
    /// <summary>
    /// The condition.
    /// </summary>
    public Expression Condition { get; set; }

    /// <summary>
    /// The true branch.
    /// </summary>
    public Block True { get; set; }

    /// <summary>
    /// The false branch.
    /// </summary>
    public Block False { get; set; }

    /// <summary>
    /// Validates the language element semantics.
    /// </summary>
    public override void Validate() {
      this.Condition.Validate();
      this.True.Validate();
      this.False?.Validate();
    }

    /// <summary>
    /// Emits the code and data.
    /// </summary>
    /// <param name="exits">The non-exceptional exit targets.</param>
    public override void Emit(Emitter emitter, Exits exits) {
      var section1 = emitter.Section(); // <- condition and
      var section2 = emitter.Section(); // <- true branch
      var section3 = emitter.Section(); // <- false branch
      var end = emitter.Section();

      section1.Emit(this.Condition, exits);
      section1.Emit(Opcode.ConditionalJump, param: 0, target: section3);
      section2.Emit(this.True, exits);

      if (this.False != null) {
        section2.Emit(Opcode.Jump, target: end);
        section3.Emit(this.False, exits);
      }
    }
  }

  /// <summary>
  /// Try-catch statement.
  /// </summary>
  private sealed class TryCatchStatement : Statement {
    /// <summary>
    /// The try block.
    /// </summary>
    public Block Try { get; set; }

    /// <summary>
    /// The catch block.
    /// </summary>
    public Block Catch { get; set; }

    /// <summary>
    /// The exception identifier.
    /// </summary>
    public Identifier Exception { get; set; }

    /// <summary>
    /// Validates the language element semantics.
    /// </summary>
    public override void Validate() {
      this.Try.Validate();
      this.Catch.Validate();
      this.Exception?.Validate();
    }

    /// <summary>
    /// Emits the code and data.
    /// </summary>
    /// <param name="emitter">The emitter.</param>
    /// <param name="exits">The non-exceptional exit targets.</param>
    public override void Emit(Emitter emitter, Exits exits) {
      var section1 = emitter.Section(); // <- try body
      var section2 = emitter.Section().At(null); // <- continue jumps here
      var section3 = emitter.Section().At(null); // <- break jumps here
      var section4 = emitter.Section().At(null); // <- return jumps here
      var section5 = emitter.Section().At(null); // <- catch body
      var end = emitter.Section();

      section1.Emit(Opcode.EnterTry, target: section5);

      section1.Emit(this.Try, new Exits(exits) {
        Continue = exits.Continue != null ? section2 : null,
        Break = exits.Break != null ? section3 : null,
        Return = section4
      });
      section1.Emit(Opcode.LeaveTry, target: end);

      if (exits.Continue != null) {
        section2.Emit(Opcode.LeaveTry, target: exits.Continue);
      }

      if (exits.Break != null) {
        section3.Emit(Opcode.LeaveTry, target: exits.Break);
      }

      section4.Emit(Opcode.LeaveTry, target: exits.Return);

      if (this.Exception != null) {
        this.Exception.EmitStore(section5.At(this.Exception.Token), delegate { }, exits); // <- exception is already on the top of the stack
      }

      section5.Emit(Opcode.Drop, value: 1);
      section5.Emit(this.Catch, exits);
    }
  }

  /// <summary>
  /// Try-finally statement.
  /// </summary>
  private sealed class TryFinallyStatement : Statement {
    /// <summary>
    /// The try block.
    /// </summary>
    public Block Try { get; set; }

    /// <summary>
    /// The finally block.
    /// </summary>
    public Block Finally { get; set; }

    /// <summary>
    /// Validates the language element semantics.
    /// </summary>
    public override void Validate() {
      this.Try.Validate();
      this.Finally.Validate();
    }

    /// <summary>
    /// Emits the code and data.
    /// </summary>
    /// <param name="emitter">The emitter.</param>
    /// <param name="exits">The non-exceptional exit targets.</param>
    public override void Emit(Emitter emitter, Exits exits) {
      var section1 = emitter.Section(); // <- try body
      var section2 = emitter.Section().At(null); // <- continue jumps here
      var section3 = emitter.Section().At(null); // <- break jumps here
      var section4 = emitter.Section().At(null); // <- return jumps here
      var section5 = emitter.Section().At(null); // <- catch body
      var section6 = emitter.Section().At(null); // <- finally entry
      var section7 = emitter.Section().At(null); // <- finally body
      var end = emitter.Section();

      section1.Emit(Opcode.EnterTry, target: section5);
      section1.Emit(this.Try, new Exits(exits) {
        Continue = exits.Continue != null ? section2 : null,
        Break = exits.Break != null ? section3 : null,
        Return = section4
      });
      section1.Emit(Opcode.EnterFinally, target: section6);
      section1.Emit(Opcode.Jump, target: end);

      if (exits.Continue != null) {
        section2.Emit(Opcode.EnterFinally, target: section6);
        section2.Emit(Opcode.Jump, target: exits.Continue);
      }

      if (exits.Break != null) {
        section3.Emit(Opcode.EnterFinally, target: section6);
        section3.Emit(Opcode.Jump, target: exits.Break);
      }

      section4.Emit(Opcode.EnterFinally, target: section6);
      section4.Emit(Opcode.Jump, target: exits.Return);

      section5.Emit(Opcode.EnterFinally, target: section7);
      section5.Emit(Opcode.Throw);

      section6.Emit(Opcode.LeaveTry, target: section7);

      section7.Emit(this.Finally, exits);
      section7.Emit(Opcode.LeaveFinally);
    }
  }

  /// <summary>
  /// An abstract base class for loop statements.
  /// </summary>
  private abstract class LoopStatement : Statement { }

  /// <summary>
  /// While statement.
  /// </summary>
  private sealed class WhileStatement : LoopStatement {
    /// <summary>
    /// The condition.
    /// </summary>
    public Expression Condition { get; set; }

    /// <summary>
    /// The loop body.
    /// </summary>
    public Block Body { get; set; }

    /// <summary>
    /// Validates the language element semantics.
    /// </summary>
    public override void Validate() {
      this.Condition.Validate();
      this.Body.Validate();
    }

    /// <summary>
    /// Emits the code and data.
    /// </summary>
    /// <param name="emitter">The emitter.</param>
    /// <param name="exits">The non-exceptional exit targets.</param>
    public override void Emit(Emitter emitter, Exits exits) {
      var section1 = emitter.Section(); // <- loop condition
      var section2 = emitter.Section(); // <- loop body
      var section3 = emitter.Section().At(null); // <- continue jumps here
      var section4 = emitter.Section().At(null); // <- jump back to start
      var section5 = emitter.Section().At(null); // <- break jumps here
      var end = emitter.Section();

      section1.Emit(this.Condition, exits);
      section1.Emit(Opcode.ConditionalJump, param: 0, target: end);

      section2.Emit(this.Body, new Exits(exits) { Continue = section3, Break = section5 });

      // continue jumps here

      section4.Emit(Opcode.Jump, target: section1);

      // break jumps here

      // end
    }
  }

  /// <summary>
  /// For statement.
  /// </summary>
  private sealed class ForStatement : LoopStatement {
    /// <summary>
    /// The init statement.
    /// </summary>
    public SimpleStatement Init { get; set; }

    /// <summary>
    /// The loop condition.
    /// </summary>
    public Expression Condition { get; set; }

    /// <summary>
    /// The loop "incrementer".
    /// </summary>
    public Expression Next { get; set; }

    /// <summary>
    /// The loop body.
    /// </summary>
    public Block Body { get; set; }

    /// <summary>
    /// Validates the language element semantics.
    /// </summary>
    public override void Validate() {
      this.Init?.Validate();
      this.Condition?.Validate();
      this.Next?.Validate();
      this.Body.Validate();
    }

    /// <summary>
    /// Emits the code and data.
    /// </summary>
    /// <param name="emitter">The emitter.</param>
    /// <param name="exits">The non-exceptional exit targets.</param>
    public override void Emit(Emitter emitter, Exits exits) {
      var section1 = emitter.Section(); // <- init
      var section2 = emitter.Section(); // <- loop condition
      var section3 = emitter.Section(); // <- loop body
      var section4 = emitter.Section().At(null); // <- continue jumps here
      var section5 = emitter.Section(); // <- loop increment
      var section6 = emitter.Section().At(null); // <- jump back to start
      var section7 = emitter.Section().At(null); // <- break jumps here
      var end = emitter.Section();

      if (this.Init != null) {
        section1.Emit(this.Init, exits);
      }

      if (this.Condition != null) {
        section2.Emit(this.Condition, exits);
        section2.Emit(Opcode.ConditionalJump, param: 0, target: end);
      }

      section3.Emit(this.Body, new Exits(exits) { Continue = section4, Break = section7 });

      // continue jumps here

      if (this.Next != null) {
        section5.Emit(this.Next, exits);
        section5.Emit(Opcode.Drop, value: 1);
      }

      section6.Emit(Opcode.Jump, target: section2);

      // break jumps here

      // end
    }
  }

  /// <summary>
  /// An abstract base class for jump statements.
  /// </summary>
  private abstract class JumpStatement : Statement {
    /// <summary>
    /// Validates the language element semantics.
    /// </summary>
    public override void Validate() {
      for (var s = this.Scope; s != null; s = s.Scope) {
        if (s is LoopScope) {
          return;
        }

        if (s is FunctionScope) {
          break; // <- cannot jump across function boundary
        }
      }

      throw this.Token.Error($"'{this.Token}' can only appear inside a loop");
    }
  }

  /// <summary>
  /// Break statement.
  /// </summary>
  private sealed class BreakStatement : JumpStatement {
    /// <summary>
    /// Emits the code and data.
    /// </summary>
    /// <param name="emitter">The emitter.</param>
    /// <param name="exits">The non-exceptional exit targets.</param>
    public override void Emit(Emitter emitter, Exits exits) {
      Debug.Assert(exits.Break != null);
      emitter.Emit(Opcode.Jump, target: exits.Break);
    }
  }

  /// <summary>
  /// Continue statement.
  /// </summary>
  private sealed class ContinueStatement : JumpStatement {
    /// <summary>
    /// Emits the code and data.
    /// </summary>
    /// <param name="emitter">The emitter.</param>
    /// <param name="exits">The non-exceptional exit targets.</param>
    public override void Emit(Emitter emitter, Exits exits) {
      Debug.Assert(exits.Continue != null);
      emitter.Emit(Opcode.Jump, target: exits.Continue);
    }
  }

  /// <summary>
  /// Return statement.
  /// </summary>
  private sealed class ReturnStatement : Statement {
    /// <summary>
    /// The return value.
    /// </summary>
    public Expression Value { get; set; }

    /// <summary>
    /// Validates the language element semantics.
    /// </summary>
    public override void Validate() {
      this.Value?.Validate();

      for (var s = this.Scope; s != null; s = s.Scope) {
        if (s is FunctionScope && s.Scope != null) {
          return;
        }
      }

      throw this.Token.Error("'return' can only appear inside a function");
    }

    /// <summary>
    /// Emits the code and data.
    /// </summary>
    /// <param name="emitter">The emitter.</param>
    /// <param name="exits">The non-exceptional exit targets.</param>
    public override void Emit(Emitter emitter, Exits exits) {
      Debug.Assert(exits.Return != null);

      if (this.Value != null) {
        emitter.Emit(this.Value, exits);
      } else {
        emitter.Emit(Opcode.Null);
      }

      emitter.Emit(Opcode.Jump, target: exits.Return);
    }
  }
}
