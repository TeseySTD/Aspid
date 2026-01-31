using Aspid.Core.Syntax;

namespace Aspid.Core.Binding;

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