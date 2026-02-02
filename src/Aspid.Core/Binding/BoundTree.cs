namespace Aspid.Core.Binding;

public sealed record TypeSymbol
{
    public string Name { get; }

    private TypeSymbol(string name, TypeSymbol? elementType = null)
    {
        Name = name;
        ElementType = elementType;
    }

    public override string ToString() => Name;
    public bool IsArray => Name.EndsWith("[]");
    public TypeSymbol? ElementType { get; }

    // Built-in types
    public static readonly TypeSymbol Int = new("int");
    public static readonly TypeSymbol Double = new("double");
    public static readonly TypeSymbol Bool = new("bool");
    public static readonly TypeSymbol String = new("string");
    public static readonly TypeSymbol Void = new("void");
    public static readonly TypeSymbol Any = new("any");
    public static readonly TypeSymbol Error = new("!error!");
    public bool IsNumeric => this == Int || this == Double;
    public bool IsBoolean => this == Bool;
    public bool IsString => this == String;

    public static TypeSymbol GetArrayType(TypeSymbol elementType)
    {
        return new TypeSymbol($"{elementType.Name}[]", elementType);
    }

    public static TypeSymbol? Parse(string s)
    {
        if (s.EndsWith("[]"))
        {
            var elementTypeName = s.Substring(0, s.Length - 2);
            var elementType = Parse(elementTypeName);
            if (elementType != null)
                return GetArrayType(elementType);
        }

        return s switch
        {
            "int" => Int,
            "string" => String,
            "bool" => Bool,
            "double" => Double,
            "void" => Void,
            "any" => Any,
            _ => null
        };
    }
}

public abstract class BoundNode
{
    public abstract TypeSymbol Type { get; }
}

public sealed class BoundErrorNode(string error) : BoundNode
{
    public string ErrorText { get; } = error;
    public override TypeSymbol Type => TypeSymbol.Error;
}