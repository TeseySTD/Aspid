using System.Globalization;
using Aspid.Core.Binding;

namespace Aspid.Core.Evaluator;

public class Evaluator
{
    private readonly Stack<Dictionary<string, object>> _scopes = new();

    public Evaluator(Dictionary<string, object> globalVariables)
    {
        _scopes.Push(globalVariables);
    }

    private object GetVariable(string name)
    {
        foreach (var scope in _scopes)
        {
            if (scope.TryGetValue(name, out var value))
                return value;
        }

        throw new Exception($"Variable '{name}' not found in runtime.");
    }

    private void SetVariable(string name, object value)
    {
        foreach (var scope in _scopes)
        {
            if (scope.ContainsKey(name))
            {
                scope[name] = value;
                return;
            }
        }

        _scopes.Peek()[name] = value;
    }

    private void DeclareVariable(string name, object value)
    {
        _scopes.Peek()[name] = value;
    }

    public object? Evaluate(BoundNode node)
    {
        return node switch
        {
            // Statements
            BoundBlockStatement bl => EvaluateBlockStatement(bl),
            BoundVariableDeclarationStatement v => EvaluateVariableDeclaration(v),
            BoundAssignmentStatement a => EvaluateAssignment(a),
            BoundArrayAssignmentStatement a => EvaluateArrayAssignment(a),
            BoundIfStatement ifStatement => EvaluateIfStatement(ifStatement),
            BoundWhileStatement whileStatement => EvaluateWhileStatement(whileStatement),
            BoundDoWhileStatement whileStatement => EvaluateDoWhileStatement(whileStatement),

            // Expressions
            BoundLiteralExpression l => l.Value,
            BoundVariableExpression v => EvaluateVariableExpression(v),
            BoundConversionExpression conv => EvaluateConversion(conv),
            BoundUnaryExpression un => EvaluateUnaryExpression(un),
            BoundBinaryExpression b => EvaluateBinaryExpression(b),
            BoundCallExpression call => EvaluateCallExpression(call),
            BoundArrayExpression array => EvaluateArrayExpression(array),
            BoundArrayAccessExpression arrayAccess => EvaluateArrayAccessExpression(arrayAccess),

            // Error Node
            BoundErrorNode => null,

            _ => throw new Exception($"Unexpected node {node.GetType().Name}")
        };
    }

    private object? EvaluateBlockStatement(BoundBlockStatement node)
    {
        _scopes.Push(new Dictionary<string, object>());

        foreach (var statement in node.Statements)
        {
            Evaluate(statement);
        }

        _scopes.Pop();

        return null;
    }

    private object? EvaluateVariableDeclaration(BoundVariableDeclarationStatement node)
    {
        var value = node.Initializer != null ? Evaluate(node.Initializer) : 0; // Set empty value to declared-only var

        if (value == null)
            return null;

        DeclareVariable(node.Variable.Name, value);

        return value;
    }

    private object EvaluateAssignment(BoundAssignmentStatement node)
    {
        var value = Evaluate(node.Expression);
        if (value == null)
            throw new Exception($"Unexpected node {node.Type} in assignment.");
        SetVariable(node.Variable.Name, value);
        return value;
    }

    private object EvaluateArrayAssignment(BoundArrayAssignmentStatement node)
    {
        var arrayObject = Evaluate(node.ArrayAccess.Array);
        var indexObject = Evaluate(node.ArrayAccess.Index);
        var value = Evaluate(node.Expression);

        if (arrayObject is List<object> list)
        {
            var index = Convert.ToInt32(indexObject);

            if (index < 0) index = list.Count + index;

            if (index < 0 || index >= list.Count)
                throw new IndexOutOfRangeException($"Index {index} is out of bounds.");

            list[index] = value;
            return value;
        }

        throw new Exception($"Cannot assign to non-array object of type {arrayObject?.GetType()}.");
    }


    private object? EvaluateIfStatement(BoundIfStatement node)
    {
        var condition = (bool?)(Evaluate(node.Condition) ?? null);

        if (condition is true)
        {
            return Evaluate(node.ThenStatement);
        }

