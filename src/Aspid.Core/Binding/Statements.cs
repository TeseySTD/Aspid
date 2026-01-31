namespace Aspid.Core.Binding;

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