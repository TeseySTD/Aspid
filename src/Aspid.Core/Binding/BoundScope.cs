using System.Diagnostics.CodeAnalysis;

namespace Aspid.Core.Binding;

public sealed class BoundScope
{
    public BoundScope? Parent { get; }
    private readonly Dictionary<string, TypeSymbol> _variables = new();

    public BoundScope(BoundScope? parent)
    {
        Parent = parent;
    }

    public bool TryDeclare(string name, TypeSymbol type)
    {
        if (!_variables.TryAdd(name, type))
            return false;

        return true;
    }

    public bool TryLookup(string name, [NotNullWhen(true)] out TypeSymbol? type)
    {
        if (_variables.TryGetValue(name, out type))
            return true;

        return Parent?.TryLookup(name, out type) ?? false;
    }
}