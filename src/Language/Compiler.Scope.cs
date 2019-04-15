using System;
using System.Collections.Generic;
using System.Diagnostics;

static partial class Compiler {
  /// <summary>
  /// An abstract base class for lexical scopes.
  /// </summary>
  private abstract class LexicalScope : LanguageElement {
    /// <summary>
    /// The inner scopes.
    /// </summary>
    public List<LexicalScope> Inner { get; } = new List<LexicalScope>();

    /// <summary>
    /// The declared slots.
    /// </summary>
    public List<Slot> Slots { get; } = new List<Slot>();

    /// <summary>
    /// The closure slots in this scope.
    /// </summary>
    public List<Slot> Closure { get; } = new List<Slot>();

    /// <summary>
    /// All slots in the entire tree.
    /// </summary>
    public IEnumerable<Slot> SlotsInTree {
      get {
        foreach (var slot in this.Slots) {
          yield return slot;
        }

        foreach (var inner in this.Inner) {
          foreach (var slot in inner.SlotsInTree) {
            yield return slot;
          }
        }
      }
    }

    /// <summary>
    /// All slots in the local tree.
    /// </summary>
    public IEnumerable<Slot> SlotsInLocalTree {
      get {
        foreach (var slot in this.Slots) {
          yield return slot;
        }

        foreach (var scope in this.Inner) {
          if (scope is BlockScope || scope is LoopScope) {
            foreach (var slot in scope.SlotsInLocalTree) {
              yield return slot;
            }
          }
        }
      }
    }

    /// <summary>
    /// All slots visible in this scope.
    /// </summary>
    public IEnumerable<Slot> SlotsInScope {
      get {
        for (var scope = this; scope != null; scope = scope.Scope) {
          foreach (var slot in scope.Slots) {
            yield return slot;
          }
        }
      }
    }

    /// <summary>
    /// The stack allocation required by the scope.
    /// </summary>
    public int StackAllocation { get; private set; }

    /// <summary>
    /// Indicates whether this scope contains references through the closure chain.
    /// </summary>
    public bool ContainsClosureReferences { get; private set; }

    /// <summary>
    /// Initializes a new instance of the class.
    /// </summary>
    /// <param name="outer">The outer scope, if any.</param>
    public LexicalScope(LexicalScope outer) {
      this.Scope = outer;
      this.Scope?.Inner.Add(this);
    }

    /// <summary>
    /// Validates the language element semantics.
    /// </summary>
    public override void Validate() {
      Debug.Assert(this is FunctionScope && this.Scope == null);

      this.MarkCaptured();
      this.ArrangeClosures();
      this.ArrangeLocals();

      this.VerifyLayout(); // <- only in DEBUG builds
    }

    /// <summary>
    /// Emits the scope init and exit code.
    /// </summary>
    /// <param name="emitter">The emitter.</param>
    /// <param name="exits">The non-exceptional exit targets.</param>
    public override void Emit(Emitter emitter, Exits exits) {
      if (this.Scope == null) {
        var slots = (from slot in this.SlotsInTree
                     where slot.Storage == SlotStorage.Global
                     select slot).ToArray();

        foreach (var slot in slots) {
          Debug.Assert(slot.StorageIndex == null);
          slot.StorageIndex = emitter.Add(default);
        }

        var globals = (from slot in slots
                       select new SourceMap.Global {
                         Index = slot.StorageIndex.Value,
                         Name = slot.Name,
                       }).ToArray();

        emitter.MarkGlobals(globals);
      }

      for (var i = 0; i < this.StackAllocation; i++) {
        emitter.Emit(Opcode.Null);
      }

      if (this.Slots.Count > 0) {
        var parameters = (from slot in this.Slots
                          where slot.Kind == SlotKind.Parameter
                          select new SourceMap.Parameter {
                            IsRestParameter = (slot.Source == SlotSource.ArgumentSlice),
                            Index = slot.SourceIndex.Value,
                            Name = slot.Name,
                          }).ToArray();

        if (parameters.Any()) {
          emitter.MarkParameters(parameters);
        }

        var variables = (from slot in this.SlotsInScope
                         where slot.Storage == SlotStorage.Local || slot.Storage == SlotStorage.Closure
                         select new SourceMap.Variable {
                           Scope = this.GetStepsToClosure(slot),
                           Index = slot.StorageIndex.Value,
                           Name = slot.Name,
                         }).ToArray();

        if (variables.Any()) {
          emitter.MarkVariables(variables);
        }
      }

      if (this.Closure.Count > 0) {
        foreach (var slot in this.Closure) {
          switch (slot.Source) {
            case SlotSource.None:
              emitter.Emit(Opcode.Null);
              break;

            case SlotSource.Argument:
              emitter.Emit(Opcode.LoadArgument, value: slot.SourceIndex.Value);
              break;

            case SlotSource.ArgumentSlice:
              emitter.Emit(Opcode.LoadArgumentList, value: slot.SourceIndex.Value);
              break;
          }
        }

        emitter.Emit(Opcode.EnterClosure, value: this.Closure.Count);

        if (exits.Break != null) {
          Debug.Assert(this is LoopScope);
          exits.Break.Emit(Opcode.LeaveClosure);
        }

        if (exits.Continue != null) {
          Debug.Assert(this is LoopScope);
          exits.Continue.Emit(Opcode.LeaveClosure);
        }
      }
    }

