using System.Collections.Generic;
using System.Diagnostics;

static partial class Compiler {
  /// <summary>
  /// Marks the token location.
  /// </summary>
  /// <param name="emitter">The emitter.</param>
  /// <param name="token">The token.</param>
  /// <returns>Reference to the emitter for method call chaining.</returns>
  private static Emitter At(this Emitter emitter, Token token) => emitter.MarkLocation(token?.Row, token?.Column);

  /// <summary>
  /// Emits a language element.
  /// </summary>
  /// <param name="emitter">The emitter.</param>
  /// <param name="element">The language element.</param>
  /// <param name="exits">The non-exceptiona exit targets.</param>
  /// <returns>Reference to the emitter for method call chaining.</returns>
  private static Emitter Emit(this Emitter emitter, LanguageElement element, Exits exits) {
    emitter.At(element.Token);

    if (exits == null) {
      var section1 = emitter.Section(); // <- function body
      var section2 = emitter.Section().At(null); // <- default result
      var section3 = emitter.Section().At(null); // <- final cleanup
      var section4 = emitter.Section().At(null); // <- return

      element.Emit(section1, new Exits(section3));
      section2.Emit(Opcode.Null);
      section4.Emit(Opcode.Return);

      return section1;
    } else {
      element.Emit(emitter, exits);
      return emitter;
    }
  }

  /// <summary>
  /// Bytecode emitter.
  /// </summary>
  sealed class Emitter {
    /// <summary>
    /// The root data section.
    /// </summary>
    private readonly DataSection data;

    /// <summary>
    /// The root code section.
    /// </summary>
    private readonly CodeSection code;

    /// <summary>
    /// The local code section.
    /// </summary>
    private readonly CodeSection local;

    /// <summary>
    /// The current source map.
    /// </summary>
    private SourceMap sourceMap;

    /// <summary>
    /// Initializes a new instance of the class.
    /// </summary>
    /// <param name="source">The source code, split into lines.</param>
    public Emitter(string[] source) {
      this.data = new DataSection();
      this.code = new CodeSection();
      this.local = new CodeSection();
      this.sourceMap = new SourceMap(source);
      this.code.Add(this.local);
    }

    /// <summary>
    /// Initializes a new instance of the class.
    /// </summary>
    /// <param name="parent">The parent emitter.</param>
    /// <param name="outer">The outer code section.</param>
    /// <param name="sourceMap">The current source map.</param>
    private Emitter(Emitter parent, CodeSection outer, SourceMap sourceMap) {
      this.data = parent.data;
      this.code = parent.code;
      this.local = new CodeSection();
      this.sourceMap = sourceMap;
      outer.Add(this.local);
    }

    /// <summary>
    /// Adds a value in the data section.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The index of the value in the data section.</returns>
    public int Add(Value value) => this.data.Add(value);

    /// <summary>
    /// Gets or adds a value in the data section.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The index of the value in the data section.</returns>
    public int GetOrAdd(Value value) => this.data.GetOrAdd(value);

    /// <summary>
    /// Sets a value in the data section.
    /// </summary>
    /// <param name="index">The index of the value in the data section.</param>
    /// <param name="value">The value.</param>
    public void Set(int index, Value value) => this.data.Set(index, value);

    /// <summary>
    /// Creates a local code section.
    /// </summary>
    /// <returns>The local code emitter.</returns>
    public Emitter Section() => new Emitter(this, this.local, this.sourceMap);

    /// <summary>
    /// Creates a function code section.
    /// </summary>
    /// <param name="name">The function name.</param>
    /// <returns>The function code emitter.</returns>
    public Emitter Function(string name) => new Emitter(this, this.code, new SourceMap(this.sourceMap) {
      Function = name,
      Parameters = null,
      Variables = null,
    });

    /// <summary>
    /// Marks the location of the following instructions.
    /// </summary>
    /// <param name="row">The row in the source code.</param>
    /// <param name="column">The column in the source code.</param>
    /// <returns>The reference to self for method call chaining.</returns>
    public Emitter MarkLocation(int? row, int? column) {
      if (this.sourceMap.Row != row || this.sourceMap.Column != column) {
        this.sourceMap = new SourceMap(this.sourceMap) { Row = row, Column = column };
      }

      return this;
    }

