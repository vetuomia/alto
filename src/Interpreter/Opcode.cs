/// <summary>
/// Virtual machine operation codes.
///
/// The operation code selects the operation during the execution of a virtual
/// machine instruction. Each operation code is paired with zero, one or two
/// operands. The operands are stored directly in the same instruction as the
/// operation code and their meaning depends on the operation.
///
/// Most operations affect the evaluation stack, pushing, popping or modifying
/// the values. These stack effects are described as inputs and outputs of the
/// operation.
///
/// Stack: (inputs in push order) -> (outputs in push order)
///
/// Push order means the order the values are pushed, e.g. (A, B) means
///
/// ```
///   Push(A);
///   Push(B);
/// ```
///
/// leaving the value B on the top of the stack.
/// </summary>
enum Opcode : byte {
  /// <summary>
  /// Pushes null on the stack.
  /// * Stack: () -> (null)
  /// </summary>
  [Assembly("null")]
  Null = 0,

  /// <summary>
  /// Pushes a boolean on the stack.
  /// * Value: 0 = false, otherwise true.
  /// * Stack: () -> (boolean)
  /// </summary>
  [Assembly("false", Value = Operand.Forbidden, DefaultValue = 0)]
  [Assembly("true", Value = Operand.Forbidden, DefaultValue = 1)]
  Boolean,

  /// <summary>
  /// Pushes a number on the stack.
  /// * Value: The number to push.
  /// * Stack: () -> (number)
  /// </summary>
  [Assembly("number", Value = Operand.Required)]
  Number,

  /// <summary>
  /// Swaps the top two values in the stack.
  /// * Stack: (a, b) -> (b, a)
  /// </summary>
  [Assembly("swap")]
  Swap,

  /// <summary>
  /// Copies the top N values from the stack on top of the stack.
  /// * Value: The number of values to copy.
  /// * Stack: (v1, v2, ..., vN) -> (v1, v2, ..., vN, v1, v2, ..., vN)
  /// </summary>
  [Assembly("copy", Value = Operand.Optional, DefaultValue = 1)]
  Copy,

  /// <summary>
  /// Discards the top N values from the stack.
  /// * Value: The number of values to discard.
  /// * Stack: (value1, value2, ..., valueN) -> ()
  /// </summary>
  [Assembly("drop", Value = Operand.Optional, DefaultValue = 1)]
  Drop,

  /// <summary>
  /// Pops the top N values from the stack and converts them into a list.
  /// * Value: The number of values to pop from the stack.
  /// * Stack: (value1, value2, ..., valueN) -> (list)
  /// </summary>
  [Assembly("list", Value = Operand.Required)]
  List,

  /// <summary>
  /// Pops the top 2*N values from the stack and converts them into a table.
  /// * Value: The number of key-value pairs to pop from the stack.
  /// * Stack: (key1, value1, key2, value2, ..., keyN, valueN) -> (table)
  /// </summary>
  [Assembly("table", Value = Operand.Required)]
  Table,

  /// <summary>
  /// Pops the top N values from the stack and moves them into a new closure,
  /// replacing the current closure and making the previous closure as the
  /// parent of the new one.
  /// * Value: The number of values to remove from the top of the stack.
  /// * Stack: (value1, value2, ..., valueN) -> ()
  /// </summary>
  [Assembly("closure", Value = Operand.Required)]
  EnterClosure,

  /// <summary>
  /// Restores the previous closure.
  /// * Stack: () -> ()
  /// </summary>
  [Assembly("endclosure")]
  LeaveClosure,

  /// <summary>
  /// Pushes a function on the stack.
  /// * Param: If non-zero, captures the current closure.
  /// * Value: The function entry point address.
  /// * Stack: () -> (function)
  /// </summary>
  [Assembly("function", Value = Operand.Required)]
  [Assembly("lambda", DefaultParam = 1, Value = Operand.Required)]
  Function,

  /// <summary>
  /// Pushes the receiver on the stack.
  /// * Stack: () -> (receiver)
  /// </summary>
  [Assembly("this")]
  LoadReceiver,

  /// <summary>
  /// Pushes an argument on the stack.
  /// * Value: The index of the argument.
  /// * Stack: () -> (argument)
  /// </summary>
  [Assembly("ldarg", Value = Operand.Required)]
  LoadArgument,

  /// <summary>
  /// Converts arguments into a list and pushes it on the stack.
  /// * Value: The index of the first argument to convert.
  /// * Stack: () -> (list)
  /// </summary>
  [Assembly("ldargs", Value = Operand.Required)]
  LoadArgumentList,

