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

public sealed record ArrayExpression(List<Expression> Elements) : Expression
{
    public override string Kind => nameof(ArrayExpression);
    public override IEnumerable<SyntaxNode> GetChildren() => Elements;
}

public sealed record ArrayAccessExpression(Expression Array, Expression Index) : Expression
{
    public override string Kind => nameof(ArrayAccessExpression);

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Array;
        yield return Index;
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

public sealed record BlockStatement(
    Lexer.Token IdentToken,
    IEnumerable<Statement> Statements,
    Lexer.Token DedentToken) : Statement
{
    public override string Kind => nameof(BlockStatement);

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        foreach (var statement in Statements)
            yield return statement;
    }
}

public sealed record AssignmentStatement(
    Expression Identifier,
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

public sealed record FunctionDeclarationStatement(
    Lexer.Token FnKeyword,
    Lexer.Token Name,
    Lexer.Token OParen,
    List<ParameterSyntax> Arguments,
    Lexer.Token CParen,
    Lexer.Token? ArrowType,
    Lexer.Token? ReturnType,
    Lexer.Token Colon,
    Statement Body
) : Statement
{
    public override string Kind => nameof(FunctionDeclarationStatement);

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        foreach (var expression in Arguments)
            yield return expression;
        yield return Body;
    }
}
public sealed record ParameterSyntax(
    Lexer.Token Identifier,
    Lexer.Token? Colon,
    Lexer.Token? TypeAnnotation
) : SyntaxNode
{
    public override string Kind => nameof(ParameterSyntax);

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield break; 
    }
}
public sealed record IfStatement(
    Lexer.Token IfKeyword,
    Lexer.Token Colon,
    Expression ConditionExpression,
    Statement ThenStatement,
    Statement? ElseStatement) : Statement
{
    public override string Kind => nameof(IfStatement);

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return ConditionExpression;
        yield return ThenStatement;
        if (ElseStatement != null)
            yield return ElseStatement;
    }
}

public sealed record WhileStatement(
    Lexer.Token WhileKeyword,
    Lexer.Token Colon,
    Expression ConditionExpression,
    Statement ActionStatement
) : Statement
{
    public override string Kind => nameof(WhileStatement);

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return ConditionExpression;
        yield return ActionStatement;
    }
}

public sealed record DoWhileStatement(
    Lexer.Token Do,
    Lexer.Token WhileKeyword,
    Lexer.Token Colon,
    Expression ConditionExpression,
    Statement ActionStatement
) : Statement
{
    public override string Kind => nameof(DoWhileStatement);

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return ConditionExpression;
        yield return ActionStatement;
    }
}

public sealed record ForInStatement(
    Lexer.Token ForKeyword,
    Lexer.Token Variable,
    Lexer.Token InKeyword,
    Expression Enumerator,
    Lexer.Token Colon,
    Statement ActionStatement
) : Statement
{
    public override string Kind => nameof(ForInStatement);

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Enumerator;
        yield return ActionStatement;
    }
}

public sealed record ReturnStatement(
    Lexer.Token ReturnKeyword,
    Expression? Expression
) : Statement
{
    public override string Kind => nameof(ReturnStatement);

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        if (Expression != null)
            yield return Expression;
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