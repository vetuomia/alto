using System;
using System.Collections.Generic;

/// <summary>
/// Contains the unit tests.
/// </summary>
static class UnitTests {
  /// <summary>
  /// Runs the unit tests.
  /// </summary>
  // [Conditional("DEBUG")]
  public static void Run() => Spec.Run(describe => {
    describe(nameof(Value), it => {
      it("is null by default", expect => {
        Value value = default;
        expect(value.IsNull);
      });

      it("can hold a boolean", expect => {
        Value value = true;
        expect(value.IsBoolean(out var v) && v == true);
      });

      it("can hold a number", expect => {
        Value value = 3.14;
        expect(value.IsNumber(out var v) && v == 3.14);
      });

      it("can hold a string", expect => {
        Value value = "hello";
        expect(value.IsString(out var v) && v == "hello");
      });

      it("can hold a list", expect => {
        Value value = new List() { 1, 2 };
        expect(value.IsList(out var v) && v.Count == 2);
      });

      it("can hold a table", expect => {
        Value value = new Table() { ["hello"] = "world" };
        expect(value.IsTable(out var v) && v["hello"].Equals("world"));
      });

      it("can compare values", expect => {
        expect(new Value() == default);
        expect(new Value(true) == true);
        expect(new Value(false) == false);
        expect(new Value(0) == 0);
        expect(new Value(3.14) == 3.14);
        expect(new Value(double.NaN) != double.NaN);
        expect(new Value("hello") == "hello");
      });

      it("can produce hash codes", expect => {
        Value value = "hello";
        expect(value.GetHashCode() == value.GetHashCode());
      });

      it("can be used as a key in a dictionary", expect => {
        var dict = new Dictionary<Value, Value>();
        dict.Add(default, "null");
        dict.Add(true, "a boolean");
        dict.Add(3.14, "a number");
        dict.Add(double.NaN, "not a number");
        dict.Add("hello", "a string");

        expect(dict[default] == "null");
        expect(dict[true] == "a boolean");
        expect(dict[3.14] == "a number");
        expect(dict[double.NaN] == "not a number");
        expect(dict["hello"] == "a string");
      });
    });

    describe(nameof(Opcode), it => {
      it("has size of 1 byte", expect => {
        expect(sizeof(Opcode) == 1);
      });

      it("fits in the instruction", expect => {
        var count = 0;

        foreach (Opcode opcode in Enum.GetValues(typeof(Opcode))) {
          var value = (int)opcode;
          expect(value >= Instruction.MinOpcode);
          expect(value <= Instruction.MaxOpcode);
          count++;
        }

        expect(count > 0);
      });
    });

    describe(nameof(Instruction), it => {
      it("has an opcode", expect => {
        var ins = new Instruction(Opcode.Null, 0, 0);

        expect(ins.Opcode == Opcode.Null);
        expect(ins.Param == 0);
        expect(ins.Value == 0);
      });

      it("cannot have an opcode beyond maximum value", expect => {
        var max = (Opcode)127;
        var ins = new Instruction(max + 1, 0, 0);

        expect(ins.Opcode <= max);
        expect(ins.Param == 0);
        expect(ins.Value == 0);
      });

      it("can have a param", expect => {
        var ins = new Instruction(Opcode.Null, 1, 0);

        expect(ins.Opcode == Opcode.Null);
        expect(ins.Param == 1);
        expect(ins.Value == 0);
      });

      it("can have a large param", expect => {
        var ins = new Instruction(Opcode.Null, Instruction.MaxParam, 0);

        expect(ins.Opcode == Opcode.Null);
        expect(ins.Param == Instruction.MaxParam);
        expect(ins.Value == 0);
      });

      it("cannot have negative param", expect => {
        var ins = new Instruction(Opcode.Null, -3, 0);

        expect(ins.Opcode == Opcode.Null);
        expect(ins.Param >= 0);
        expect(ins.Value == 0);
      });

      it("cannot have a param beyond the maximum", expect => {
        var ins = new Instruction(Opcode.Null, Instruction.MaxParam + 1, 0);

        expect(ins.Opcode == Opcode.Null);
        expect(ins.Param <= Instruction.MaxParam);
        expect(ins.Value == 0);
      });

      it("can have a value", expect => {
        var ins = new Instruction(Opcode.Null, 0, 12345);

        expect(ins.Opcode == Opcode.Null);
        expect(ins.Param == 0);
        expect(ins.Value == 12345);
      });

      it("can have a large value", expect => {
        var ins = new Instruction(Opcode.Null, 0, Instruction.MaxValue);

        expect(ins.Opcode == Opcode.Null);
        expect(ins.Param == 0);
        expect(ins.Value == Instruction.MaxValue);
      });

      it("cannot have a positive value beyond the maximum", expect => {
        var ins = new Instruction(Opcode.Null, 0, Instruction.MaxValue + 1);

        expect(ins.Opcode == Opcode.Null);
        expect(ins.Param == 0);
        expect(ins.Value < Instruction.MaxValue);
      });

      it("can have a negative value", expect => {
        var ins = new Instruction(Opcode.Null, 0, -5);

        expect(ins.Opcode == Opcode.Null);
        expect(ins.Param == 0);
        expect(ins.Value == -5);
      });

      it("can have a large negative value", expect => {
        var ins = new Instruction(Opcode.Null, 0, Instruction.MinValue);

        expect(ins.Opcode == Opcode.Null);
        expect(ins.Param == 0);
        expect(ins.Value == Instruction.MinValue);
      });

      it("cannot have a negative value beyond the minimum", expect => {
        var ins = new Instruction(Opcode.Null, 0, Instruction.MinValue - 1);

        expect(ins.Opcode == Opcode.Null);
        expect(ins.Param == 0);
        expect(ins.Value > Instruction.MinValue);
      });

      it("cannot have param and value beyond maximum values", expect => {
        var ins = new Instruction(Opcode.Null, Instruction.MaxParam + 1, Instruction.MaxValue + 1);

        expect(ins.Opcode == Opcode.Null);
        expect(ins.Param <= Instruction.MaxParam);
        expect(ins.Value <= Instruction.MaxValue);
      });
    });

    describe(nameof(Interpreter), it => {
      it("can process literals, data and arguments", expect => {
        var mod = Assembler.Assemble(@"
          global    greeting     'Hello World!'
          global    PI           3.14

          null
          ldglob    greeting
          ldglob    PI
          true
          number    15
          this
          ldarg     0
          ldargs    1
          list      8
          return
        ");

        var result = mod.Main("a", "b", "c");
        expect(result.IsList(out var list));
        expect(list.Count == 8);
        expect(list[0] == default);
        expect(list[1] == "Hello World!");
        expect(list[2] == 3.14);
        expect(list[3] == true);
        expect(list[4] == 15);
        expect(list[5] == mod.Exports);
        expect(list[6] == "a");
        expect(list[7].IsList(out var arr) && arr.Count == 2 && arr[0] == "b" && arr[1] == "c");
      });

      it("can swap, copy and drop values", expect => {
        var mod = Assembler.Assemble(@"
          false
          true
          swap

          number    10
          copy
          copy      4 ; <- bottom of the stack
          drop      3 ; <- drop all except the bottom value

          this
          list      6
          return
        ");

        var result = mod.Main();
        expect(result.IsList(out var list));
        expect(list.Count == 6);
        expect(list[0] == true);
        expect(list[1] == false);
        expect(list[2] == 10);
        expect(list[3] == 10);
        expect(list[4] == true);
        expect(list[5] == mod.Exports);
      });

      it("can define local variables and perform basic math", expect => {
        var mod = Assembler.Assemble(@"
          const     a   0
          const     b   1
          const     c   2

          number    2   ; a = 2
          copy          ; b = a + 1
          number    1
          add
          number    0   ; c = 0

          ldvar     b   ; tmp1 = (b + 5)
          number    5
          add
          ldvar     a   ; tmp2 = (a * b)
          ldvar     b
          mul
          div           ; tmp3 = tmp1 / tmp2
          number    1   ; tmp4 = tmp3 - 1
          sub

          stvar     c   ; c = tmp4
          drop

          list      3   ; return [a, b, c]
          return
        ");

        var result = mod.Main();
        expect(result.IsList(out var list));
        expect(list.Count == 3);
        expect(list[0] == 2);
        expect(list[1] == 3);
        expect(list[2].IsNumber(out var c) && c > 0.333 && c < 0.334);
      });

      it("can perform conditional logic and jumps", expect => {
        var mod = Assembler.Assemble(@"
          global    less     'Less'
          global    greater  'Greater'

          ldarg     0       ; if(arg0 < 10) {
          number    10
          less
          jump!     else
          ldglob    less    ;   return 'Less';
          return
        else:               ; } else {
          ldglob    greater ;   return 'Greater';
          return            ; }
        ");

        expect(mod.Main(5) == "Less");
        expect(mod.Main(15) == "Greater");
      });

      it("can define nested functions and capture variables", expect => {
        var mod = Assembler.Assemble(@"
          const     a         0
          const     b         1

          const     outer     1  ; closure scope
          const     c         0

          number    0         ; c = 0
          closure   1         ; move c into closure

          function  add       ; a = add(3, 5)
          null                ; this = null
          number    3         ; arg0 = 3
          number    5         ; arg1 = 5
          call      2

          number    15        ; c = 15
          stvar     outer c
          drop

          lambda    add_c     ; b = add_c(2)
          null                ; receiver
          number    2         ; arg0 = 2
          call      1         ;

          list      2         ; return [a, b];
          return

        add:                  ; function add(arg0, arg1) {
          ldarg     0         ;   return arg0 + arg1;
          ldarg     1
          add
          return              ; }

        add_c:                ; function add_c(arg0) {
          ldarg     0         ;   return arg0 + c;
          ldvar     outer c
          add
          return              ; }
        ");

        var result = mod.Main();
        expect(result.IsList(out var list));
        expect(list.Count == 2);
        expect(list[0] == 8);
        expect(list[1] == 17);
      });

      it("construct tables and access their members", expect => {
        var mod = Assembler.Assemble(@"
          const     A   0
          global    B   'B'
          global    C   'C'

          ldglob    B   ; A = { B: fun, C: 1 }
          function  fun
          ldglob    C
          number    1
          table     2

          ldvar     A   ; A.B(1, 2)
          copy
          ldglob    B
          ldelem
          swap
          number    1
          number    2
          call      2

          ldvar     A   ; A[B](2, 3)
          copy
          ldglob    B
          ldelem
          swap
          number    2
          number    3
          call      2

          ldvar     A   ; A[C] += 1
          ldglob    C
          copy      2
          ldelem
          number    1
          add
          stelem
          drop

          ldvar     A   ; A.C
          ldglob    C
          ldelem

          list      3
          return

        fun:            ; function(arg0, argu1) {
          ldarg   0     ;   return arg0 + arg1;
          ldarg   1
          add
          return        ; }
        ");

        var result = mod.Main();
        expect(result.IsList(out var list));
        expect(list.Count == 3);
        expect(list[0] == 3);
        expect(list[1] == 5);
        expect(list[2] == 2);
      });

      it("can import globals", expect => {
        var mod = Assembler.Assemble(@"
          import    Greeting  'Greeting'

          ldglob    Greeting  ; <- the value has been replaced
          return
        ");

        mod.Importing += (source, import) => {
          if (import.Name == "Greeting") {
            import.Value = "Hello, world!";
          }
        };

        expect(mod.Main() == "Hello, world!");
      });

      it("can throw and catch exceptions", expect => {
        var mod = Assembler.Assemble(@"
          global    error   'error'

          try       exit
          number    10
          drop
          endtry    next
        exit:
          return

        next:
          try       handler
          ldglob    error
          throw
          endtry    end
        handler:
          return

        end:
          false
          return
        ");

        var result = mod.Main();
        expect(result.IsException(out var exception) && exception.Message == "error");
      });

      it("can read properties", expect => {
        var mod = Assembler.Assemble(@"
          global    Greeting  'Greeting'
          global    length    'length'

          ldglob    Greeting
          ldglob    length
          ldelem
          return
        ");

        expect(mod.Main() == 8);
      });
    });

    describe(nameof(Compiler), it => {
      it("can parse and compile simple scripts", expect => {
        var mod = Compiler.Compile(@"
          const C = 3
          const add = (a, b) => a + b + C

          this.add = add
          this.name = 'Hello'
        ");

        var result = mod.Main();
        expect(result.IsNull);
        expect(mod.Exports["add"].IsFunction(out var fn) && fn(default, 1, 2) == 6);
        expect(mod.Exports["name"] == "Hello");
      });

      it("handles closures properly", expect => {
        var mod = Compiler.Compile(@"
          const C = 10

          export const new = (inc) => {
            var A = 3;

            return {
              get: () => A,
              set: (a) => A = a,
              add: () => A += inc,
              adder: (i) => () => {
                A += i
                return C
              }
            }
          }
        ");

        var result = mod.Main();
        expect(result.IsNull);
        expect(mod.Exports["new"].IsFunction(out var fn));
        expect(fn(default, 5).IsTable(out var obj));
        expect(obj["get"].IsFunction(out var get) && get(default) == 3);
        expect(obj["set"].IsFunction(out var set) && set(default, 2) == 2 && get(default) == 2);
        expect(obj["add"].IsFunction(out var add) && add(default) == 7 && get(default) == 7);
        expect(obj["adder"].IsFunction(out var adder) && adder(default, 1).IsFunction(out var next) && next(default) == 10 && get(default) == 8);
      });

      it("handles closures in loops properly", expect => {
        var mod = Compiler.Compile(@"
          var f = [null, null];
          var i = 0;

          while (i < 2) {
            var n = 1

            for (var j = 0; j < 1; j += 1) {
              f[i] = (c) => n += c
            }

            i += 1
          }

          this.a = f[0](2); // 1 + 2 = 3
          this.b = f[1](4); // 1 + 4 = 5
        ");

        expect(mod.Main().IsNull);
        expect(mod.Exports["a"] == 3);
        expect(mod.Exports["b"] == 5);
      });

      it("can break and continue in loops", expect => {
        var mod = Compiler.Compile(@"
          const assert = function(condition) {
            if (!condition) {
              throw 'condition was false'
            }
          }

          {
            for (var i = 0; i < 10; i += 1) {
              var n = 1

              if (i == 3) {
                continue
              }

              var fn = () => n += 1
              fn()

              assert(n == 2)

              if (i >= 5) {
                break
              }
            }
          }

          {
            var i = 0;
            while (i < 10) {
              var n = 1

              if (i == 3) {
                i = 4
                continue
              }

              var fn = () => n += 1
              fn()

              assert(n == 2)

              if (i >= 5) {
                break
              }

              i += 1
            }

            assert(i == 5)
          }
        ");

        expect(mod.Main().IsNull);
      });


      it("survives Knuth's man or boy test", expect => {
        var mod = Compiler.Compile(@"
          const a = (k, x1, x2, x3, x4, x5) => {
            var k_ = k

            const b = () => {
              k_ -= 1
              return a(k_, b, x1, x2, x3, x4)
            }

            return (k_ > 0) ? b() : x4() + x5()
          }

          const x = (n) => () => n

          this.test = (n) => a(n, x(1), x(-1), x(-1), x(1), x(0))
        ");

        expect(mod.Main().IsNull);
        expect(mod.Exports["test"].IsFunction(out var test));
        expect(test(default, 7) == -1);
        expect(test(default, 8) == -10);
        expect(test(default, 9) == -30);
        expect(test(default, 10) == -67);
        expect(test(default, 11) == -138);
        // values greater than 11 cause a StackOverflowException
      });
    });
  });
}
