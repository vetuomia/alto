using System;
using System.Collections.Generic;

/// <summary>
/// Generic top down parser.
/// </summary>
sealed class Parser : Parser.ISyntaxBuilder {
  /// <summary>
  /// The expression statement rule identifier.
  /// </summary>
  private const string ExpressionStatement = "(expression statement)";

  /// <summary>
  /// The syntax rules.
  /// </summary>
  private readonly Dictionary<string, SyntaxRules> syntaxRules = new Dictionary<string, SyntaxRules> {
    [Token.End] = new SyntaxRules { UnaryPower = -1, LeftPower = -1, RightPower = -1 },
  };

  /// <summary>
  /// The source tokens.
  /// </summary>
  private readonly Queue<Token> tokens;

  /// <summary>
  /// The last processed token.
  /// </summary>
  private Token last;

  /// <summary>
  /// The syntax builder.
  /// </summary>
  public ISyntaxBuilder Syntax => this;

  /// <summary>
  /// The last token.
  /// </summary>
  public Token Last => this.last;

  /// <summary>
  /// The next token.
  /// </summary>
  public Token Next => this.tokens.Count > 0 ? this.tokens.Peek() : this.last;

  /// <summary>
  /// Initializes a new instance of the class.
  /// </summary>
  /// <param name="sourceText">The source text.</param>
  /// <param name="fileName">The file name.</param>
  public Parser(string sourceText, string fileName) => this.tokens = Token.Split(sourceText, fileName);

  /// <summary>
  /// Removes the next token.
  /// </summary>
  public Token Advance() => this.tokens.Count > 0 ? (this.last = this.tokens.Dequeue()) : this.last;

  /// <summary>
  /// Removes the next token and requires that it matches.
  /// </summary>
  /// <param name="token">The expected token.</param>
  /// <param name="message">The optional error message.</param>
  public Token Required(string token, string message = null) => this.Next.Is(token) ? this.Advance() : throw this.Next.Error(message ?? $"Expected '{token}' after '{this.last}'");

  /// <summary>
  /// If the next token matches, removes it and returns true; otherwise false.
  /// </summary>
  /// <param name="token">The expected token.</param>
  public Token Optional(string token) => this.Next.Is(token) ? this.Advance() : null;

  /// <summary>
  /// Determines whether the token sequence matches.
  /// </summary>
  /// <param name="tokens">The token sequence.</param>
  public bool Match(params string[] tokens) {
    var matched = 0;

    using (var e = this.tokens.GetEnumerator()) {
      while (matched < tokens.Length && e.MoveNext()) {
        if (e.Current.IsNot(tokens[matched++])) {
          return false;
        }
      }
    }

    return matched == tokens.Length;
  }

  /// <summary>
  /// Returns a parse error.
  /// </summary>
  /// <param name="message">The error message.</param>
  public ParseError Error(string message) => this.last.Error(message);

  /// <summary>
  /// Parses a 0..N sequence of tokens until the end token: a b c )
  /// </summary>
  /// <param name="end">The end token.</param>
  /// <param name="each">The to call for each item.</param>
  public void RepeatZeroOrMoreUntil(string end, Action each) {
    while (this.Next.IsNot(end)) {
      each();
    }

    this.Required(end);
  }

  /// <summary>
  /// Parses a 0..N sequence of tokens with separators until the end token: a , b , c )
  /// </summary>
  /// <param name="separator">The separator token.</param>
  /// <param name="end">The end token.</param>
  /// <param name="each">The action to call for each item.</param>
  public void RepeatZeroOrMoreWithSeparatorUntil(string end, string separator, Action each) {
    if (this.Next.IsNot(end)) {
      do {
        each();
      } while (this.Optional(separator));
    }

    this.Required(end);
  }

  /// <summary>
  /// Parses a 0..N sequence of tokens with separators until the end token, allowing a trailing separator: a , b , c , )
  /// </summary>
  /// <param name="end">The end token.</param>
  /// <param name="separator">The separator token.</param>
  /// <param name="each">The action to call for each item.</param>
  public void RepeatZeroOrMoreWithTrailingSeparatorUntil(string end, string separator, Action each) {
    do {
      if (this.Next.Is(end)) {
        break;
      }

      each();
    } while (this.Optional(separator));

    this.Required(end);
  }

