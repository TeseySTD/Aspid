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
            BoundBinaryExpression b => EvaluateBinaryExpression(b),
            
            // Error Node
            BoundErrorNode => null,

            _ => throw new Exception($"Unexpected node {node.GetType().Name}")
        };
    }

    private object? EvaluateVariableDeclaration(BoundVariableDeclarationStatement node)
    {
        var value = node.Initializer != null ? Evaluate(node.Initializer) : null;
        
        _variables[node.Variable.Name] = value!;
        
        return value;
    }

    private object EvaluateAssignment(BoundAssignmentStatement node)
    {
        var value = Evaluate(node.Expression);
        _variables[node.Variable.Name] = value!;
        return value!;
    }

    private object EvaluateVariableExpression(BoundVariableExpression node)
    {
        return _variables[node.Name];
    }

    private object EvaluateBinaryExpression(BoundBinaryExpression node)
    {
        var left = Evaluate(node.Left)!;
        var right = Evaluate(node.Right)!;

        return node.Op.Kind switch
        {
            BoundBinaryOperatorKind.Addition => EvaluateAddition(node, left, right),
            BoundBinaryOperatorKind.Subtraction => EvaluateSubtraction(node, left, right),
            BoundBinaryOperatorKind.Multiplication => EvaluateMultiplication(node, left, right),
            BoundBinaryOperatorKind.Division => EvaluateDivision(node, left, right),
            BoundBinaryOperatorKind.Equals => EvaluateEquals(left, right),
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

    private object EvaluateEquals(object left, object right)
    {
        if (IsNumeric(left) && IsNumeric(right))
        {
            return Math.Abs(Convert.ToDouble(left) - Convert.ToDouble(right)) < double.Epsilon;
        }

        return Equals(left, right);
    }

    private static bool IsNumeric(object obj) => obj is int or double;
}
