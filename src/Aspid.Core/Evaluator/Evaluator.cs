using Aspid.Core.Binding;

namespace Aspid.Core.Evaluator;

public class Evaluator
{
    private readonly Dictionary<string, object> _variables;

    public Evaluator(Dictionary<string, object> variables)
    {
        _variables = variables;
    }

    public object? Evaluate(BoundNode node)
    {
        return node switch
        {
            // Statements
            BoundVariableDeclarationStatement v => EvaluateVariableDeclaration(v),
            BoundAssignmentStatement a => EvaluateAssignment(a),

            // Expressions
            BoundLiteralExpression l => l.Value,
            BoundVariableExpression v => EvaluateVariableExpression(v),
            BoundConversionExpression conv => EvaluateConversion(conv),
            BoundUnaryExpression un => EvaluateUnaryExpression(un),
            BoundBinaryExpression b => EvaluateBinaryExpression(b),

            // Error Node
            BoundErrorNode => null,

            _ => throw new Exception($"Unexpected node {node.GetType().Name}")
        };
    }

    private object? EvaluateVariableDeclaration(BoundVariableDeclarationStatement node)
    {
        var value = node.Initializer != null ? Evaluate(node.Initializer) : null;

        if (value == null)
        {
            return null;
        }

        _variables[node.Variable.Name] = value;

        return value;
    }

    private object EvaluateAssignment(BoundAssignmentStatement node)
    {
        var value = Evaluate(node.Expression);
        _variables[node.Variable.Name] = value ?? throw new Exception($"Unexpected node {node.Type} in assignment.");
        return value;
    }

    private object EvaluateConversion(BoundConversionExpression node)
    {
        var value = Evaluate(node.Expression);

        if (node.Type == TypeSymbol.Bool)
        {
            return Convert.ToBoolean(value);
        }

        if (node.Type == TypeSymbol.Double)
        {
            return Convert.ToDouble(value);
        }

        throw new Exception($"Unexpected conversion to {node.Type}");
    }

    private object EvaluateVariableExpression(BoundVariableExpression node)
    {
        return _variables[node.Name];
    }

    private object EvaluateUnaryExpression(BoundUnaryExpression node)
    {
        object operand = node.Operand;
        if (node.Op.Kind != BoundUnaryOperatorKind.PreIncrement && node.Op.Kind != BoundUnaryOperatorKind.PreDecrement)
            operand = Evaluate(node.Operand) ??
                      throw new Exception($"Unexpected node {node.Operand.Type} in unary expression.");
        var operandVar = (BoundVariableExpression)node.Operand;

        return node.Op.Kind switch
        {
            BoundUnaryOperatorKind.Identity => operand,
            BoundUnaryOperatorKind.Negation => EvaluateNegation(node, operand),
            BoundUnaryOperatorKind.LogicalNegation => EvaluateLogicalNegation(node, operand),
            BoundUnaryOperatorKind.PostIncrement => ChangeValue(operandVar, 1, false),
            BoundUnaryOperatorKind.PostDecrement => ChangeValue(operandVar, -1, false),
            BoundUnaryOperatorKind.PreIncrement => ChangeValue(operandVar, 1, true),
            BoundUnaryOperatorKind.PreDecrement => ChangeValue(operandVar, -1, true),
            _ => throw new Exception($"Unexpected unary operator {node.Op.Kind}")
        };
    }

    private object EvaluateNegation(BoundUnaryExpression node, object operand)
    {
        if (node.Type == TypeSymbol.Double)
            return -(double)operand;

        if (node.Type == TypeSymbol.Int)
            return -(int)operand;

        throw new Exception($"Unexpected type {node.Type} for negation");
    }

    private object ChangeValue(BoundVariableExpression v, object amount, bool returnNew)
    {
        if (v.Type == TypeSymbol.Int)
        {
            var oldVal = (int)_variables[v.Name];
            var newVal = oldVal + (int)amount;
            _variables[v.Name] = newVal;
            return returnNew ? newVal : oldVal;
        }

        if (v.Type == TypeSymbol.Double)
        {
            var oldVal = (double)_variables[v.Name];
            var newVal = oldVal + (double)amount;
            _variables[v.Name] = newVal;
            return returnNew ? newVal : oldVal;
        }

        throw new InvalidOperationException();
    }

    private bool EvaluateLogicalNegation(BoundUnaryExpression node, object operand)
    {
        if (!node.Type.IsBoolean)
            throw new Exception($"Non-boolean type in unary operator {node.Op.Kind} is not supported");
        return !(bool)operand;
    }

    private object EvaluateBinaryExpression(BoundBinaryExpression node)
    {
        var left = Evaluate(node.Left) ??
                   throw new Exception($"Unexpected node {node.Left.Type} in binary expression.");
        var right = Evaluate(node.Right) ??
                    throw new Exception($"Unexpected node {node.Right.Type} in binary expression.");

        return node.Op.Kind switch
        {
            BoundBinaryOperatorKind.Addition => EvaluateAddition(node, left, right),
            BoundBinaryOperatorKind.Subtraction => EvaluateSubtraction(node, left, right),
            BoundBinaryOperatorKind.Multiplication => EvaluateMultiplication(node, left, right),
            BoundBinaryOperatorKind.Division => EvaluateDivision(node, left, right),
            BoundBinaryOperatorKind.Equals => EvaluateEquals(left, right),
            BoundBinaryOperatorKind.NotEquals => !EvaluateEquals(left, right),
            _ => throw new Exception($"Unexpected binary operator {node.Op.Kind}")
        };
    }

    private object EvaluateAddition(BoundBinaryExpression node, object left, object right)
    {
        if (node.Type == TypeSymbol.String)
        {
            return Convert.ToString(left) + Convert.ToString(right);
        }

        if (node.Type == TypeSymbol.Double)
        {
            return Convert.ToDouble(left) + Convert.ToDouble(right);
        }

        return Convert.ToInt32(left) + Convert.ToInt32(right);
    }

    private object EvaluateSubtraction(BoundBinaryExpression node, object left, object right)
    {
        if (node.Type == TypeSymbol.Double)
            return Convert.ToDouble(left) - Convert.ToDouble(right);

        return Convert.ToInt32(left) - Convert.ToInt32(right);
    }

    private object EvaluateMultiplication(BoundBinaryExpression node, object left, object right)
    {
        if (node.Type == TypeSymbol.Double)
            return Convert.ToDouble(left) * Convert.ToDouble(right);

        return Convert.ToInt32(left) * Convert.ToInt32(right);
    }

    private object EvaluateDivision(BoundBinaryExpression node, object left, object right)
    {
        if (node.Type == TypeSymbol.Double)
            return Convert.ToDouble(left) / Convert.ToDouble(right);

        return Convert.ToInt32(left) / Convert.ToInt32(right);
    }

    private bool EvaluateEquals(object left, object right)
    {
        if (IsNumeric(left) && IsNumeric(right))
        {
            return Math.Abs(Convert.ToDouble(left) - Convert.ToDouble(right)) < double.Epsilon;
        }

        return Equals(left, right);
    }

    private static bool IsNumeric(object obj) => obj is int or double;
}