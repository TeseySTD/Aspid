using Aspid.Core.Syntax;

namespace Aspid.Core.Binding;

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