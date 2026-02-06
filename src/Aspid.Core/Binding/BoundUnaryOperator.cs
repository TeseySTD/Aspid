using Aspid.Core.Syntax;
using static Aspid.Core.Binding.BoundUnaryOperatorKind;

namespace Aspid.Core.Binding;

public enum BoundUnaryOperatorKind
{
    Identity, // +a
    Negation, // -a
    LogicalNegation, // !a 
    PreIncrement, // ++a
    PreDecrement, // --a
    PostIncrement, // a++
    PostDecrement, // a--
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

    private static readonly Dictionary<Lexer.LexerTokenKind, BoundUnaryOperatorKind> PrefixSyntaxMap = new()
    {
        [Lexer.LexerTokenKind.Plus] = Identity,
        [Lexer.LexerTokenKind.Minus] = Negation,
        [Lexer.LexerTokenKind.Not] = LogicalNegation,
        [Lexer.LexerTokenKind.PlusPlus] = PreIncrement,
        [Lexer.LexerTokenKind.MinusMinus] = PreDecrement,
    };

    private static readonly Dictionary<Lexer.LexerTokenKind, BoundUnaryOperatorKind> PostfixSyntaxMap = new()
    {
        [Lexer.LexerTokenKind.PlusPlus] = PostIncrement,
        [Lexer.LexerTokenKind.MinusMinus] = PostDecrement,
    };

    public static BoundUnaryOperatorKind? GetPrefixOperatorKind(Lexer.LexerTokenKind kind)
        => PrefixSyntaxMap.TryGetValue(kind, out var k) ? k : null;

    public static BoundUnaryOperatorKind? GetPostfixOperatorKind(Lexer.LexerTokenKind kind)
        => PostfixSyntaxMap.TryGetValue(kind, out var k) ? k : null;


    public static BoundUnaryOperator? Bind(BoundUnaryOperatorKind kind, TypeSymbol operandType)
    {
        var isOperandEqualsAny = operandType == TypeSymbol.Any;
        return kind switch
        {
            Identity when operandType.IsNumeric || isOperandEqualsAny => new(Lexer.LexerTokenKind.Plus, kind, operandType),
            Negation when operandType.IsNumeric || isOperandEqualsAny => new(Lexer.LexerTokenKind.Minus, kind, operandType),
            PreIncrement when operandType.IsNumeric || isOperandEqualsAny => new(Lexer.LexerTokenKind.PlusPlus, kind, operandType),
            PostIncrement when operandType.IsNumeric || isOperandEqualsAny => new(Lexer.LexerTokenKind.PlusPlus, kind, operandType),
            PreDecrement when operandType.IsNumeric || isOperandEqualsAny => new(Lexer.LexerTokenKind.MinusMinus, kind, operandType),
            PostDecrement when operandType.IsNumeric || isOperandEqualsAny => new(Lexer.LexerTokenKind.MinusMinus, kind, operandType),
            LogicalNegation when operandType.IsBoolean || isOperandEqualsAny => new(Lexer.LexerTokenKind.Not, kind, operandType),
            _ => null
        };
    }
}