    /// <summary>
    /// Gets the number of steps to walk through the closure chain to reach the slot.
    /// </summary>
    /// <param name="slot">The slot.</param>
    public int GetStepsToClosure(Slot slot) {
      var count = 0;

      if (slot.ClosureScope != null) {
        for (var i = this; i != null; i = i.Scope) {
          if (i.Closure.Count > 0) {
            count++;
          }

          if (i == slot.ClosureScope) {
            break;
          }
        }
      }

      return count;
    }

    /// <summary>
    /// Marks all capturing references in the entire tree.
    /// </summary>
    private void MarkCaptured() {
      bool MustCapture(LexicalScope scope, Slot slot) {
        for (var i = scope; i != slot.Scope; i = i.Scope) {
          if (i is FunctionScope) {
            return true; // <- slot is declared outside the function
          }
        }

        return false; // <- slot is in the same function
      }

      var slots = from slot in this.SlotsInTree
                  where slot.Storage != SlotStorage.Global
                  where slot.References.Any(r => MustCapture(r.Scope, slot))
                  select slot;

      foreach (var slot in slots) {
        slot.Storage = SlotStorage.Closure;

        slot.References.ForEach(r => {
          for (var i = r.Scope; i != slot.Scope; i = i.Scope) {
            if (i is FunctionScope) {
              i.ContainsClosureReferences = true;
            }
          }
        });
      }
    }

    /// <summary>
    /// Arranges and validates the closures.
    /// </summary>
    private void ArrangeClosures() {
      bool CanMoveInto(LexicalScope scope, Slot slot) {
        for (var i = slot.Scope; i != scope; i = i.Scope) {
          if (i is LoopScope) {
            return false; // <- captured slots cannot be moved outside a loop
          }
        }

        return true;
      }

      void AssignClosures(LexicalScope scope) {
        var slots = from slot in scope.SlotsInLocalTree
                    where slot.Storage == SlotStorage.Closure && slot.ClosureScope == null
                    where CanMoveInto(scope, slot)
                    select slot;

        foreach (var slot in slots) {
          slot.ClosureScope = scope;
          slot.StorageIndex = scope.Closure.Count;
          scope.Closure.Add(slot);
        }

        foreach (var inner in scope.Inner) {
          AssignClosures(inner);
        }
      }

      AssignClosures(this);
    }

    /// <summary>
    /// Arranges and validates the local slots.
    /// </summary>
    private void ArrangeLocals() {
      int AssignStorage(LexicalScope scope, int index) {
        var slots = from slot in scope.Slots
                    where slot.Storage == SlotStorage.Local
                    select slot;

        foreach (var slot in slots) {
          slot.StorageIndex = index;
          index++;
        }

        foreach (var inner in scope.Inner) {
          if (inner is FunctionScope) {
            inner.ArrangeLocals();
          } else {
            index = Math.Max(index, AssignStorage(inner, index));
          }
        }

        return index;
      }

      this.StackAllocation = AssignStorage(this, 0);
    }

