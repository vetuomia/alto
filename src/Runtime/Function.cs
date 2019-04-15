/// <summary>
/// Function delegate, pointing either to an interpreted function or to a .NET
/// method. This uniformity allows .NET code to seamlessly call interpreted
/// functions and vice versa, without knowing the implementation details.
///
/// All functions are variadic (take a variable number of arguments) and return
/// a value. If the function did not produce a result, the return value is null.
///
/// Functions are unbound by default, meaning the function receives the "this"
/// reference (receiver) through a parameter. The function can be bound by
/// wrapping it to another function that replaces the receiver with the bound
/// reference.
/// </summary>
/// <param name="receiver">The receiver, also known as the "this" reference.</param>
/// <param name="arguments">The arguments.</param>
/// <returns>The result, or null the function did not produce a value.</returns>
delegate Value Function(Value receiver, params Value[] arguments);