  /// <summary>
  /// Parses a 1..N sequence of tokens until the end token: a b c )
  /// </summary>
  /// <param name="end">The end token.</param>
  /// <param name="each">The to call for each item.</param>
  public void RepeatOneOrMoreUntil(string end, Action each) {
    do {
      each();
    } while (this.Next.IsNot(end));

    this.Required(end);
  }

  /// <summary>
  /// Parses a 1..N sequence of tokens with separators: a , b , c
  /// </summary>
  /// <param name="separator">The separator token.</param>
  /// <param name="each">The to call for each item.</param>
  public void RepeatOneOrMoreWithSeparator(string separator, Action each) {
    do {
      each();
    } while (this.Optional(separator));
  }

  /// <summary>
  /// Parses a 1..N sequence of tokens with separators until the end token: a , b , c )
  /// </summary>
  /// <param name="end">The end token.</param>
  /// <param name="separator">The separator token.</param>
  /// <param name="each">The action to call for each item.</param>
  public void RepeatOneOrMoreWithSeparatorUntil(string end, string separator, Action each) {
    do {
      each();

      if (this.Next.Is(end)) {
        break;
      }
    } while (this.Optional(separator));

    this.Required(end);
  }

  /// <summary>
  /// Parses a primitive.
  /// </summary>
  /// <param name="context">The optional parse context.</param>
  public object Primitive(object context) {
    var syntax = this.GetSyntax(this.Next);
    return syntax.UnaryRule(this.Advance(), syntax.UnaryPower, context);
  }

  /// <summary>
  /// Parses an expression.
  /// </summary>
  /// <param name="context">The parse context.</param>
  /// <param name="power">The binding power.</param>
  public object Expression(object context, int power = 0) {
    var syntax = this.GetSyntax(this.Next);
    var result = syntax.UnaryRule(this.Advance(), syntax.UnaryPower, context);

    while (true) {
      syntax = this.GetSyntax(this.Next);

      if (syntax.LeftPower <= power) {
        break;
      }

      result = syntax.BinaryRule(this.Advance(), syntax.RightPower, result, context);
    }

    return result;
  }

  /// <summary>
  /// Parses a declaration.
  /// </summary>
  /// <param name="context">The parse context.</param>
  public object Declaration(object context) {
    var syntax = this.GetSyntax(this.Next);
    return syntax.DeclarationRule(this.Advance(), context);
  }

  /// <summary>
  /// Parses a statement.
  /// </summary>
  /// <param name="context">The parse context.</param>
  public object Statement(object context) {
    var syntax = this.GetSyntax(this.Next);

    if (syntax.StatementRule != null) {
      return syntax.StatementRule(this.Advance(), context);
    }

    syntax = this.GetOrAddSyntax(ExpressionStatement);

    if (syntax.StatementRule != null) {
      return syntax.StatementRule(this.Next, context);
    }

    throw this.Next.Error("Expected a statement");
  }

  /// <summary>
  /// Defines a syntax rule for unary tokens. (e.g. aritmetic prefix operators)
  /// </summary>
  /// <param name="power">The binding power.</param>
  /// <param name="tokens">The tokens.</param>
  ISyntaxRuleBuilder<UnarySyntaxRule> ISyntaxBuilder.Unary(int power, params string[] tokens) {
    foreach (var token in tokens) {
      this.GetOrAddSyntax(token);
    }

    return new SyntaxRuleBuilder<UnarySyntaxRule>((rule) => {
      foreach (var token in tokens) {
        var info = this.GetOrAddSyntax(token);
        info.UnaryPower = power;
        info.UnaryRule = rule;
      }
    });
  }

  /// <summary>
  /// Defines a syntax rule for left associative binary tokens. (e.g. aritmetic infix operators)
  /// </summary>
  /// <param name="power">The binding power.</param>
  /// <param name="tokens">The tokens.</param>
  ISyntaxRuleBuilder<BinarySyntaxRule> ISyntaxBuilder.Left(int power, params string[] tokens) {
    foreach (var token in tokens) {
      this.GetOrAddSyntax(token);
    }

    return new SyntaxRuleBuilder<BinarySyntaxRule>((rule) => {
      foreach (var token in tokens) {
        var syntax = this.GetOrAddSyntax(token);
        syntax.LeftPower = power;
        syntax.RightPower = power;
        syntax.BinaryRule = rule;
      }
    });
  }