    /// <summary>
    /// Verifies the scope layout.
    /// </summary>
    [Conditional("DEBUG")]
    private void VerifyLayout() {
      void VerifySlotLayout(Slot slot) {
        Debug.Assert(slot.Name != null);
        Debug.Assert(slot.Scope.Slots.Contains(slot));

        switch (slot.Kind) {
          case SlotKind.Import:
            Debug.Assert(slot.Storage == SlotStorage.Global);
            Debug.Assert(slot.Source == SlotSource.None);
            break;

          case SlotKind.Parameter:
            Debug.Assert(slot.Storage != SlotStorage.Global);
            Debug.Assert(slot.Storage != SlotStorage.Local);
            Debug.Assert(slot.Source != SlotSource.None);
            break;

          case SlotKind.Variable:
            Debug.Assert(slot.Storage != SlotStorage.None);
            Debug.Assert(slot.Source == SlotSource.None);
            break;

          default:
            Debug.Fail("Unexpected slot kind");
            break;
        }

        switch (slot.Storage) {
          case SlotStorage.None:
            Debug.Assert(slot.Source != SlotSource.None); // <- must be a parameter
            Debug.Assert(slot.StorageIndex == null);
            Debug.Assert(slot.ClosureScope == null);
            break;

          case SlotStorage.Global:
            Debug.Assert(slot.Source == SlotSource.None);
            Debug.Assert(slot.StorageIndex == null); // <- assigned during emit
            Debug.Assert(slot.ClosureScope == null);
            Debug.Assert(slot.Scope.Scope == null); // <- globals are allowed only in the global scope
            Debug.Assert(slot.Kind == SlotKind.Import);
            break;

          case SlotStorage.Closure: // <- source may be a parameter
            Debug.Assert(slot.StorageIndex >= 0);
            Debug.Assert(slot.ClosureScope != null);
            Debug.Assert(slot.ClosureScope.Closure[slot.StorageIndex.Value] == slot);
            break;

          case SlotStorage.Local:
            Debug.Assert(slot.Source == SlotSource.None);
            Debug.Assert(slot.StorageIndex >= 0);
            Debug.Assert(slot.ClosureScope == null);
            break;

          default:
            Debug.Fail("Unexpected slot storage");
            break;
        }

        switch (slot.Source) {
          case SlotSource.None:
            Debug.Assert(slot.SourceIndex == null);
            break;

          case SlotSource.Argument:
          case SlotSource.ArgumentSlice:
            Debug.Assert(slot.SourceIndex >= 0);
            Debug.Assert(slot.Kind == SlotKind.Parameter);
            break;

          default:
            Debug.Fail("Unexpected slot source");
            break;
        }
      }

      void VerifyScopeLayout(LexicalScope scope) {
        switch (scope) {
          case BlockScope _:
            Debug.Assert(scope.ContainsClosureReferences == false);
            Debug.Assert(scope.StackAllocation == 0);
            Debug.Assert(scope.Closure.Count == 0);
            break;

          case LoopScope _:
            Debug.Assert(scope.ContainsClosureReferences == false);
            Debug.Assert(scope.StackAllocation == 0);
            break;

          case FunctionScope _:
            Debug.Assert(scope.StackAllocation >= 0);

            foreach (var slot in scope.SlotsInLocalTree) {
              if (slot.Storage == SlotStorage.Local) {
                Debug.Assert(slot.StorageIndex.Value < scope.StackAllocation);
              }
            }
            break;

          default:
            Debug.Fail("Unexpected scope type");
            break;
        }

        foreach (var inner in scope.Inner) {
          Debug.Assert(inner.Scope == scope);
          VerifyScopeLayout(inner);
        }
      }

      foreach (var slot in this.SlotsInTree) {
        VerifySlotLayout(slot);
      }

      VerifyScopeLayout(this);
    }
  }

  /// <summary>
  /// Global function scope.
  /// </summary>
  private sealed class FunctionScope : LexicalScope {
    /// <summary>
    /// Initializes a new instance of the class.
    /// </summary>
    /// <param name="outer">The outer scope, if any.</param>
    public FunctionScope(LexicalScope outer) : base(outer) { }
  }

  /// <summary>
  /// Local block scope.
  /// </summary>
  private sealed class BlockScope : LexicalScope {
    /// <summary>
    /// Initializes a new instance of the class.
    /// </summary>
    /// <param name="outer">The outer scope, if any.</param>
    public BlockScope(LexicalScope outer) : base(outer) { }
  }

  /// <summary>
  /// Local loop scope.
  /// </summary>
  private sealed class LoopScope : LexicalScope {
    /// <summary>
    /// Initializes a new instance of the class.
    /// </summary>
    /// <param name="outer">The outer scope, if any.</param>
    public LoopScope(LexicalScope outer) : base(outer) { }
  }
}