    /// <summary>
    /// Marks the parameters used in the following instructions.
    /// </summary>
    /// <param name="parameters">The parameters.</param>
    /// <returns>The reference to self for method call chaining.</returns>
    public Emitter MarkParameters(SourceMap.Parameter[] parameters) {
      this.sourceMap = new SourceMap(this.sourceMap) { Parameters = parameters };
      return this;
    }

    /// <summary>
    /// Marks the variables used in the following instructions.
    /// </summary>
    /// <param name="variables">The variables.</param>
    /// <returns>The reference to self for method call chaining.</returns>
    public Emitter MarkVariables(SourceMap.Variable[] variables) {
      this.sourceMap = new SourceMap(this.sourceMap) { Variables = variables };
      return this;
    }

    /// <summary>
    /// Marks the globals used in the following instructions.
    /// </summary>
    /// <param name="globals">The globals.</param>
    /// <returns>The reference to self for method call chaining.</returns>
    public Emitter MarkGlobals(SourceMap.Global[] globals) {
      this.sourceMap = new SourceMap(this.sourceMap) { Globals = globals };
      return this;
    }

    /// <summary>
    /// Emits an instruction.
    /// </summary>
    /// <param name="opcode">The opcode.</param>
    /// <returns>The reference to self for method call chaining.</returns>
    public Emitter Emit(Opcode opcode) {
      this.local.Add(new CodeEmitter(opcode, 0, 0, null, this.sourceMap));
      return this;
    }

    /// <summary>
    /// Emits an instruction.
    /// </summary>
    /// <param name="opcode">The opcode.</param>
    /// <param name="value">The value.</param>
    /// <returns>The reference to self for method call chaining.</returns>
    public Emitter Emit(Opcode opcode, int value) {
      Debug.Assert(Instruction.InValueRange(value));
      this.local.Add(new CodeEmitter(opcode, 0, value, null, this.sourceMap));
      return this;
    }

    /// <summary>
    /// Emits an instruction.
    /// </summary>
    /// <param name="opcode">The opcode.</param>
    /// <param name="target">The target block emitter.</param>
    /// <returns>The reference to self for method call chaining.</returns>
    public Emitter Emit(Opcode opcode, Emitter target) {
      Debug.Assert(target != null);
      this.local.Add(new CodeEmitter(opcode, 0, 0, target.local, this.sourceMap));
      return this;
    }

    /// <summary>
    /// Emits an instruction.
    /// </summary>
    /// <param name="opcode">The opcode.</param>
    /// <param name="param">The param.</param>
    /// <param name="value">The value.</param>
    /// <param name="target">The target block emitter.</param>
    /// <returns>The reference to self for method call chaining.</returns>
    public Emitter Emit(Opcode opcode, int param, int value) {
      Debug.Assert(Instruction.InParamRange(param));
      Debug.Assert(Instruction.InValueRange(value));
      this.local.Add(new CodeEmitter(opcode, param, value, null, this.sourceMap));
      return this;
    }

    /// <summary>
    /// Emits an instruction.
    /// </summary>
    /// <param name="opcode">The opcode.</param>
    /// <param name="param">The param.</param>
    /// <param name="target">The target block emitter.</param>
    /// <returns>The reference to self for method call chaining.</returns>
    public Emitter Emit(Opcode opcode, int param, Emitter target) {
      Debug.Assert(Instruction.InParamRange(param));
      Debug.Assert(target != null);
      this.local.Add(new CodeEmitter(opcode, param, 0, target.local, this.sourceMap));
      return this;
    }

    /// <summary>
    /// Assembles the code and data into a module.
    /// </summary>
    /// <returns>The assembled module.</returns>
    public Module Assemble() => new Module(this.code.Assemble(), this.data.Assemble(), this.code.EmitSourceMaps().ToArray());

    /// <summary>
    /// An abstract base class for code elements.
    /// </summary>
    private abstract class CodeElement {
      /// <summary>
      /// The element address in the code.
      /// </summary>
      public int Address { get; set; }

      /// <summary>
      /// Emits the instructions.
      /// </summary>
      /// <returns>The instructions.</returns>
      public abstract IEnumerable<Instruction> Emit();

      /// <summary>
      /// Emits the source maps.
      /// </summary>
      /// <returns>The source maps.</returns>
      public abstract IEnumerable<SourceMap> EmitSourceMaps();
    }

    /// <summary>
    /// Code section that holds a tree of the nested sections and instructions.
    /// </summary>
    private sealed class CodeSection : CodeElement {
      /// <summary>
      /// The code elements.
      /// </summary>
      private readonly List<CodeElement> elements = new List<CodeElement>();

