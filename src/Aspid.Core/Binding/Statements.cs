namespace Aspid.Core.Binding;

public sealed class BoundBlockStatement(List<BoundNode> statements) : BoundNode
{
    public List<BoundNode> Statements { get; } = statements;
    public override TypeSymbol Type => TypeSymbol.Void;
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

public sealed class BoundArrayAssignmentStatement(
    BoundArrayAccessExpression arrayAccess,
    BoundNode expression
) : BoundNode
{
    public BoundArrayAccessExpression ArrayAccess { get; } = arrayAccess;
    public BoundNode Expression { get; } = expression;

    public override TypeSymbol Type => TypeSymbol.Void;
}

public sealed class BoundIfStatement(
    BoundNode condition,
    BoundNode thenStatement,
    BoundNode? elseStatement
) : BoundNode
{
    public BoundNode Condition { get; } = condition;
    public BoundNode ThenStatement { get; } = thenStatement;
    public BoundNode? ElseStatement { get; } = elseStatement;

    public override TypeSymbol Type => TypeSymbol.Void;
}

public sealed class BoundWhileStatement(
    BoundNode condition,
    BoundNode actionStatement
) : BoundNode
{
    public BoundNode Condition { get; } = condition;
    public BoundNode ActionStatement { get; } = actionStatement;

    public override TypeSymbol Type => TypeSymbol.Void;
}

public sealed class BoundDoWhileStatement(
    BoundNode condition,
    BoundNode actionStatement
) : BoundNode
{
    public BoundNode Condition { get; } = condition;
    public BoundNode ActionStatement { get; } = actionStatement;

    public override TypeSymbol Type => TypeSymbol.Void;
}

public sealed class BoundForInStatement(
    BoundVariableDeclarationStatement variable,
    BoundNode enumerator,
    BoundNode actionStatement
) : BoundNode
{
    public BoundVariableDeclarationStatement Variable { get; } = variable;
    public BoundNode Enumerator { get; } = enumerator;
    public BoundNode ActionStatement { get; } = actionStatement;
    public override TypeSymbol Type => TypeSymbol.Void;
}