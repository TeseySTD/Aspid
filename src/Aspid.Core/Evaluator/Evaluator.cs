using System.Globalization;
using Aspid.Core.Binding;

namespace Aspid.Core.Evaluator;

public class Evaluator
{
    private readonly Stack<ExecutionScope> _scopes = new();

    public Evaluator(Dictionary<string, object> globalVariables,
        Dictionary<FunctionSymbol, Func<object[], object?>> globalFunctions)
    {
        var scope = new ExecutionScope(globalVariables, globalFunctions);
        _scopes.Push(scope);
    }

    private object GetVariable(string name)
    {
        foreach (var scope in _scopes)
        {
            if (scope.TryGetVariable(name, out var value))
                return value;
        }

        throw new Exception($"Variable '{name}' not found in runtime.");
    }

    private void SetVariable(string name, object value)
    {
        foreach (var scope in _scopes)
        {
            if (scope.IsVariableDeclared(name))
            {
                scope.SetVariable(name, value);
                return;
            }
        }

        _scopes.Peek().SetVariable(name, value);
    }

    private void DeclareVariable(string name, object value)
    {
        if (_scopes.Peek().IsVariableDeclared(name))
            throw new Exception($"Variable '{name}' is already declared in runtime.");
        _scopes.Peek().SetVariable(name, value);
    }


    private Func<object[], object?> GetFunction(FunctionSymbol symbol)
    {
        foreach (var scope in _scopes)
        {
            if (scope.TryGetFunction(symbol, out var value))
                return value;
        }

        throw new Exception($"Function '{symbol.Name}' not found in runtime.");
    }

    private void SetFunction(FunctionSymbol symbol, Func<object[], object?> value)
    {
        foreach (var scope in _scopes)
        {
            if (scope.IsFunctionDeclared(symbol))
            {
                scope.SetFunction(symbol, value);
                return;
            }
        }

        _scopes.Peek().SetFunction(symbol, value);
    }

    private void DeclareFunction(FunctionSymbol symbol, Func<object[], object?> value)
    {
        if (_scopes.Peek().IsFunctionDeclared(symbol))
            throw new Exception($"Function '{symbol.Name}' is already declared in runtime.");
        _scopes.Peek().SetFunction(symbol, value);
    }


    public object? Evaluate(BoundNode node)
    {
        return node switch
        {
            // Statements
            BoundBlockStatement bl => EvaluateBlockStatement(bl),
            BoundVariableDeclarationStatement v => EvaluateVariableDeclarationStatement(v),
            BoundFunctionDeclarationStatement f => EvaluateFunctionDeclarationStatement(f),
            BoundAssignmentStatement a => EvaluateAssignment(a),
            BoundArrayAssignmentStatement a => EvaluateArrayAssignment(a),
            BoundIfStatement ifStatement => EvaluateIfStatement(ifStatement),
            BoundWhileStatement whileStatement => EvaluateWhileStatement(whileStatement),
            BoundDoWhileStatement whileStatement => EvaluateDoWhileStatement(whileStatement),
            BoundForInStatement forInStatement => EvaluateForInStatement(forInStatement),
            BoundReturnStatement returnStatement => EvaluateReturnStatement(returnStatement),

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
        _scopes.Push(new());

        foreach (var statement in node.Statements)
        {
            Evaluate(statement);
        }

        _scopes.Pop();

        return null;
    }

    private object? EvaluateVariableDeclarationStatement(BoundVariableDeclarationStatement node)
    {
        var value = node.Initializer != null ? Evaluate(node.Initializer) : 0; // Set empty value to declared-only var

        if (value == null)
            return null;

        DeclareVariable(node.Variable.Name, value);

        return value;
    }

    private object EvaluateFunctionDeclarationStatement(BoundFunctionDeclarationStatement node)
    {
        var value = CompileUserFunction(node);
        DeclareFunction(node.Function, value);
        return value;
    }

    private Func<object[], object?> CompileUserFunction(BoundFunctionDeclarationStatement node)
    {
        return (args) =>
        {
            var functionScope = new ExecutionScope();

            for (int i = 0; i < node.Function.Parameters.Count; i++)
            {
                functionScope.SetVariable(node.Function.Parameters[i].Name, args[i]);
            }

            _scopes.Push(functionScope);

            try
            {
                Evaluate(node.Action);
            }
            catch (ReturnException ex)
            {
                return ex.Value;
            }
            finally
            {
                _scopes.Pop();
            }

            return null;
        };
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
        } while (condition is true);

        return null;
    }

    private object? EvaluateForInStatement(BoundForInStatement node)
    {
        var collection = Evaluate(node.Enumerator);

