namespace Aspid.Core.Binding;

public class BoundStatement : BoundNode
{
    public override TypeSymbol Type => TypeSymbol.Void;
}

public sealed class BoundBlockStatement(List<BoundNode> statements) : BoundStatement 
{
    public List<BoundNode> Statements { get; } = statements;
}

public sealed class BoundVariableDeclarationStatement(BoundVariableExpression variable, BoundNode? initializer)
    : BoundStatement 
{
    public BoundVariableExpression Variable { get; } = variable;
    public BoundNode? Initializer { get; } = initializer;
}

public sealed class BoundFunctionDeclarationStatement(FunctionSymbol function, BoundNode action) : BoundStatement
{
    public FunctionSymbol Function { get; } = function;
    public BoundNode Action { get; } = action;
}

public sealed class BoundAssignmentStatement(BoundVariableExpression variable, BoundNode expression) : BoundStatement
{
    public BoundVariableExpression Variable { get; } = variable;
    public BoundNode Expression { get; } = expression;
}

public sealed class BoundArrayAssignmentStatement(
    BoundArrayAccessExpression arrayAccess,
    BoundNode expression
) : BoundStatement
{
    public BoundArrayAccessExpression ArrayAccess { get; } = arrayAccess;
    public BoundNode Expression { get; } = expression;
}

public sealed class BoundIfStatement(
    BoundNode condition,
    BoundNode thenStatement,
    BoundNode? elseStatement
) : BoundStatement
{
    public BoundNode Condition { get; } = condition;
    public BoundNode ThenStatement { get; } = thenStatement;
    public BoundNode? ElseStatement { get; } = elseStatement;
}

public sealed class BoundWhileStatement(
    BoundNode condition,
    BoundNode actionStatement
) : BoundStatement
{
    public BoundNode Condition { get; } = condition;
    public BoundNode ActionStatement { get; } = actionStatement;
}

public sealed class BoundDoWhileStatement(
    BoundNode condition,
    BoundNode actionStatement
) : BoundStatement
{
    public BoundNode Condition { get; } = condition;
    public BoundNode ActionStatement { get; } = actionStatement;
}

public sealed class BoundForInStatement(
    BoundVariableDeclarationStatement variable,
    BoundNode enumerator,
    BoundNode actionStatement
) : BoundStatement
{
    public BoundVariableDeclarationStatement Variable { get; } = variable;
    public BoundNode Enumerator { get; } = enumerator;
    public BoundNode ActionStatement { get; } = actionStatement;
}

public sealed class BoundReturnStatement(BoundNode? expression) : BoundStatement
{
    public BoundNode? Expression { get; } = expression;
}