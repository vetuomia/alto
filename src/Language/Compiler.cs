using System.Diagnostics;

/// <summary>
/// Alto language compiler.
/// </summary>
static partial class Compiler {
  /// <summary>
  /// Compiles the source text into a module.
  /// </summary>
  /// <param name="sourceText">The source text.</param>
  /// <param name="module">The source module.</param>
  /// <returns>The compiled module.</returns>
  public static Module Compile(string sourceText, string module = "") {
    var parser = new Parser(sourceText, module);

    // -------------------------------------------------------------------------

    LexicalScope scope = new FunctionScope(null) { Token = parser.Next };

    void EnterFunctionScope(Token token) {
      scope = new FunctionScope(scope) { Token = token };
    }

    void EnterBlockScope(Token token) {
      scope = new BlockScope(scope) { Token = token };
    }

    void EnterLoopScope(Token token) {
      scope = new LoopScope(scope) { Token = token };
    }

    void LeaveScope() {
      scope = scope.Scope;
      Debug.Assert(scope != null);
    }

    // -------------------------------------------------------------------------

    Slot MakeSlot(string name) {
      foreach (var found in scope.SlotsInScope) {
        if (found.Name == name) {
          throw parser.Error($"'{name}' is already defined in this scope");
        }
      }

      var slot = new Slot { Name = name, Scope = scope };
      scope.Slots.Add(slot);
      return slot;
    }

    Slot FindSlot(string name) {
      foreach (var found in scope.SlotsInScope) {
        if (found.Name == name) {
          return found;
        }
      }

      throw parser.Error($"Undefined '{name}'");
    }

    // -------------------------------------------------------------------------

    // Identifier declaration
    parser.Syntax.Declaration(Token.Word).Do((token, context) => new Identifier(MakeSlot(token.Text)) {
      Token = token,
      Scope = scope,
    });

    // -------------------------------------------------------------------------

    // Grouping: ( expr )
    // Function expression: ( ) => expr_or_block
    // Function expression: ( ...ident ) => expr_or_block
    // Function expression: ( ident , ... ) => expr_or_block
    parser.Syntax.Primitive("(").Do((token, context) => {
      var isFunction =
        parser.Match(")", "=>") ||
        parser.Match(Token.Word, ",") ||
        parser.Match(Token.Word, ")", "=>") ||
        parser.Match("...", Token.Word, ")", "=>");

      if (isFunction) {
        var expression = new FunctionExpression {
          Token = token,
          Scope = scope,
        };

        EnterFunctionScope(token);
        {
          parser.RepeatZeroOrMoreWithSeparatorUntil(")", ",", delegate {
            var isRestParameter = parser.Optional("...");

            if (parser.Declaration(context) is Identifier parameter) {
              var index = expression.Parameters.Count;
              expression.Parameters.Add(parameter);

              parameter.Slot.Kind = SlotKind.Parameter;
              parameter.Slot.ReadOnly = true; // <- parameters are always read-only

              if (isRestParameter) {
                parameter.Slot.Source = SlotSource.ArgumentSlice;
                parameter.Slot.SourceIndex = index;
                parser.Next.Require(")", "The rest parameter must be the last");
              } else {
                parameter.Slot.Source = SlotSource.Argument;
                parameter.Slot.SourceIndex = index;
              }
            } else {
              throw parser.Error("Expected a parameter name");
            }
          });

          var arrow = parser.Required("=>");

          if (parser.Optional("{") is Token begin) {
            expression.Body = new Block {
              Token = begin,
              Scope = scope,
            };

            parser.RepeatZeroOrMoreUntil("}", delegate {
              expression.Body.Statements.Add(parser.Statement(context) as Statement);
            });
          } else if (parser.Expression(context) is Expression value) {
            expression.Body = new Block {
              Token = arrow,
              Scope = scope,
              Statements = {
                new ReturnStatement {
                  Token = arrow,
                  Scope = scope,
                  Value = value,
                },
              },
            };
          } else {
            throw parser.Error("Expected an expression after '=>'");
          }
        }
        LeaveScope();

        return expression;
      } else {
        var inner = parser.Expression(context);
        parser.Required(")");
        return inner;
      }
    });

    // -------------------------------------------------------------------------

    // Identifier reference: ident
    // Function expression: ident => expr_or_block
    parser.Syntax.Primitive(Token.Word).Do((token, context) => {
      if (parser.Optional("=>") is Token arrow) {
        var expression = new FunctionExpression {
          Token = token,
          Scope = scope,
        };

        EnterFunctionScope(token);
        {
          var parameter = new Identifier(MakeSlot(token.Text)) {
            Token = token,
            Scope = scope,
          };

          expression.Parameters.Add(parameter);
          parameter.Slot.Kind = SlotKind.Parameter;
          parameter.Slot.ReadOnly = true; // <- parameters are always read-only
          parameter.Slot.Source = SlotSource.Argument;
          parameter.Slot.SourceIndex = 0;

          if (parser.Optional("{") is Token begin) {
            expression.Body = new Block {
              Token = begin,
              Scope = scope,
            };

            parser.RepeatZeroOrMoreUntil("}", delegate {
              expression.Body.Statements.Add(parser.Statement(context) as Statement);
            });
          } else if (parser.Expression(context) is Expression value) {
            expression.Body = new Block {
              Token = arrow,
              Scope = scope,
              Statements = {
                  new ReturnStatement {
                    Token = arrow,
                    Scope = scope,
                    Value = value,
                  },
                },
            };
          } else {
            throw parser.Error("Expected an expression after '=>'");
          }
        }
        LeaveScope();

        return expression;
      } else {
        return new Identifier(FindSlot(token.Text)) {
          Token = token,
          Scope = scope,
        };
      }
    });

    // -------------------------------------------------------------------------

    // Receiver literal: this
    parser.Syntax.Primitive("this").Do((token, context) => new ReceiverExpression {
      Token = token,
      Scope = scope,
    });

    // -------------------------------------------------------------------------

    // Null literal: null
    parser.Syntax.Primitive("null").Do((token, context) => new NullLiteral {
      Token = token,
      Scope = scope,
    });

    // -------------------------------------------------------------------------

    // Boolean literal: true
    parser.Syntax.Primitive("true", "false").Do((token, context) => new BooleanLiteral {
      Token = token,
      Scope = scope,
      Value = token.Text == "true",
    });

    // -------------------------------------------------------------------------

    // Number literal: 3.14
    parser.Syntax.Primitive(Token.Number).Do((token, context) => new NumberLiteral {
      Token = token,
      Scope = scope,
    });

    // -------------------------------------------------------------------------

    // String literal: "string"
    parser.Syntax.Primitive(Token.String).Do((token, context) => new StringLiteral {
      Token = token,
      Scope = scope,
    });

    // -------------------------------------------------------------------------

    // List expression: [ expr, expr, ... ]
    parser.Syntax.Primitive("[").Do((token, context) => {
      var expression = new ListExpression {
        Token = token,
        Scope = scope,
      };

      parser.RepeatZeroOrMoreWithTrailingSeparatorUntil("]", ",", delegate {
        if (parser.Expression(context) is Expression value) {
          expression.Values.Add(value);
        } else {
          throw parser.Error("Expected an expression");
        }
      });

      return expression;
    });

    // -------------------------------------------------------------------------

    // Table expression: { expr : expr, expr : expr, ... }
    parser.Syntax.Primitive("{").Do((token, context) => {
      var expression = new TableExpression {
        Token = token,
        Scope = scope,
      };

      parser.RepeatZeroOrMoreWithTrailingSeparatorUntil("}", ",", delegate {
        Expression key;

        if (parser.Optional("[")) {
          if (parser.Expression(context) is Expression selector) {
            key = selector;
          } else {
            throw parser.Error("Expected an expression after '['");
          }

          parser.Required("]");
        } else {
          key = new StringLiteral {
            Token = parser.Required(Token.Word, "Expected a member name before ':'"),
            Scope = scope,
          };
        }

        parser.Required(":");

        if (parser.Expression(context) is Expression value) {
          expression.Values.Add((key, value));
        } else {
          throw parser.Error("Expected an expression after ':'");
        }
      });

      return expression;
    });

    // -------------------------------------------------------------------------

    // Throw expression: throw expr
    parser.Syntax.Primitive("throw").Do((token, context) => {
      var expression = new ThrowExpression {
        Token = token,
        Scope = scope,
      };

      if (parser.Expression(context) is Expression value) {
        expression.Value = value;
      } else {
        throw parser.Error("Expected an expression after throw");
      }

      return expression;
    });

    // -------------------------------------------------------------------------

    // Function expression: function ( ident, ident, ... ) { statement; statement; ... }
    parser.Syntax.Primitive("function").Do((token, context) => {
      var expression = new FunctionExpression {
        Token = token,
        Scope = scope,
      };

      EnterFunctionScope(token);
      {
        parser.Required("(");
        parser.RepeatZeroOrMoreWithSeparatorUntil(")", ",", delegate {
          var isRestParameter = parser.Optional("...");

          if (parser.Declaration(context) is Identifier parameter) {
            var index = expression.Parameters.Count;
            expression.Parameters.Add(parameter);

            parameter.Slot.Kind = SlotKind.Parameter;
            parameter.Slot.ReadOnly = true; // <- parameters are always read-only

            if (isRestParameter) {
              parameter.Slot.Source = SlotSource.ArgumentSlice;
              parameter.Slot.SourceIndex = index;
              parser.Next.Require(")", "The rest parameter must be the last");
            } else {
              parameter.Slot.Source = SlotSource.Argument;
              parameter.Slot.SourceIndex = index;
            }
          } else {
            throw parser.Error("Expected a parameter name");
          }
        });

        expression.Body = new Block {
          Token = parser.Required("{"),
          Scope = scope,
        };

        parser.RepeatZeroOrMoreUntil("}", delegate {
          expression.Body.Statements.Add(parser.Statement(context) as Statement);
        });
      }
      LeaveScope();

      return expression;
    });

    // -------------------------------------------------------------------------

    // Function call: expr ( expr, expr, ... )
    parser.Syntax.Left(90, "(").Do((token, power, left, context) => {
      var expression = new FunctionCallExpression {
        Token = token,
        Scope = scope,
      };

      if (left is Expression function) {
        expression.Function = function;
      } else {
        throw parser.Error("Expected an expression before '('");
      }

      parser.RepeatZeroOrMoreWithSeparatorUntil(")", ",", delegate {
        if (parser.Expression(context) is Expression argument) {
          expression.Arguments.Add(argument);
        } else {
          throw parser.Error("Expected an expression");
        }
      });

      return expression;
    });

    // -------------------------------------------------------------------------

    // Member access: expr [ expr ]
    parser.Syntax.Left(90, "[").Do((token, power, left, context) => {
      var expression = new MemberAccessExpression {
        Token = token,
        Scope = scope,
      };

      if (left is Expression container) {
        expression.Container = container;
      } else {
        throw parser.Error("Expected an expression before '['");
      }

      if (parser.Expression(context) is Expression selector) {
        expression.Selector = selector;
      } else {
        throw parser.Error("Expected an expression after '['");
      }

      parser.Required("]");

      return expression;
    });

    // -------------------------------------------------------------------------

    // Member access: expr . ident
    parser.Syntax.Left(90, ".").Do((token, power, left, context) => {
      var expression = new MemberAccessExpression {
        Token = token,
        Scope = scope,
      };

      if (left is Expression container) {
        expression.Container = container;
      } else {
        throw parser.Error("Expected an expression before '.'");
      }

      expression.Selector = new StringLiteral {
        Token = parser.Required(Token.Word, "Expected a member name after '.'"),
        Scope = scope,
      };

      return expression;
    });

    // -------------------------------------------------------------------------

    // Unary expression: - expr
    parser.Syntax.Unary(80, "+", "-", "!").Do((token, power, context) => {
      var expression = new UnaryExpression {
        Token = token,
        Scope = scope,
      };

      if (parser.Expression(context, power) is Expression right) {
        expression.Right = right;
      } else {
        throw parser.Error($"Expected an expression after '{token}'");
      }

      return expression;
    });

    // -------------------------------------------------------------------------

    // Binary expression: expr + expr
    Parser.BinarySyntaxRule binaryExpression = (token, power, left, context) => {
      var expression = new BinaryExpression {
        Token = token,
        Scope = scope,
      };

      if (left is Expression before) {
        expression.Left = before;
      } else {
        throw parser.Error($"Expected an expression before '{token}'");
      }

      if (parser.Expression(context, power) is Expression right) {
        expression.Right = right;
      } else {
        throw parser.Error($"Expected an expression after '{token}'");
      }

      return expression;
    };

    parser.Syntax.Left(70, "*", "/", "%").Do(binaryExpression);
    parser.Syntax.Left(65, "+", "-").Do(binaryExpression);
    parser.Syntax.Left(60, "<", "<=", ">", ">=").Do(binaryExpression);
    parser.Syntax.Left(55, "&").Do(binaryExpression);
    parser.Syntax.Left(50, "^").Do(binaryExpression);
    parser.Syntax.Left(45, "|").Do(binaryExpression);
    parser.Syntax.Left(40, "==", "!=").Do(binaryExpression);

    // -------------------------------------------------------------------------

    // Conditional binary expression: expr || expr
    Parser.BinarySyntaxRule conditionalBinaryExpression = (token, power, left, context) => {
      var expression = new ConditionalBinaryExpression {
        Token = token,
        Scope = scope,
      };

      if (left is Expression before) {
        expression.Left = before;
      } else {
        throw parser.Error($"Expected an expression before '{token}'");
      }

      if (parser.Expression(context, power) is Expression right) {
        expression.Right = right;
      } else {
        throw parser.Error($"Expected an expression after '{token}'");
      }

      return expression;
    };

    parser.Syntax.Left(35, "&&").Do(conditionalBinaryExpression);
    parser.Syntax.Left(30, "||").Do(conditionalBinaryExpression);

    // -------------------------------------------------------------------------

    // Conditional ternary expression: expr ? expr : expr
    parser.Syntax.Right(20, "?").Do((token, power, left, context) => {
      var expression = new ConditionalTernaryExpression {
        Token = token,
        Scope = scope,
      };

      if (left is Expression condition) {
        expression.Condition = condition;
      } else {
        throw parser.Error("Expected an expression before '?'");
      }

      if (parser.Expression(context, power) is Expression whenTrue) {
        expression.True = whenTrue;
      } else {
        throw parser.Error("Expected an expression after '?'");
      }

      parser.Required(":");

      if (parser.Expression(context, power) is Expression whenFalse) {
        expression.False = whenFalse;
      } else {
        throw parser.Error("Expected an expression after ':'");
      }

      return expression;
    });

    // -------------------------------------------------------------------------

    // Assignment expression: ident = expr
    parser.Syntax.Right(10, "=", "+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=").Do((token, power, left, context) => {
      var expression = new AssignmentExpression {
        Token = token,
        Scope = scope,
      };

      if (left is AssignableExpression target) {
        expression.Target = target;
      } else {
        throw parser.Error($"Expected a variable or a member expression before '{token}'");
      }

      if (parser.Expression(context, power) is Expression value) {
        expression.Value = value;
      } else {
        throw parser.Error($"Expected an expression after '{token}'");
      }

      return expression;
    });

    // -------------------------------------------------------------------------

    // Expression statement, e.g. an assignment or function call.
    parser.Syntax.ExpressionStatement((token, context) => {
      var statement = new ExpressionStatement {
        Token = token,
        Scope = scope,
      };

      var expression = parser.Expression(context);

      switch (expression) {
        case AssignmentExpression _:
        case FunctionCallExpression _:
        case ThrowExpression _:
          statement.Expression = expression as Expression;
          break;

        default:
          throw parser.Error("Expected an assignment, a function call or throw");
      }

      parser.Optional(";");

      return statement;
    });

    // -------------------------------------------------------------------------

    // Block statement: { statement; statement; ... }
    parser.Syntax.Statement("{").Do((token, context) => {
      EnterBlockScope(token);
      var statement = new Block {
        Token = scope.Token,
        Scope = scope,
      };

      parser.RepeatZeroOrMoreUntil("}", delegate {
        statement.Statements.Add(parser.Statement(context) as Statement);
      });
      LeaveScope();

      return statement;
    });

    // -------------------------------------------------------------------------

    // Declaration statement: var ident = expr ;
    parser.Syntax.Statement("var", "const").Do((token, context) => {
      var statement = new DeclarationStatement {
        Token = token,
        Scope = scope,
      };

      if (parser.Declaration(context) is Identifier identifier) {
        identifier.Slot.Kind = SlotKind.Variable;
        identifier.Slot.Storage = SlotStorage.Local;
        identifier.Slot.ReadOnly = (token.Text == "const");
        statement.Identifier = identifier;
      } else {
        throw parser.Error("Expected a name before '='");
      }

      parser.Required("=");

      if (parser.Expression(context) is Expression expression) {
        statement.Value = expression;
      } else {
        throw parser.Error("Expected an expression after '='");
      }

      if (expression is FunctionExpression function) {
        function.Name = identifier.Name;
      }

      parser.Optional(";");

      return statement;
    });

    // -------------------------------------------------------------------------

    // Import statement: import ident from "string"
    parser.Syntax.Statement("import").Do((token, context) => {
      var statement = new ImportStatement {
        Token = token,
        Scope = scope,
      };

      if (parser.Declaration(context) is Identifier identifier) {
        identifier.Slot.Kind = SlotKind.Import;
        identifier.Slot.Storage = SlotStorage.Global;
        identifier.Slot.ReadOnly = true;  // <- imports are always read-only
        statement.Identifier = identifier;
      } else {
        throw parser.Error("Expected a name after 'import'");
      }

      parser.Required("from");

      if (parser.Expression(context) is StringLiteral literal) {
        statement.Source = literal.Value;
      } else {
        throw parser.Error("Expected a string literal after 'from'");
      }

      parser.Optional(";");

      return statement;
    });

    // -------------------------------------------------------------------------

    // Export statement: export const ident = expr ;
    parser.Syntax.Statement("export").Do((token, context) => {
      var statement = new ExportStatement {
        Token = token,
        Scope = scope,
      };

      parser.Required("const", "Exports must be marked as const");

      if (parser.Declaration(context) is Identifier identifier) {
        identifier.Slot.Kind = SlotKind.Variable;
        identifier.Slot.Storage = SlotStorage.Local;
        identifier.Slot.ReadOnly = true;  // <- exports are always read-only
        statement.Identifier = identifier;
      } else {
        throw parser.Error("Expected a name before '='");
      }

      parser.Required("=");

      if (parser.Expression(context) is Expression expression) {
        statement.Value = expression;
      } else {
        throw parser.Error("Expected an expression after '='");
      }

      if (expression is FunctionExpression function) {
        function.Name = identifier.Name;
      }

      parser.Optional(";");

      return statement;
    });

    // -------------------------------------------------------------------------

    // If statement: if ( expr ) { statement; statement; ... } else { statement; statement; ... }
    parser.Syntax.Statement("if").Do((token, context) => {
      var statement = new IfStatement {
        Token = token,
        Scope = scope,
      };

      parser.Required("(");

      if (parser.Expression(context) is Expression condition) {
        statement.Condition = condition;
      } else {
        throw parser.Error("Expected an expression after '('");
      }

      parser.Required(")");

      EnterBlockScope(parser.Required("{"));
      statement.True = new Block {
        Token = scope.Token,
        Scope = scope,
      };

      parser.RepeatZeroOrMoreUntil("}", delegate {
        statement.True.Statements.Add(parser.Statement(context) as Statement);
      });
      LeaveScope();

      if (parser.Optional("else") is Token e) {
        if (parser.Next.Is("if")) { // <- allow inlined if after else
          statement.False = new Block {
            Token = e,
            Scope = scope,
          };

          statement.False.Statements.Add(parser.Statement(context) as Statement);
        } else {
          EnterBlockScope(parser.Required("{"));
          statement.False = new Block {
            Token = scope.Token,
            Scope = scope,
          };

          parser.RepeatZeroOrMoreUntil("}", delegate {
            statement.False.Statements.Add(parser.Statement(context) as Statement);
          });
          LeaveScope();
        }
      }

      return statement;
    });

    // -------------------------------------------------------------------------

    // Try-catch-finally statement: try { ... } catch ( ident ) { ... } finally { ... }
    parser.Syntax.Statement("try").Do((token, context) => {
      EnterBlockScope(token);

      EnterBlockScope(parser.Required("{"));
      var tryBlock = new Block {
        Token = scope.Token,
        Scope = scope,
      };

      parser.RepeatZeroOrMoreUntil("}", delegate {
        tryBlock.Statements.Add(parser.Statement(context) as Statement);
      });
      LeaveScope();

      if (parser.Optional("catch") is Token catchToken) {
        var tryCatch = new TryCatchStatement {
          Token = token,
          Scope = scope,
          Try = tryBlock,
        };

        EnterBlockScope(catchToken); // <- catch block starts here to capture the exception variable
        if (parser.Optional("(")) {
          if (parser.Declaration(context) is Identifier identifier) {
            identifier.Slot.Kind = SlotKind.Variable;
            identifier.Slot.Storage = SlotStorage.Local;
            identifier.Slot.ReadOnly = true; // <- exception variables are always read-only
            tryCatch.Exception = identifier;
          }

          parser.Required(")");
        }

        tryCatch.Catch = new Block {
          Token = parser.Required("{"),
          Scope = scope,
        };

        parser.RepeatZeroOrMoreUntil("}", delegate {
          tryCatch.Catch.Statements.Add(parser.Statement(context) as Statement);
        });
        LeaveScope();

        if (parser.Optional("finally") is Token finallyToken) {
          var tryFinally = new TryFinallyStatement {
            Token = finallyToken,
            Scope = scope,
            Try = new Block {
              Token = finallyToken,
              Scope = scope,
              Statements = { tryCatch },
            },
          };

          EnterBlockScope(parser.Required("{"));
          tryFinally.Finally = new Block {
            Token = scope.Token,
            Scope = scope,
          };

          parser.RepeatZeroOrMoreUntil("}", delegate {
            tryFinally.Finally.Statements.Add(parser.Statement(context) as Statement);
          });
          LeaveScope();

          LeaveScope(); // <- try scope
          return tryFinally;
        } else {
          LeaveScope(); // <- try scope
          return tryCatch;
        }
      } else {
        parser.Required("finally");

        var tryFinally = new TryFinallyStatement {
          Token = token,
          Scope = scope,
          Try = tryBlock,
        };

        EnterBlockScope(parser.Required("{"));
        tryFinally.Finally = new Block {
          Token = scope.Token,
          Scope = scope,
        };

        parser.RepeatZeroOrMoreUntil("}", delegate {
          tryFinally.Finally.Statements.Add(parser.Statement(context) as Statement);
        });
        LeaveScope();

        LeaveScope(); // <- try scope
        return tryFinally;
      }
    });

    // -------------------------------------------------------------------------

    // While statement: while ( expr ) { statement; statement; ... }
    parser.Syntax.Statement("while").Do((token, context) => {
      var statement = new WhileStatement {
        Token = token,
        Scope = scope,
      };

      parser.Required("(");

      if (parser.Expression(context) is Expression condition) {
        statement.Condition = condition;
      } else {
        throw parser.Error("Expected an expression after '('");
      }

      parser.Required(")");

      EnterLoopScope(parser.Required("{"));
      statement.Body = new Block {
        Token = scope.Token,
        Scope = scope,
      };

      parser.RepeatZeroOrMoreUntil("}", delegate {
        statement.Body.Statements.Add(parser.Statement(context) as Statement);
      });
      LeaveScope();

      return statement;
    });

    // -------------------------------------------------------------------------

    // For statement: for ( statement ; expr ; expr ) { statement; statement; ... }
    parser.Syntax.Statement("for").Do((token, context) => {
      var statement = new ForStatement {
        Token = token,
        Scope = scope,
      };

      EnterBlockScope(parser.Required("(")); // <- for scope begins here to capture the declarations in the init statement
      {
        if (!parser.Optional(";")) {
          if (parser.Statement(context) is SimpleStatement init) {
            statement.Init = init;
          } else {
            throw parser.Error("Expected a declaration or expression after '('");
          }
        }

        if (parser.Next.IsNot(";")) {
          if (parser.Expression(context) is Expression condition) {
            statement.Condition = condition;
          } else {
            throw parser.Error("Expected an expression after ';'");
          }
        }

        parser.Required(";");

        if (parser.Next.IsNot(")")) {
          if (parser.Expression(context) is Expression next) {
            statement.Next = next;
          } else {
            throw parser.Error("Expected an expression before ')'");
          }
        }

        parser.Required(")");

        EnterLoopScope(parser.Required("{"));
        statement.Body = new Block {
          Token = scope.Token,
          Scope = scope,
        };

        parser.RepeatZeroOrMoreUntil("}", delegate {
          statement.Body.Statements.Add(parser.Statement(context) as Statement);
        });
        LeaveScope();
      }
      LeaveScope(); // <- for scope ends here

      return statement;
    });

    // -------------------------------------------------------------------------

    // Break statement: break ;
    parser.Syntax.Statement("break").Do((token, context) => {
      parser.Optional(";");

      return new BreakStatement {
        Token = token,
        Scope = scope,
      };
    });

    // -------------------------------------------------------------------------

    // Continue statement: continue ;
    parser.Syntax.Statement("continue").Do((token, context) => {
      parser.Optional(";");

      return new ContinueStatement {
        Token = token,
        Scope = scope,
      };
    });

    // -------------------------------------------------------------------------

    // Return statement: return expr ;
    parser.Syntax.Statement("return").Do((token, context) => {
      var statement = new ReturnStatement {
        Token = token,
        Scope = scope,
      };

      if (parser.Next.IsNot(";", "}")) {
        if (parser.Expression(context) is Expression expression) {
          statement.Value = expression;
        } else {
          throw parser.Error("Expected an expression after 'return'");
        }
      }

      parser.Optional(";");

      return statement;
    });

    // -------------------------------------------------------------------------

    // Closers, punctuation and keywords that cannot appear by themselves
    parser.Syntax.Primitive(",", ";", ":", ")", "]", "}", "else", "from", "catch", "finally");

    // -------------------------------------------------------------------------

    var script = new Block {
      Scope = scope,
      Token = parser.Next,
    };

    parser.RepeatZeroOrMoreUntil(Token.End, delegate {
      script.Statements.Add(parser.Statement(null) as Statement);
    });

    script.Validate();

    scope.Validate();

    return new Emitter(script.Token.SourceText).Emit(script, null).Assemble();
  }
}