  /// <summary>
  /// Pushes a value from a global on the stack.
  /// * Value: The index of the global in the data section.
  /// * Stack: () -> (value)
  /// </summary>
  [Assembly("ldglob", Value = Operand.Required)]
  LoadGlobal,

  /// <summary>
  /// Pushes a value from a variable on the stack.
  /// * Param: The number of steps in the closure chain. Zero is the local scope.
  /// * Value: The index of the variable in the selected scope.
  /// * Stack: () -> (value)
  /// </summary>
  [Assembly("ldvar", Param = Operand.Optional, DefaultParam = 0, Value = Operand.Required)]
  LoadVariable,

  /// <summary>
  /// Pops the top value from the stack and assigns it to a variable.
  /// * Param: The steps in the closure chain. Zero is the local scope.
  /// * Value: The index of the variable in the selected scope.
  /// * Stack: (value) -> (value)
  /// </summary>
  [Assembly("stvar", Param = Operand.Optional, DefaultParam = 0, Value = Operand.Required)]
  StoreVariable,

  /// <summary>
  /// Pops the top two values from the stack and extracts a member from the
  /// first using the second as the key.
  /// * Stack: (object, key) -> (value)
  /// </summary>
  [Assembly("ldelem")]
  LoadElement,

  /// <summary>
  /// Pops the top three values from the stack and assigns the third to a member
  /// of the first using the second as the key.
  /// * Stack: (object, key, value) -> (value)
  /// </summary>
  [Assembly("stelem")]
  StoreElement,

  /// <summary>
  /// Pops the top two values from the stack and pushes a boolean indicating
  /// whether the values are equal.
  /// * Stack: (a, b) -> (boolean)
  /// </summary>
  [Assembly("equal")]
  Equal,

  /// <summary>
  /// Pops the top two values from the stack and pushes a boolean indicating
  /// whether the first is less than the second. If either of the values is not
  /// a number, the result is false.
  /// * Stack: (a, b) -> (boolean)
  /// </summary>
  [Assembly("less")]
  Less,

  /// <summary>
  /// Pops the top two values from the stack and pushes a boolean indicating
  /// whether the first is less than or equal to the second. If either of the
  /// values is not a number, the result is false.
  /// * Stack: (a, b) -> (boolean)
  /// </summary>
  [Assembly("lequal")]
  LessOrEqual,

  /// <summary>
  /// Pops the top two values from the stack and pushes a boolean indicating
  /// whether the first is greater than the second. If either of the values is
  /// not a number, the result is false.
  /// * Stack: (a, b) -> (boolean)
  /// </summary>
  [Assembly("greater")]
  Greater,

  /// <summary>
  /// Pops the top two values from the stack and pushes a boolean indicating
  /// whether the first is greater than or equal to the second. If either of
  /// the values is not a number, the result is false.
  /// * Stack: (a, b) -> (boolean)
  /// </summary>
  [Assembly("gequal")]
  GreaterOrEqual,

  /// <summary>
  /// Pops the top value from the stack and pushes a negation of the value. If
  /// the value is not a number, the result is NaN.
  /// * Stack: (number) -> (number)
  /// </summary>
  [Assembly("neg")]
  Negate,

  /// <summary>
  /// Pops the top two values from the stack, adds the second to the first and
  /// pushes the result. If either of the values is not a number, the result is
  /// NaN.
  /// * Stack: (a, b) -> (number)
  /// </summary>
  [Assembly("add")]
  Add,

  /// <summary>
  /// Pops the top two values from the stack, subtracts the second from the
  /// first and pushes the result. If either of the values is not a number, the
  /// result is NaN.
  /// * Stack: (a, b) -> (number)
  /// </summary>
  [Assembly("sub")]
  Subtract,

  /// <summary>
  /// Pops the top two values from the stack, multiplies the second by the first
  /// and pushes the result. If either of the values is not a number, the result
  /// is NaN.
  /// * Stack: (a, b) -> (number)
  /// </summary>
  [Assembly("mul")]
  Multiply,

  /// <summary>
  /// Pops the top two values from the stack, divides the second by the first
  /// and pushes the result. If either of the values is not a number, the result
  /// is NaN.
  /// * Stack: (a, b) -> (number)
  /// </summary>
  [Assembly("div")]
  Divide,

  /// <summary>
  /// Pops the top two values from the stack, multiplies the second by the first
  /// and pushes the remainder. If either of the values is not a number, the
  /// result is NaN.
  /// * Stack: (a, b) -> (number)
  /// </summary>
  [Assembly("rem")]
  Remainder,