      /// <summary>
      /// Adds a code element to the section.
      /// </summary>
      /// <param name="element">The code element.</param>
      public void Add(CodeElement element) {
        this.elements.Add(element);
      }

      /// <summary>
      /// Assembles the code section.
      /// </summary>
      /// <returns>The code section instructions.</returns>
      public Instruction[] Assemble() {
        var address = 0;

        CodeElement Resolve(CodeElement element) {
          element.Address = address;

          if (element is CodeSection section) {
            foreach (var child in section.elements) {
              Resolve(child);
            }
          } else {
            address++;
          }

          return element;
        }

        return Resolve(this).Emit().ToArray();
      }

      /// <summary>
      /// Emits the instructions.
      /// </summary>
      /// <returns>The instructions.</returns>
      public override IEnumerable<Instruction> Emit() {
        foreach (var item in this.elements) {
          foreach (var instruction in item.Emit()) {
            yield return instruction;
          }
        }
      }

      /// <summary>
      /// Emits the source maps.
      /// </summary>
      /// <returns>The source maps.</returns>
      public override IEnumerable<SourceMap> EmitSourceMaps() {
        foreach (var item in this.elements) {
          foreach (var sourceMap in item.EmitSourceMaps()) {
            yield return sourceMap;
          }
        }
      }
    }

    /// <summary>
    /// Code emitter that produces the final instructions.
    /// </summary>
    private sealed class CodeEmitter : CodeElement {
      /// <summary>
      /// The opcode.
      /// </summary>
      private readonly Opcode opcode;

      /// <summary>
      /// The param.
      /// </summary>
      private readonly int param;

      /// <summary>
      /// The value.
      /// </summary>
      private readonly int value;

      /// <summary>
      /// The target element, if any.
      /// </summary>
      private readonly CodeElement target;

      /// <summary>
      /// The source map.
      /// </summary>
      private readonly SourceMap sourceMap;

      /// <summary>
      /// Initializes a new instance of the class.
      /// </summary>
      /// <param name="opcode">The opcode.</param>
      /// <param name="param">The param.</param>
      /// <param name="value">The value.</param>
      /// <param name="target">The target element, if any.</param>
      /// <param name="sourceMap">The source map.</param>
      public CodeEmitter(Opcode opcode, int param, int value, CodeElement target, SourceMap sourceMap) {
        this.opcode = opcode;
        this.param = param;
        this.value = value;
        this.target = target;
        this.sourceMap = sourceMap;
      }

      /// <summary>
      /// Emits the instructions.
      /// </summary>
      /// <returns>The instructions.</returns>
      public override IEnumerable<Instruction> Emit() {
        yield return new Instruction(this.opcode, this.param, this.target?.Address ?? this.value);
      }

      /// <summary>
      /// Emits the source maps.
      /// </summary>
      /// <returns>The source maps.</returns>
      public override IEnumerable<SourceMap> EmitSourceMaps() {
        yield return this.sourceMap;
      }
    }

    /// <summary>
    /// Data section that holds a list of data elements.
    /// </summary>
    private sealed class DataSection {
      /// <summary>
      /// The lookup table for values.
      /// </summary>
      private readonly Dictionary<Value, int> lookup = new Dictionary<Value, int>();

      /// <summary>
      /// The values.
      /// </summary>
      private readonly List<Value> values = new List<Value>();

      /// <summary>
      /// Adds a value to the data section.
      /// </summary>
      /// <param name="value">The value.</param>
      /// <returns>The index of the value.</returns>
      public int Add(Value value) {
        var index = this.values.Count;
        this.values.Add(value);
        return index;
      }

      /// <summary>
      /// Gets or adds a value to the data section.
      /// </summary>
      /// <param name="value">The value.</param>
      /// <returns>The index of the value.</returns>
      public int GetOrAdd(Value value) {
        if (!this.lookup.TryGetValue(value, out var index)) {
          this.lookup.Add(value, index = this.Add(value));
        }

        return index;
      }

      /// <summary>
      /// Sets a value in the data section.
      /// </summary>
      /// <param name="index">The index of the value in the data section.</param>
      /// <param name="value">The value.</param>
      public void Set(int index, Value value) => this.values[index] = value;

      /// <summary>
      /// Assembles the data section.
      /// </summary>
      /// <returns>The data section values.</returns>
      public Value[] Assemble() => this.values.ToArray();
    }
  }
}
