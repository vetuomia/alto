using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

/// <summary>
/// Lexical token.
/// </summary>
sealed class Token {
  /// <summary>
  /// Wildcard token.
  /// </summary>
  public const string Any = "(any)";

  /// <summary>
  /// Word token type for keywords, identifiers and such.
  /// </summary>
  public const string Word = "(word)";

  /// <summary>
  /// Number token type for number literals.
  /// </summary>
  public const string Number = "(number)";

  /// <summary>
  /// String token type for string literals.
  /// </summary>
  public const string String = "(string)";

  /// <summary>
  /// Punctuation token type for operators, separators and such.
  /// </summary>
  public const string Punctuation = "(punctuation)";

  /// <summary>
  /// End token type, indicates the end of the source text.
  /// </summary>
  public const string End = "(end)";

  /// <summary>
  /// The token type: "(word)", "(number)", "(string)", "(punctuation)", "(expression)", "(end)"
  /// </summary>
  public string Type { get; private set; }

  /// <summary>
  /// The token as it appears in the source text.
  /// </summary>
  public string Text { get; private set; }

  /// <summary>
  /// The string token value. Only valid if the token type is "(string)".
  /// </summary>
  public string StringValue { get; private set; }

  /// <summary>
  /// The number token value. Only valid if the token type is "(number)".
  /// </summary>
  public double NumberValue { get; private set; }

  /// <summary>
  /// The line number where the token appears in the source text.
  /// </summary>
  public int Row { get; private set; }

  /// <summary>
  /// The column number where the token appears in the source text.
  /// </summary>
  public int Column { get; private set; }

  /// <summary>
  /// The entire source code for context.
  /// </summary>
  public string[] Source { get; private set; }

  /// <summary>
  /// The source file name.
  /// </summary>
  public string FileName { get; private set; }

  /// <summary>
  /// The token recognition pattern.
  /// </summary>
  private static readonly Regex Tokens = new Regex(string.Join("|",
    // 1: Whitespace
    @"(\s+)",

    // 2: Comment
    @"(\/\/.*)",

    // 3: Word
    @"([_a-zA-Z][_a-zA-Z0-9]*)",

    // 4: Number
    @"(\d+(?:\.\d+)?(?:[eE][+\-]?\d+)?)",

    // 5: String (with \n \r \" \' \\ escapes)
    @"(""(?:[^""\\]|\\[nr""\\])*""|'(?:[^'\\]|\\[nr""\\])*')",

    // 6: Punctuation
    @"(" + string.Join("|",
      @"&&",                    // &&
      @"\|\|",                  // ||
      @"=>",                    // =>
      @"\.\.\.",                // ...
      @"[(){}\[\]?.,:;]",       // (  )  {  }  [  ]  ?  .  ,  :  ;
      @"[+\-*\/%\^&\|<>!=]=?"   // +  -  *  /  %  ^  &  |  <  >  !  =
                                // += -= *= /= %= ^= &= |= <= >= != ==
    ) + @")")
  );

  /// <summary>
  /// Determines whether the token matches.
  /// </summary>
  /// <param name="token">The token.</param>
  public bool Is(string token) => this.Text == token || this.Type == token || Any == token;

  /// <summary>
  /// Determines whether the token matches one of the given tokens.
  /// </summary>
  /// <param name="tokens">The tokens.</param>
  public bool Is(params string[] tokens) => tokens.Any(this.Is);

  /// <summary>
  /// Determines whether the token does not match.
  /// </summary>
  /// <param name="token">The token.</param>
  public bool IsNot(string token) => !this.Is(token);

  /// <summary>
  /// Determines whether the token does not match any of the given tokens.
  /// </summary>
  /// <param name="tokens">The tokens.</param>
  public bool IsNot(params string[] tokens) => !tokens.Any(this.Is);

  /// <summary>
  /// Ensures that the token matches, throws an error otherwise.
  /// </summary>
  /// <param name="token">The token.</param>
  /// <param name="message">The optional error message.</param>
  public Token Require(string token, string message = null) => this.Is(token) ? this : throw this.Error(message);