        if (collection is List<object> list)
        {
            var variableName = node.Variable.Variable.Name;

            foreach (var item in list)
            {
                _scopes.Peek().SetVariable(variableName, item);

                Evaluate(node.ActionStatement);
            }

            return null;
        }

        throw new Exception(
            $"Unexpected type '{collection?.GetType().Name}' in for-loop. Expected Array (List<object>).");
    }

    private object? EvaluateReturnStatement(BoundReturnStatement node)
    {
        var exceptionValue = node.Expression == null ? null : Evaluate(node.Expression);
        throw new ReturnException(exceptionValue);
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

        var implementation = GetFunction(node.Function);
        return implementation(args!);
    }

    private object EvaluateUnaryExpression(BoundUnaryExpression node)
    {
        object operand = node.Operand;
        if (node.Op.Kind != BoundUnaryOperatorKind.PreIncrement &&
            node.Op.Kind != BoundUnaryOperatorKind.PreDecrement &&
            node.Op.Kind != BoundUnaryOperatorKind.PostIncrement &&
            node.Op.Kind != BoundUnaryOperatorKind.PostDecrement)
        {
            operand = Evaluate(node.Operand) ??
                      throw new Exception($"Unexpected node {node.Operand.Type} in unary expression.");
        }

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
            throw new Exception($"Operator '-' cannot be applied to operand of type '{operand.GetType()}'");
        }