  /// <summary>
  /// Defines a syntax rule for right associative binary tokens. (e.g. assignment operators)
  /// </summary>
  /// <param name="power">The binding power.</param>
  /// <param name="tokens">The tokens.</param>
  ISyntaxRuleBuilder<BinarySyntaxRule> ISyntaxBuilder.Right(int power, params string[] tokens) {
    foreach (var token in tokens) {
      this.GetOrAddSyntax(token);
    }

    return new SyntaxRuleBuilder<BinarySyntaxRule>((rule) => {
      foreach (var token in tokens) {
        var syntax = this.GetOrAddSyntax(token);
        syntax.LeftPower = power;
        syntax.RightPower = power - 1;
        syntax.BinaryRule = rule;
      }
    });
  }

  /// <summary>
  /// Defines a syntax rule for primitive tokens. (e.g. literals and semantic symbols)
  /// </summary>
  /// <param name="tokens">The tokens.</param>
  ISyntaxRuleBuilder<SyntaxRule> ISyntaxBuilder.Primitive(params string[] tokens) {
    foreach (var token in tokens) {
      this.GetOrAddSyntax(token);
    }

    return new SyntaxRuleBuilder<SyntaxRule>((rule) => {
      UnarySyntaxRule primitiveRule = (token, power, context) => rule(token, context);

      foreach (var token in tokens) {
        var syntax = this.GetOrAddSyntax(token);
        syntax.UnaryPower = 0;
        syntax.UnaryRule = primitiveRule;
      }
    });
  }

  /// <summary>
  /// Defines a syntax rule for declaration tokens. (e.g. type and variable names)
  /// </summary>
  /// <param name="tokens">The tokens.</param>
  ISyntaxRuleBuilder<SyntaxRule> ISyntaxBuilder.Declaration(params string[] tokens) {
    foreach (var token in tokens) {
      this.GetOrAddSyntax(token);
    }

    return new SyntaxRuleBuilder<SyntaxRule>((rule) => {
      foreach (var token in tokens) {
        var syntax = this.GetOrAddSyntax(token);
        syntax.DeclarationRule = rule;
      }
    });
  }

  /// <summary>
  /// Defines a syntax rule for statement tokens. (e.g. control structures)
  /// </summary>
  /// <param name="tokens">The tokens.</param>
  ISyntaxRuleBuilder<SyntaxRule> ISyntaxBuilder.Statement(params string[] tokens) {
    foreach (var token in tokens) {
      this.GetOrAddSyntax(token);
    }

    return new SyntaxRuleBuilder<SyntaxRule>((rule) => {
      foreach (var token in tokens) {
        var syntax = this.GetOrAddSyntax(token);
        syntax.StatementRule = rule;
      }
    });
  }

  /// <summary>
  /// Defines a syntax rule for expression statements.
  /// </summary>
  /// <param name="rule">The syntax rule.</param>
  void ISyntaxBuilder.ExpressionStatement(SyntaxRule rule) {
    var syntax = this.GetOrAddSyntax(ExpressionStatement);
    syntax.StatementRule = rule;
  }

  /// <summary>
  /// Gets syntax rules for a token.
  /// </summary>
  /// <param name="token">The token.</param>
  private SyntaxRules GetOrAddSyntax(string token) {
    if (!this.syntaxRules.TryGetValue(token, out var syntax)) {
      this.syntaxRules.Add(token, syntax = new SyntaxRules());
    }

    return syntax;
  }

  /// <summary>
  /// Gets syntax rules for a token.
  /// </summary>
  /// <param name="token">The token.</param>
  private SyntaxRules GetSyntax(Token token) {
    SyntaxRules syntax;

    if (this.syntaxRules.TryGetValue(token.Text, out syntax)) {
      return syntax;
    }

    if (this.syntaxRules.TryGetValue(token.Type, out syntax)) {
      return syntax;
    }

    throw token.Error();
  }

  /// <summary>
  /// Syntax rule for a token.
  /// </summary>
  /// <param name="token">The token.</param>
  /// <param name="context">The optional parse context.</param>
  /// <returns>The parsed syntax tree.</returns>
  public delegate object SyntaxRule(Token token, object context);

  /// <summary>
  /// Syntax rule for a token.
  /// </summary>
  /// <param name="token">The token.</param>
  /// <param name="power">The binding power.</param>
  /// <param name="context">The optional parse context.</param>
  /// <returns>The parsed syntax tree.</returns>
  public delegate object UnarySyntaxRule(Token token, int power, object context);

