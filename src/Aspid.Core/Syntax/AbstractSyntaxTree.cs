namespace Aspid.Core.Syntax;

public abstract record SyntaxNode
{
    public abstract string Kind { get; }
    public abstract IEnumerable<SyntaxNode> GetChildren();
}

public abstract record Expression : SyntaxNode;

public abstract record Statement : SyntaxNode;

public sealed record ExpressionStatement(Expression Expression) : Statement
{
    public override string Kind => nameof(ExpressionStatement);

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Expression;
    }
}

public sealed record NumberExpression(Lexer.Token NumberToken) : Expression
{
    public override string Kind => nameof(NumberExpression);

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield break;
    }
}

public sealed record StringExpression(Lexer.Token StringToken) : Expression
{
    public override string Kind => nameof(StringExpression);

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield break;
    }
}

public sealed record BooleanExpression(Lexer.Token KeywordToken, bool Value) : Expression
{
    public override string Kind => nameof(BooleanExpression);

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

public sealed record UnaryExpression(Lexer.Token OperatorToken, Expression Operand) : Expression
{
    public override string Kind => nameof(UnaryExpression);

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Operand;
    }
}

public sealed record PostfixUnaryExpression(Lexer.Token OperatorToken, Expression Operand) : Expression
{
    public override string Kind => nameof(UnaryExpression);

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Operand;
    }
}

public sealed record CallExpression(Expression Function, List<Expression> Arguments) : Expression
{
    public override string Kind => nameof(CallExpression);

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Function;
        foreach (var arg in Arguments) yield return arg;
    }
}

public sealed record AssignmentStatement(
    Lexer.Token IdentifierToken,
    Expression Expression
) : Statement
{
    public override string Kind => nameof(AssignmentStatement);

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Expression;
    }
}

public sealed record VariableDeclarationStatement(
    Lexer.Token Variable,
    Lexer.Token TypeIdentifier,
    Expression? Initializer
) : Statement
{
    public override string Kind => nameof(VariableDeclarationStatement);

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        if (Initializer != null)
            yield return Initializer;
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