        throw new Exception($"Unexpected type {node.Type} for negation");
    }

    private object ChangeValue(BoundVariableExpression v, object amount, bool returnNew)
    {
        if (v.Type == TypeSymbol.Int || v.Type == TypeSymbol.Double || v.Type == TypeSymbol.Any)
        {
            var value = GetVariable(v.Name);

            if (value is int oldInt)
            {
                var change = (int)amount;
                var newVal = oldInt + change;
                SetVariable(v.Name, newVal);
                return returnNew ? newVal : oldInt;
            }

            if (value is double oldDouble)
            {
                var change = (int)amount;

                var newVal = oldDouble + change;
                SetVariable(v.Name, newVal);
                return returnNew ? newVal : oldDouble;
            }

            throw new Exception($"Operator '++' or '--' cannot be applied to operand of type '{value.GetType().Name}'");
        }

        throw new InvalidOperationException($"Cannot increment variable of type {v.Type}");
    }


    private bool EvaluateLogicalNegation(BoundUnaryExpression node, object operand)
    {
        if (node.Type == TypeSymbol.Any)
        {
            if (operand is bool b) return !b;
            throw new Exception($"Operator '!' cannot be applied to operand of type '{operand.GetType()}'");
        }

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
            BoundBinaryOperatorKind.Greater => EvaluateNumericComparison(left, right, (l, r) => l > r),
            BoundBinaryOperatorKind.GreaterOrEquals => EvaluateNumericComparison(left, right, (l, r) => l >= r),
            BoundBinaryOperatorKind.Less => EvaluateNumericComparison(left, right, (l, r) => l < r),
            BoundBinaryOperatorKind.LessOrEquals => EvaluateNumericComparison(left, right, (l, r) => l <= r),
            BoundBinaryOperatorKind.LogicalAnd => EvaluateLogicalOperator(left, right, (l,r) => l && r),
            BoundBinaryOperatorKind.LogicalOr => EvaluateLogicalOperator(left, right, (l, r) => l || r),
            _ => throw new Exception($"Unexpected binary operator {node.Op.Kind}")
        };
    }

    private object EvaluateAddition(BoundBinaryExpression node, object left, object right)
    {
        if (node.Type == TypeSymbol.String)
            return Convert.ToString(left, CultureInfo.InvariantCulture) +
                   Convert.ToString(right, CultureInfo.InvariantCulture);
        if (node.Type == TypeSymbol.Double)
            return Convert.ToDouble(left, CultureInfo.InvariantCulture) +
                   Convert.ToDouble(right, CultureInfo.InvariantCulture);
        if (node.Type == TypeSymbol.Int)
            return Convert.ToInt32(left, CultureInfo.InvariantCulture) +
                   Convert.ToInt32(right, CultureInfo.InvariantCulture);

        if (node.Type == TypeSymbol.Any)
        {
            if (left is string || right is string)
                return Convert.ToString(left, CultureInfo.InvariantCulture) +
                       Convert.ToString(right, CultureInfo.InvariantCulture);

            if (left is double || right is double)
                return Convert.ToDouble(left, CultureInfo.InvariantCulture) +
                       Convert.ToDouble(right, CultureInfo.InvariantCulture);

            return Convert.ToInt32(left, CultureInfo.InvariantCulture) +
                   Convert.ToInt32(right, CultureInfo.InvariantCulture);
        }

        throw new Exception($"Unexpected types for addition: {left.GetType()} and {right.GetType()}");
    }


    private object EvaluateSubtraction(BoundBinaryExpression node, object left, object right)
    {
        if (node.Type == TypeSymbol.Double)
            return Convert.ToDouble(left, CultureInfo.InvariantCulture) -
                   Convert.ToDouble(right, CultureInfo.InvariantCulture);
        if (node.Type == TypeSymbol.Int)
            return Convert.ToInt32(left, CultureInfo.InvariantCulture) -
                   Convert.ToInt32(right, CultureInfo.InvariantCulture);
        if (node.Type == TypeSymbol.Any)
        {
            if (left is double || right is double)
                return Convert.ToDouble(left, CultureInfo.InvariantCulture) -
                       Convert.ToDouble(right, CultureInfo.InvariantCulture);

            return Convert.ToInt32(left, CultureInfo.InvariantCulture) -
                   Convert.ToInt32(right, CultureInfo.InvariantCulture);
        }

        throw new Exception($"Unexpected types for subtraction: {left.GetType()} and {right.GetType()}");
    }

    private object EvaluateMultiplication(BoundBinaryExpression node, object left, object right)
    {
        if (node.Type == TypeSymbol.Double)
            return Convert.ToDouble(left, CultureInfo.InvariantCulture) *
                   Convert.ToDouble(right, CultureInfo.InvariantCulture);
        if (node.Type == TypeSymbol.Int)
            return Convert.ToInt32(left, CultureInfo.InvariantCulture) *
                   Convert.ToInt32(right, CultureInfo.InvariantCulture);
        if (node.Type == TypeSymbol.Any)
        {
            if (left is double || right is double)
                return Convert.ToDouble(left, CultureInfo.InvariantCulture) *
                       Convert.ToDouble(right, CultureInfo.InvariantCulture);

            return Convert.ToInt32(left, CultureInfo.InvariantCulture) *
                   Convert.ToInt32(right, CultureInfo.InvariantCulture);
        }

        throw new Exception($"Unexpected types for multiplication: {left.GetType()} and {right.GetType()}");
    }

    private object EvaluateDivision(BoundBinaryExpression node, object left, object right)
    {
        if (node.Type == TypeSymbol.Double)
            return Convert.ToDouble(left, CultureInfo.InvariantCulture) /
                   Convert.ToDouble(right, CultureInfo.InvariantCulture);
        if (node.Type == TypeSymbol.Int)
            return Convert.ToInt32(left, CultureInfo.InvariantCulture) /
                   Convert.ToInt32(right, CultureInfo.InvariantCulture);
        if (node.Type == TypeSymbol.Any)
        {
            if (left is double || right is double)
                return Convert.ToDouble(left, CultureInfo.InvariantCulture) /
                       Convert.ToDouble(right, CultureInfo.InvariantCulture);

            return Convert.ToInt32(left, CultureInfo.InvariantCulture) /
                   Convert.ToInt32(right, CultureInfo.InvariantCulture);
        }

        throw new Exception($"Unexpected types for division: {left.GetType()} and {right.GetType()}");
    }

    private bool EvaluateEquals(object left, object right)
    {
        if (IsNumeric(left) && IsNumeric(right))
        {
            return Math.Abs(Convert.ToDouble(left, CultureInfo.InvariantCulture) -
                            Convert.ToDouble(right, CultureInfo.InvariantCulture)) < double.Epsilon;
        }

        return Equals(left, right);
    }

    private bool EvaluateNumericComparison(object left, object right, Func<double, double, bool> operation)
    {
        if (IsNumeric(left) && IsNumeric(right))
        {
            var l = Convert.ToDouble(left, CultureInfo.InvariantCulture);
            var r = Convert.ToDouble(right, CultureInfo.InvariantCulture);
            return operation(l, r);
        }

        throw new Exception($"Cannot compare types: {left.GetType()} and {right.GetType()}");
    }

    private bool EvaluateLogicalOperator(object left, object right, Func<bool, bool, bool> operation)
    {
        if (( left is not bool && !IsNumeric(left)) || ( right is not bool  && !IsNumeric(right)))
        {
            throw new Exception($"Cannot cast to bool types: {left.GetType()} and {right.GetType()}");
        }

        var l = Convert.ToBoolean(left, CultureInfo.InvariantCulture);
        var r = Convert.ToBoolean(right, CultureInfo.InvariantCulture);
        return operation(l, r);
    }

    private static bool IsNumeric(object obj) => obj is int or double;
}