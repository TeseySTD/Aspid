namespace Aspid.Core.Binding;

public sealed class TypeSymbol
{
    public string Name { get; }

    private TypeSymbol(string name)
    {
        Name = name;
    }

    public override string ToString() => Name;

    // Built-in types
    public static readonly TypeSymbol Int = new("int");
    public static readonly TypeSymbol Double = new("double");
    public static readonly TypeSymbol Bool = new("bool");
    public static readonly TypeSymbol String = new("string");
    public static readonly TypeSymbol Void = new("void");
    public static readonly TypeSymbol Error = new("!error!");
    public bool IsNumeric => this == Int || this == Double;
    public bool IsBoolean => this == Bool;
    public bool IsString => this == String;

    public static TypeSymbol? Parse(string s) => s switch
    {
        "int" => Int,
        "double" => Double,
        "bool" => Bool,
        "string" => String,
        "void" => Void,
        _ => null
    };
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
