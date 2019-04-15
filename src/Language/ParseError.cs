using System;
using System.Text;

/// <summary>
/// Parse error.
/// </summary>
sealed class ParseError : Exception {
  public string FileName { get; }

  /// <summary>
  /// The source text, split into rows.
  /// </summary>
  public string[] SourceText { get; }

  /// <summary>
  /// The row number.
  /// </summary>
  public int Row { get; }

  /// <summary>
  /// The column number.
  /// </summary>
  public int Column { get; }

  /// <summary>
  /// Initializes a new instance of the class.
  /// </summary>
  /// <param name="message">The error message.</param>
  /// <param name="fileName">The file name.</param>
  /// <param name="sourceText">The source text, split into rows.</param>
  /// <param name="row">The row number.</param>
  /// <param name="column">The column number.</param>
  public ParseError(string message, string fileName, string[] sourceText, int row, int column = -1) : base(message) {
    this.FileName = fileName;
    this.SourceText = sourceText;
    this.Row = row;
    this.Column = column;
  }

  /// <summary>
  /// Returns a string representation of the error.
  /// </summary>
  public override string ToString() {
    var sb = new StringBuilder();

    void FormatRow(int row) {
      sb.AppendFormat("{0,4}: ", row + 1)
        .AppendLine(this.SourceText[row]);
    }

    sb.Append("ERROR ")
      .Append(this.FileName)
      .Append("(")
      .Append(this.Row + 1);

    if (this.Column >= 0) {
      sb.Append(",")
      .Append(this.Column + 1);
    }

    sb.Append("): ")
      .Append(this.Message);

    if (this.Source != null) {
      sb.AppendLine();

      if (this.Row >= 2) {
        FormatRow(this.Row - 2);
      }

      if (this.Row >= 1) {
        FormatRow(this.Row - 1);
      }

      FormatRow(this.Row);

      if (this.Column >= 0) {
        sb.Append(' ', this.Column + 6)
          .Append("^-- ")
          .Append(this.Message);
      } else {
        sb.Append(' ', 6)
          .Append('^', this.SourceText[this.Row].Length);
      }
    }

    return sb.ToString();
  }
}