        if (node.ElseStatement != null)
        {
            return Evaluate(node.ElseStatement);
        }

        return null;
    }

    private object? EvaluateWhileStatement(BoundWhileStatement node)
    {
        var condition = (bool?)(Evaluate(node.Condition) ?? null);
        while (condition is true)
        {
            Evaluate(node.ActionStatement);
            condition = (bool?)(Evaluate(node.Condition) ?? null);
        }

        return null;
    }

    private object? EvaluateDoWhileStatement(BoundDoWhileStatement node)
    {
        bool? condition;
        do
        {
            Evaluate(node.ActionStatement);
            condition = (bool?)(Evaluate(node.Condition) ?? null);
        } 
        while (condition is true);
        return null;
    }

    private object EvaluateConversion(BoundConversionExpression node)
    {
        var value = Evaluate(node.Expression);

        if (node.Type == TypeSymbol.Any && value != null)
            return value;
        if (node.Type == TypeSymbol.Bool)
            return Convert.ToBoolean(value, CultureInfo.InvariantCulture);
        if (node.Type == TypeSymbol.Double)
            return Convert.ToDouble(value, CultureInfo.InvariantCulture);
        if (node.Type == TypeSymbol.Int)
        {
            if (value is string s)
            {
                s = s.Trim();
                int hexIndex = s.IndexOf("0x", StringComparison.OrdinalIgnoreCase);
                if (hexIndex >= 0) // Remove 0x if it is hex number
                {
                    s = s.Remove(hexIndex, 2);

                    return Convert.ToInt32(s, 16);
                }
            }

            return Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }


        throw new Exception($"Unexpected conversion to {node.Type}");
    }

    private object EvaluateVariableExpression(BoundVariableExpression node)
    {
        return GetVariable(node.Name);
    }

    private object EvaluateArrayExpression(BoundArrayExpression node)
    {
        var list = new List<object>();
        if (node.Elements != null)
            foreach (var element in node.Elements)
            {
                var result = Evaluate(element);
                if (result is null) throw new Exception($"Unexpected element in array expression: {element.Type}");
                list.Add(result);
            }

        return list;
    }

    private object EvaluateArrayAccessExpression(BoundArrayAccessExpression node)
    {
        var arrayObj = Evaluate(node.Array);
        var indexObj = Evaluate(node.Index);

        if (arrayObj is List<object> list)
        {
            var index = Convert.ToInt32(indexObj);

            // Negative indexes
            if (index < 0) index = list.Count + index;

            if (index < 0 || index >= list.Count)
                throw new IndexOutOfRangeException($"Index {index} is out of range.");

            return list[index];
        }

        throw new Exception($"Cannot index non-array object of type {arrayObj?.GetType()}.");
    }

    private object? EvaluateCallExpression(BoundCallExpression node)
    {
        var args = node.Arguments.Select(Evaluate).ToArray();

        if (BuiltInFunctions.Implementations.TryGetValue(node.Function, out var implementation))
        {
            return implementation(args!);
        }

        throw new Exception($"Function {node.Function.Name} is not implemented.");
    }

    private object EvaluateUnaryExpression(BoundUnaryExpression node)
    {
        object operand = node.Operand;
        if (node.Op.Kind != BoundUnaryOperatorKind.PreIncrement && node.Op.Kind != BoundUnaryOperatorKind.PreDecrement)
            operand = Evaluate(node.Operand) ??
                      throw new Exception($"Unexpected node {node.Operand.Type} in unary expression.");

        return node.Op.Kind switch
        {
            BoundUnaryOperatorKind.Identity => operand,
            BoundUnaryOperatorKind.Negation => EvaluateNegation(node, operand),
            BoundUnaryOperatorKind.LogicalNegation => EvaluateLogicalNegation(node, operand),
            BoundUnaryOperatorKind.PostIncrement => ChangeValue((BoundVariableExpression)node.Operand, 1, false),
            BoundUnaryOperatorKind.PostDecrement => ChangeValue((BoundVariableExpression)node.Operand, -1, false),
            BoundUnaryOperatorKind.PreIncrement => ChangeValue((BoundVariableExpression)node.Operand, 1, true),
            BoundUnaryOperatorKind.PreDecrement => ChangeValue((BoundVariableExpression)node.Operand, -1, true),
            _ => throw new Exception($"Unexpected unary operator {node.Op.Kind}")
        };
    }