  /// <summary>
  /// Ensures that the token does not match, throws an error otherwise.
  /// </summary>
  /// <param name="token">The token.</param>
  /// <param name="message">The optional error message.</param>
  public Token Reject(string token, string message = null) => this.IsNot(token) ? this : throw this.Error(message);

  /// <summary>
  /// Returns a parse error for this token.
  /// </summary>
  /// <param name="message">The error message.</param>
  public ParseError Error(string message = null) => new ParseError(message ?? $"Unexpected '{this}'", this.FileName, this.Source, this.Row, this.Column);

  /// <summary>
  /// Returns the string representation of the token.
  /// </summary>
  public override string ToString() => this.Text;

  /// <summary>
  /// Splits the source text into tokens.
  /// </summary>
  /// <param name="sourceText">The source text.</param>
  /// <param name="fileName">The file name.</param>
  public static Queue<Token> Split(string sourceText, string fileName) {
    var source = new Regex(@"\n|\r\n?").Split(sourceText);
    var tokens = new Queue<Token>();

    for (var row = 0; row < source.Length; row++) {
      var line = source[row];

      if (row == 0 && line.StartsWith("#!")) { // <- skip hashbang
        continue;
      }

      foreach (var token in ParseTokens(line, row, 0, line.Length)) {
        tokens.Enqueue(token);
      }
    }

    tokens.Enqueue(new Token {
      Type = End,
      Text = "end of input",
      Row = source.Length - 1,
      Column = source[source.Length - 1].Length,
      Source = source,
      FileName = fileName,
    });

    return tokens;

    Group Match(string line, int start, out int index) {
      var m = Tokens.Match(line, start);

      if (m.Success) {
        for (index = 1; index < m.Groups.Count; index++) {
          var group = m.Groups[index];

          if (group.Success) {
            return group;
          }
        }
      }

      index = 0;
      return null;
    }

    IEnumerable<Token> ParseTokens(string line, int row, int column, int end, bool throwOnComments = false) {
      while (column < end) {
        var match = Match(line, column, out var index);

        if (match == null || column != match.Index) {
          throw new ParseError($"Unexpected '{line.Substring(column)}'", fileName, source, row, column);
        }

        switch (index) {
          case 1: // Whitespace
            break;

          case 2: // Comment
            if (throwOnComments) {
              throw new ParseError($"Unexpected '{line.Substring(column)}'", fileName, source, row, column);
            }
            break;

          case 3: // Word
            yield return new Token {
              Type = Word,
              Text = match.Value,
              Row = row,
              Column = column,
              Source = source,
              FileName = fileName,
            };
            break;

          case 4: // Number
            yield return new Token {
              Type = Number,
              Text = match.Value,
              NumberValue = ParseNumber(match.Value),
              Row = row,
              Column = column,
              Source = source,
              FileName = fileName,
            };
            break;

          case 5: // String
            yield return new Token {
              Type = String,
              Text = match.Value,
              StringValue = ParseString(match.Value),
              Row = row,
              Column = column,
              Source = source,
              FileName = fileName,
            };
            break;

          case 6: // Punctuation
            yield return new Token {
              Type = Punctuation,
              Text = match.Value,
              Row = row,
              Column = column,
              Source = source,
              FileName = fileName,
            };
            break;
        }

        column = match.Index + match.Length;
      }
    }

    string ParseString(string str) => str
      .Substring(1, str.Length - 2)
      .Replace(@"\n", "\n")
      .Replace(@"\r", "\r")
      .Replace(@"\\", "\\")
      .Replace(@"\""", "\"")
      .Replace(@"\'", "'");

    double ParseNumber(string str) => double.Parse(str, NumberStyles.Float, CultureInfo.InvariantCulture);
  }

  /// <summary>
  /// Implicit conversion from token to a boolean.
  /// </summary>
  /// <param name="token">The token.</param>
  public static implicit operator bool(Token token) => token != null;
}
