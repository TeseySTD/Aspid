using System.Diagnostics.CodeAnalysis;

namespace Aspid.Core.Binding;

public sealed class BoundScope
{
    public BoundScope? Parent { get; }
    private readonly Dictionary<string, TypeSymbol> _variables = new();
    private readonly Dictionary<string, FunctionSymbol> _functions = new();

    public BoundScope(BoundScope? parent)
    {
        Parent = parent;
    }

    public bool TryDeclareVariable(string name, TypeSymbol type)
    {
        if (!_variables.TryAdd(name, type))
            return false;

        return true;
    }

    public bool TryLookupVariable(string name, [NotNullWhen(true)] out TypeSymbol? type)
    {
        if (_variables.TryGetValue(name, out type))
            return true;

        return Parent?.TryLookupVariable(name, out type) ?? false;
    }

    public bool TryDeclareFunction(FunctionSymbol symbol)
    {
        if (!_functions.TryAdd(symbol.Name, symbol))
            return false;

        return true;
    }

    public bool TryLookupFunction(string name, [NotNullWhen(true)] out FunctionSymbol? type)
    {
        if (_functions.TryGetValue(name, out type))
            return true;

        return Parent?.TryLookupFunction(name, out type) ?? false;
    }
}