    private object EvaluateNegation(BoundUnaryExpression node, object operand)
    {
        if (node.Type == TypeSymbol.Double)
            return -(double)operand;

        if (node.Type == TypeSymbol.Int)
            return -(int)operand;

        if (node.Type == TypeSymbol.Any)
        {
            if (operand is double d)
                return -d;
            if (operand is int i)
                return -i;
            throw new Exception($"Unexpected - unary operator with any type");
        }

        throw new Exception($"Unexpected type {node.Type} for negation");
    }

    private object ChangeValue(BoundVariableExpression v, object amount, bool returnNew)
    {
        if (v.Type == TypeSymbol.Int)
        {
            var oldVal = (int)GetVariable(v.Name);
            var newVal = oldVal + (int)amount;
            SetVariable(v.Name, newVal);
            return returnNew ? newVal : oldVal;
        }

        if (v.Type == TypeSymbol.Double)
        {
            var oldVal = (double)GetVariable(v.Name);
            var newVal = oldVal + (double)amount;
            SetVariable(v.Name, newVal);
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
        if (node.Type == TypeSymbol.String) return Convert.ToString(left) + Convert.ToString(right);
        if (node.Type == TypeSymbol.Double) return Convert.ToDouble(left) + Convert.ToDouble(right);
        if (node.Type == TypeSymbol.Int) return Convert.ToInt32(left) + Convert.ToInt32(right);

        if (node.Type == TypeSymbol.Any)
        {
            if (left is string || right is string)
                return Convert.ToString(left) + Convert.ToString(right);

            if (left is double || right is double)
                return Convert.ToDouble(left) + Convert.ToDouble(right);

            return Convert.ToInt32(left) + Convert.ToInt32(right);
        }

        throw new Exception($"Unexpected types for addition: {left.GetType()} and {right.GetType()}");
    }


    private object EvaluateSubtraction(BoundBinaryExpression node, object left, object right)
    {
        if (node.Type == TypeSymbol.Double) return Convert.ToDouble(left) - Convert.ToDouble(right);
        if (node.Type == TypeSymbol.Int) return Convert.ToInt32(left) - Convert.ToInt32(right);
        if (node.Type == TypeSymbol.Any)
        {
            if (left is double || right is double)
                return Convert.ToDouble(left) - Convert.ToDouble(right);

            return Convert.ToInt32(left) - Convert.ToInt32(right);
        }

        throw new Exception($"Unexpected types for subtraction: {left.GetType()} and {right.GetType()}");
    }

    private object EvaluateMultiplication(BoundBinaryExpression node, object left, object right)
    {
        if (node.Type == TypeSymbol.Double) return Convert.ToDouble(left) * Convert.ToDouble(right);
        if (node.Type == TypeSymbol.Int) return Convert.ToInt32(left) * Convert.ToInt32(right);
        if (node.Type == TypeSymbol.Any)
        {
            if (left is double || right is double)
                return Convert.ToDouble(left) * Convert.ToDouble(right);

            return Convert.ToInt32(left) * Convert.ToInt32(right);
        }

        throw new Exception($"Unexpected types for multiplication: {left.GetType()} and {right.GetType()}");
    }

    private object EvaluateDivision(BoundBinaryExpression node, object left, object right)
    {
        if (node.Type == TypeSymbol.Double) return Convert.ToDouble(left) / Convert.ToDouble(right);
        if (node.Type == TypeSymbol.Int) return Convert.ToInt32(left) / Convert.ToInt32(right);
        if (node.Type == TypeSymbol.Any)
        {
            if (left is double || right is double)
                return Convert.ToDouble(left) / Convert.ToDouble(right);

            return Convert.ToInt32(left) / Convert.ToInt32(right);
        }

        throw new Exception($"Unexpected types for division: {left.GetType()} and {right.GetType()}");
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