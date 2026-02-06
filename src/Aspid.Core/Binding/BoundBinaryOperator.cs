using Aspid.Core.Syntax;

namespace Aspid.Core.Binding;

public enum BoundBinaryOperatorKind
{
    Addition,
    Subtraction,
    Multiplication,
    Division,

    // Comparison
    Equals,
    NotEquals,
    Less,
    LessOrEquals,
    Greater,
    GreaterOrEquals
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
        [Lexer.LexerTokenKind.NotEq] = BoundBinaryOperatorKind.NotEquals,
        [Lexer.LexerTokenKind.Greater] = BoundBinaryOperatorKind.Greater,
        [Lexer.LexerTokenKind.GreaterOrEqual] = BoundBinaryOperatorKind.GreaterOrEquals,
        [Lexer.LexerTokenKind.Less] = BoundBinaryOperatorKind.Less,
        [Lexer.LexerTokenKind.LessOrEqual] = BoundBinaryOperatorKind.LessOrEquals,
    };

    public static BoundBinaryOperatorKind? GetOperatorKind(Lexer.LexerTokenKind kind)
        => SyntaxMap.TryGetValue(kind, out var k) ? k : null;

    public static BoundBinaryOperator? Bind(BoundBinaryOperatorKind kind, TypeSymbol left, TypeSymbol right)
    {
        // Equality
        if (kind == BoundBinaryOperatorKind.Equals || kind == BoundBinaryOperatorKind.NotEquals)
        {
            if (left == right)
                return new BoundBinaryOperator(kind, left, right, TypeSymbol.Bool);
            if (left.IsNumeric && right.IsNumeric)
                return new BoundBinaryOperator(kind, left, right, TypeSymbol.Bool);
            if (left == TypeSymbol.Any || right == TypeSymbol.Any) // Allow to compare with any
                return new BoundBinaryOperator(kind, left, right, TypeSymbol.Bool);
        }

        // Relational
        if (kind == BoundBinaryOperatorKind.Less || kind == BoundBinaryOperatorKind.LessOrEquals ||
            kind == BoundBinaryOperatorKind.Greater || kind == BoundBinaryOperatorKind.GreaterOrEquals)
        {
            if (left.IsNumeric && right.IsNumeric)
                return new BoundBinaryOperator(kind, left, right, TypeSymbol.Bool);
            if (left == TypeSymbol.Any || right == TypeSymbol.Any) // Allow to compare with any
                return new BoundBinaryOperator(kind, left, right, TypeSymbol.Bool);
        }

        if (left == TypeSymbol.Any || right == TypeSymbol.Any)
            return new BoundBinaryOperator(kind, left, right, TypeSymbol.Any);

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