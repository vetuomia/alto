using System;
using System.Diagnostics;

/// <summary>
/// Behavior driven testing framework.
/// </summary>
static class Spec {
  /// <summary>
  /// Test subject specification.
  /// </summary>
  /// <param name="subject">The test subject.</param>
  /// <param name="behaviors">The specified behaviors.</param>
  public delegate void Describe(string subject, Action<It> behaviors);

  /// <summary>
  /// Tets subject behavior specification.
  /// </summary>
  /// <param name="behavior">The expected behavior.</param>
  /// <param name="tests">The tests to verify the expected behavior.</param>
  public delegate void It(string behavior, Action<Expect> tests = null);

  /// <summary>
  /// Test assertion.
  /// </summary>
  /// <param name="condition">The assert condition.</param>
  public delegate void Expect(bool condition);

  /// <summary>
  /// Runs a test suite and prints the result to the console.
  /// </summary>
  /// <param name="describe">Test subject specifications.</param>
  public static void Run(Action<Describe> describe) {
    var tests = 0;
    var failed = 0;
    var unknown = 0;

    describe((subject, it) => {
      Console.WriteLine($"{subject}");

      it((behavior, expect) => {
        tests++;
        var status = "NOT TESTED";
        var failure = default(string);
        var duration = 0L;

        if (expect != null) {

          try {
            var asserts = 0;
            var timing = Stopwatch.StartNew();

            expect((condition) => {
              asserts++;
              Trace.Assert(condition);
            });

            duration = timing.ElapsedMilliseconds;

            if (asserts > 0) {
              status = "OK";
            } else {
              unknown++;
            }
          } catch (Exception error) {
            failed++;
            status = "FAIL";
            failure = error.ToString();
          }
        } else {
          unknown++;
        }

        if (failure == null) {
          if (duration > 10) {
            Console.WriteLine($"  {behavior}: {status} ({duration}ms)");
          } else {
            Console.WriteLine($"  {behavior}: {status}");
          }
        } else {
          Console.WriteLine($"  {behavior}: {status}");
          Console.WriteLine($"    {failure}");
        }
      });

      Console.WriteLine(" ");
    });

    Console.Write($"Tests: {tests}");

    if (failed > 0) {
      Console.Write($", Failed: {failed}");
    }

    if (unknown > 0) {
      Console.Write($", Unknown: {unknown}");
    }

    Console.WriteLine();
  }
}