  /// <summary>
  /// Syntax rule for a token.
  /// </summary>
  /// <param name="token">The token.</param>
  /// <param name="power">The binding power.</param>
  /// <param name="left">The parsed left syntax tree.</param>
  /// <param name="context">The optional parse context.</param>
  /// <returns>The parsed syntax tree.</returns>
  public delegate object BinarySyntaxRule(Token token, int power, object left, object context);

  /// <summary>
  /// Syntax builder.
  /// </summary>
  public interface ISyntaxBuilder {
    /// <summary>
    /// Defines a syntax rule for unary tokens. (e.g. aritmetic prefix operators)
    /// </summary>
    /// <param name="power">The binding power.</param>
    /// <param name="tokens">The tokens.</param>
    ISyntaxRuleBuilder<UnarySyntaxRule> Unary(int power, params string[] tokens);

    /// <summary>
    /// Defines a syntax rule for left associative binary tokens. (e.g. aritmetic infix operators)
    /// </summary>
    /// <param name="power">The binding power.</param>
    /// <param name="tokens">The tokens.</param>
    ISyntaxRuleBuilder<BinarySyntaxRule> Left(int power, params string[] tokens);

    /// <summary>
    /// Defines a syntax rule for right associative binary tokens. (e.g. assignment operators)
    /// </summary>
    /// <param name="power">The binding power.</param>
    /// <param name="tokens">The tokens.</param>
    ISyntaxRuleBuilder<BinarySyntaxRule> Right(int power, params string[] tokens);

    /// <summary>
    /// Defines a syntax rule for primitive tokens. (e.g. literals, keywords and semantic symbols)
    /// </summary>
    /// <param name="tokens">The tokens.</param>
    ISyntaxRuleBuilder<SyntaxRule> Primitive(params string[] tokens);

    /// <summary>
    /// Defines a syntax rule for declaration tokens. (e.g. type and variable name declarations)
    /// </summary>
    /// <param name="tokens">The tokens.</param>
    ISyntaxRuleBuilder<SyntaxRule> Declaration(params string[] tokens);

    /// <summary>
    /// Defines a syntax rule for statement tokens. (e.g. control structures)
    /// </summary>
    /// <param name="tokens">The tokens.</param>
    ISyntaxRuleBuilder<SyntaxRule> Statement(params string[] tokens);

    /// <summary>
    /// Defines a syntax rule for expression statements.
    /// </summary>
    /// <param name="rule">The syntax rule.</param>
    void ExpressionStatement(SyntaxRule rule);
  }

  /// <summary>
  /// Syntax rule builder.
  /// </summary>
  public interface ISyntaxRuleBuilder<TRule> {
    /// <summary>
    /// Defines the syntax rule.
    /// </summary>
    /// <param name="rule">The syntax rule.</param>
    void Do(TRule rule);
  }

  /// <summary>
  /// Syntax rule builder implementation.
  /// </summary>
  private sealed class SyntaxRuleBuilder<TRule> : ISyntaxRuleBuilder<TRule> {
    /// <summary>
    /// The build action.
    /// </summary>
    private readonly Action<TRule> build;

    /// <summary>
    /// Initializes a new instance of the class.
    /// </summary>
    /// <param name="build">The build action.</param>
    public SyntaxRuleBuilder(Action<TRule> build) => this.build = build;

    /// <summary>
    /// Defines the syntax rule.
    /// </summary>
    /// <param name="rule">The syntax rule.</param>
    public void Do(TRule rule) => this.build(rule);
  }

  /// <summary>
  /// Syntax rules for a token.
  /// </summary>
  private sealed class SyntaxRules {
    /// <summary>
    /// The unary binding power.
    /// </summary>
    public int UnaryPower { get; set; }

    /// <summary>
    /// The left binding power.
    /// </summary>
    public int LeftPower { get; set; }

    /// <summary>
    /// The right binding power.
    /// </summary>
    public int RightPower { get; set; }

    /// <summary>
    /// The unary rule.
    /// </summary>
    public UnarySyntaxRule UnaryRule { get; set; } = (token, power, context) => throw token.Error();

    /// <summary>
    /// The binary rule.
    /// </summary>
    public BinarySyntaxRule BinaryRule { get; set; } = (token, power, left, context) => throw token.Error();

    /// <summary>
    /// The declaration rule.
    /// </summary>
    public SyntaxRule DeclarationRule { get; set; } = (token, context) => throw token.Error();

    /// <summary>
    /// The statement rule.
    /// </summary>
    public SyntaxRule StatementRule { get; set; }
  }
}
