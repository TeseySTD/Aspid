using System.Diagnostics.CodeAnalysis;
using Aspid.Core.Binding;

namespace Aspid.Core.Evaluator;

public class ExecutionScope(
    Dictionary<string, object> variables,
    Dictionary<FunctionSymbol, Func<object[], object?>> functions
)
{
    private Dictionary<string, object> Variables { get; } = variables;
    private Dictionary<FunctionSymbol, Func<object[], object?>> Functions { get; } = functions;

    public ExecutionScope() : this(new(), new()) { }

    public bool TryGetVariable(string name, [NotNullWhen(true)]out object? variable)
    {
        return Variables.TryGetValue(name, out variable);
    }

    public void SetVariable(string name, object value)
    {
        Variables[name] = value;
    }

    public bool IsVariableDeclared(string name)
    {
        return Variables.ContainsKey(name);
    }

    public bool TryGetFunction(FunctionSymbol symbol, [NotNullWhen(true)]out Func<object[], object?>? variable)
    {
        return Functions.TryGetValue(symbol, out variable);
    }

    public void SetFunction(FunctionSymbol symbol, Func<object[], object?> node)
    {
        Functions[symbol] = node;
    }

    public bool IsFunctionDeclared(FunctionSymbol symbol)
    {
        return Functions.ContainsKey(symbol);
    }
}