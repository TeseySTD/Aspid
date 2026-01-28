namespace Core.Syntax;

public abstract record SyntaxNode
{
    public abstract string Kind { get; }
    public abstract IEnumerable<SyntaxNode> GetChildren();
}

public abstract record Expression : SyntaxNode;

public sealed record NumberExpression(Lexer.Token NumberToken) : Expression
{
    public override string Kind => nameof(NumberExpression);

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield break;
    }
}

public sealed record VariableExpression(Lexer.Token VariableName) : Expression
{
    public override string Kind => nameof(VariableExpression);

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield break;
    }
}

public sealed record BinaryExpression(Expression Left, Lexer.Token OperatorToken, Expression Right)
    : Expression
{
    public override string Kind => nameof(BinaryExpression);

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Left;
        yield return Right;
    }
}

public sealed record AssignmentExpression(VariableExpression Left, Expression Right)
    : Expression
{
    public override string Kind => nameof(AssignmentExpression);

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Left;
        yield return Right;
    }
}

public sealed record ParenthesizedExpression(
    Lexer.Token OpenParenToken,
    Expression Expression,
    Lexer.Token CloseParenToken
) : Expression
{
    public override string Kind => nameof(ParenthesizedExpression);

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Expression;
    }
}