  /// <summary>
  /// Pops the top value from the stack, converts it into a boolean and pushes
  /// the logical negation on the stack.
  /// * Stack: (value) -> (boolean)
  /// </summary>
  [Assembly("not")]
  Not,

  /// <summary>
  /// Pops the top two values from the stack, converts them into booleans and
  /// pushes the result of a logical AND operation on the stack.
  /// * Stack: (a, b) -> (boolean)
  /// </summary>
  [Assembly("and")]
  And,

  /// <summary>
  /// Pops the top two values from the stack, converts them into booleans and
  /// pushes the result of a logical OR operation on the stack.
  /// * Stack: (a, b) -> (boolean)
  /// </summary>
  [Assembly("or")]
  Or,

  /// <summary>
  /// Pops the top two values from the stack, converts them into booleans and
  /// pushes the result of a logical XOR operation on the stack.
  /// * Stack: (a, b) -> (boolean)
  /// </summary>
  [Assembly("xor")]
  Xor,

  /// <summary>
  /// Jumps to the address.
  /// * Value: The target address.
  /// * Stack: () -> ()
  /// </summary>
  [Assembly("jump", Value = Operand.Required)]
  Jump,

  /// <summary>
  /// Pops the top value from the stack, converts it into a boolean and jumps
  /// to the target address if the result matches the Param operand.
  /// * Param: 0 = false, otherwise true.
  /// * Value: The target address.
  /// * Stack: (boolean) -> ()
  /// </summary>
  [Assembly("jump?", Param = Operand.Forbidden, DefaultParam = 1, Value = Operand.Required)]
  [Assembly("jump!", Param = Operand.Forbidden, DefaultParam = 0, Value = Operand.Required)]
  ConditionalJump,

  /// <summary>
  /// Converts the top value from the stack to a boolean without popping it,
  /// and if the result is false, jumps to the target address. Otherwise, pops
  /// the value and continues without jumping.
  /// * Value: The target address.
  /// * Stack: (value) -> (value if false)
  /// </summary>
  [Assembly("cand", Value = Operand.Required)]
  ConditionalAnd,

  /// <summary>
  /// Converts the top value from the stack to a boolean without popping it,
  /// and if the result is true, jumps to the target address. Otherwise, pops
  /// the value and continues without jumping.
  /// * Value: The target address.
  /// * Stack: (value) -> (value if true)
  /// </summary>
  [Assembly("cor", Value = Operand.Required)]
  ConditionalOr,

  /// <summary>
  /// Pops the top value from the stack, converts it into an exception and
  /// throws it. If an exception handler is present, unwinds the stack and
  /// jumps to the innermost handler address, pushing the exception on the
  /// stack. Otherwise, exits the function and continues propagating the
  /// exception to outer scopes.
  /// * Stack: (error) -> ()
  /// </summary>
  [Assembly("throw")]
  Throw,

  /// <summary>
  /// Creates a new exception handler and installs it as the innermost handler.
  /// * Value: The exception handler address.
  /// </summary>
  [Assembly("try", Value = Operand.Required)]
  EnterTry,

  /// <summary>
  /// Removes the innermost exception handler and jumps to the target address.
  /// * Value: The target address.
  /// </summary>
  [Assembly("endtry", Value = Operand.Required)]
  LeaveTry,

  /// <summary>
  /// Enters the finally block at the target address.
  /// * Stack: () -> (return address)
  /// * Value: The target address.
  /// </summary>
  [Assembly("finally", Value = Operand.Required)]
  EnterFinally,

  /// <summary>
  /// Leaves the finally block and returns to the previous address.
  /// * Stack: (return address) -> ()
  /// </summary>
  [Assembly("endfinally")]
  LeaveFinally,

  /// <summary>
  /// Calls a function with arguments from the stack.
  /// * Value: The number of arguments to remove from the top of the stack.
  /// * Stack: (function, receiver, arg1, arg2, ..., argN) -> (value)
  /// </summary>
  [Assembly("call", Value = Operand.Required)]
  Call,

  /// <summary>
  /// Calls a function with an argument list from the stack.
  /// * Stack: (function, receiver, list) -> (value)
  /// </summary>
  [Assembly("apply")]
  Apply,

  /// <summary>
  /// Returns from the function, with the top value from the stack as the result.
  /// * Stack: (value) -> ()
  /// </summary>
  [Assembly("return")]
  Return,
}
