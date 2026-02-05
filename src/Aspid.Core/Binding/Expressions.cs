namespace Aspid.Core.Binding;

public sealed class BoundUnaryExpression(BoundUnaryOperator op, BoundNode operand) : BoundNode
{
    public BoundUnaryOperator Op { get; } = op;
    public BoundNode Operand { get; } = operand;
    public override TypeSymbol Type => Op.ResultType;
}

public sealed class BoundBinaryExpression(BoundNode left, BoundBinaryOperator op, BoundNode right) : BoundNode
{
    public BoundNode Left { get; } = left;
    public BoundBinaryOperator Op { get; } = op;
    public BoundNode Right { get; } = right;
    public override TypeSymbol Type { get; } = op.ResultType;
}

public sealed class BoundLiteralExpression(object value) : BoundNode
{
    public object Value { get; } = value;

    public override TypeSymbol Type => Value switch
    {
        int => TypeSymbol.Int,
        double => TypeSymbol.Double,
        bool => TypeSymbol.Bool,
        string => TypeSymbol.String,
        null => TypeSymbol.Void,
        _ => throw new Exception($"Unexpected literal type: {Value.GetType()}")
    };
}

public sealed class BoundConversionExpression(TypeSymbol type, BoundNode expression) : BoundNode
{
    public override TypeSymbol Type { get; } = type;
    public BoundNode Expression { get; } = expression;
}

public sealed class BoundVariableExpression(string name, TypeSymbol type) : BoundNode
{
    public string Name { get; } = name;
    public override TypeSymbol Type { get; } = type;
}

public sealed class BoundArrayExpression(List<BoundNode>? elements, TypeSymbol type) : BoundNode
{
    public List<BoundNode>? Elements { get; } = elements;
    public override TypeSymbol Type { get; } = type;
}

public sealed class BoundArrayAccessExpression(BoundNode array, BoundNode index, TypeSymbol type) : BoundNode
{
    public BoundNode Index { get; } = index;
    public BoundNode Array { get; } = array;
    public override TypeSymbol Type { get; } = type;
}

public sealed class BoundCallExpression(FunctionSymbol function, List<BoundNode> arguments) : BoundNode
{
    public FunctionSymbol Function { get; } = function;
    public List<BoundNode> Arguments { get; } = arguments;
    public override TypeSymbol Type => Function.Type;
}