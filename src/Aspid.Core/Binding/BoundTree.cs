using Aspid.Core.Syntax;

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

public enum BoundUnaryOperatorKind
{
    Identity, // +a
    Negation, // -a
    LogicalNegation // !a 
}

public sealed class BoundUnaryOperator
{
    public Lexer.LexerTokenKind SyntaxKind { get; }
    public BoundUnaryOperatorKind Kind { get; }
    public TypeSymbol OperandType { get; }
    public TypeSymbol ResultType { get; }

    private BoundUnaryOperator(Lexer.LexerTokenKind syntaxKind, BoundUnaryOperatorKind kind, TypeSymbol operandType,
        TypeSymbol resultType)
    {
        SyntaxKind = syntaxKind;
        Kind = kind;
        OperandType = operandType;
        ResultType = resultType;
    }

    private BoundUnaryOperator(Lexer.LexerTokenKind syntaxKind, BoundUnaryOperatorKind kind, TypeSymbol operandType)
        : this(syntaxKind, kind, operandType, operandType)
    {
    }

    private static readonly Dictionary<Lexer.LexerTokenKind, BoundUnaryOperatorKind> SyntaxMap = new()
    {
        [Lexer.LexerTokenKind.Plus] = BoundUnaryOperatorKind.Identity,
        [Lexer.LexerTokenKind.Minus] = BoundUnaryOperatorKind.Negation,
    };


    public static BoundUnaryOperatorKind? GetOperatorKind(Lexer.LexerTokenKind kind)
        => SyntaxMap.TryGetValue(kind, out var k) ? k : null;


    public static BoundUnaryOperator? Bind(BoundUnaryOperatorKind kind, TypeSymbol operandType)
    {
        if (operandType.IsNumeric)
        {
            switch (kind)
            {
                case BoundUnaryOperatorKind.Identity:
                    return new(Lexer.LexerTokenKind.Plus, kind, operandType);
                case BoundUnaryOperatorKind.Negation:
                    return new(Lexer.LexerTokenKind.Minus, kind, operandType);
            }
        }

        return null;
    }
}

public enum BoundBinaryOperatorKind
{
    Addition,
    Subtraction,
    Multiplication,
    Division,
    Equals,
    NotEquals,
}

public sealed record BoundBinaryOperator
{
    public BoundBinaryOperatorKind Kind { get; }
    public TypeSymbol LeftType { get; }
    public TypeSymbol RightType { get; }
    public TypeSymbol ResultType { get; }

    private BoundBinaryOperator(BoundBinaryOperatorKind kind, TypeSymbol leftType, TypeSymbol rightType,
        TypeSymbol resultType)
    {
        Kind = kind;
        LeftType = leftType;
        RightType = rightType;
        ResultType = resultType;
    }

    private static readonly Dictionary<Lexer.LexerTokenKind, BoundBinaryOperatorKind> SyntaxMap = new()
    {
        [Lexer.LexerTokenKind.Plus] = BoundBinaryOperatorKind.Addition,
        [Lexer.LexerTokenKind.Minus] = BoundBinaryOperatorKind.Subtraction,
        [Lexer.LexerTokenKind.Star] = BoundBinaryOperatorKind.Multiplication,
        [Lexer.LexerTokenKind.Div] = BoundBinaryOperatorKind.Division,
        [Lexer.LexerTokenKind.EqEq] = BoundBinaryOperatorKind.Equals,
        [Lexer.LexerTokenKind.NotEq] = BoundBinaryOperatorKind.NotEquals
    };

    public static BoundBinaryOperatorKind? GetOperatorKind(Lexer.LexerTokenKind kind)
        => SyntaxMap.TryGetValue(kind, out var k) ? k : null;

    public static BoundBinaryOperator? Bind(BoundBinaryOperatorKind kind, TypeSymbol left, TypeSymbol right)
    {
        if (kind == BoundBinaryOperatorKind.Equals || kind == BoundBinaryOperatorKind.NotEquals)
        {
            if (left == right)
                return new BoundBinaryOperator(kind, left, right, TypeSymbol.Bool);
            if (left.IsNumeric && right.IsNumeric) // Numeric types can be comparable
                return new BoundBinaryOperator(kind, left, right, TypeSymbol.Bool);
        }
        
        // Round to double for numeric operations
        if (left.IsNumeric && right.IsNumeric)
        {
            var resultType = (left == TypeSymbol.Double || right == TypeSymbol.Double)
                ? TypeSymbol.Double
                : TypeSymbol.Int;

            return new BoundBinaryOperator(kind, left, right, resultType);
        }

        // String concatenation
        if (kind == BoundBinaryOperatorKind.Addition && (left.IsString || right.IsString))
        {
            return new BoundBinaryOperator(kind, left, right, TypeSymbol.String);
        }

        return null;
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

public sealed class BoundVariableDeclarationStatement(BoundVariableExpression variable, BoundNode? initializer)
    : BoundNode
{
    public BoundVariableExpression Variable { get; } = variable;
    public BoundNode? Initializer { get; } = initializer;
    public override TypeSymbol Type => TypeSymbol.Void;
}

public sealed class BoundAssignmentStatement(BoundVariableExpression variable, BoundNode expression) : BoundNode
{
    public BoundVariableExpression Variable { get; } = variable;
    public BoundNode Expression { get; } = expression;

    public override TypeSymbol Type => TypeSymbol.Void;
}

public sealed class BoundUnaryExpression(BoundUnaryOperator op, BoundNode operand) : BoundNode
{
    public BoundUnaryOperator Op { get; } = op;
    public BoundNode Operand { get; } = operand;
    public override TypeSymbol Type => Op.ResultType;
}

public sealed class BoundBinaryExpression(BoundNode left, BoundBinaryOperator op, BoundNode right) : BoundNode
{
    public BoundNode Left { get; } = left;
    public BoundBinaryOperator Op { get; } = op;
    public BoundNode Right { get; } = right;
    public override TypeSymbol Type { get; } = op.ResultType;
}

public sealed class BoundLiteralExpression(object value) : BoundNode
{
    public object Value { get; } = value;

    public override TypeSymbol Type => Value switch
    {
        int => TypeSymbol.Int,
        double => TypeSymbol.Double,
        bool => TypeSymbol.Bool,
        string => TypeSymbol.String,
        null => TypeSymbol.Void,
        _ => throw new Exception($"Unexpected literal type: {Value.GetType()}")
    };
}

public sealed class BoundConversionExpression(TypeSymbol type, BoundNode expression) : BoundNode
{
    public override TypeSymbol Type { get; } = type;
    public BoundNode Expression { get; } = expression;
}

public sealed class BoundVariableExpression(string name, TypeSymbol type) : BoundNode
{
    public string Name { get; } = name;
    public override TypeSymbol Type { get; } = type;
}