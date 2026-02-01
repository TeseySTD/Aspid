using System.Globalization;
using Aspid.Core.Syntax;

namespace Aspid.Core.Binding;

public class Binder
{
    private BoundScope _scope;
    public List<string> Diagnostics { get; } = new();

    public Binder(BoundScope? parent)
    {
        _scope = new BoundScope(parent);
    }

    public BoundNode Bind(Statement statement)
    {
        return statement switch
        {
            BlockStatement block => BindBlockStatement(block),
            VariableDeclarationStatement declaration => BindVariableDeclarationStatement(declaration),
            AssignmentStatement assignment => BindAssignmentStatement(assignment),
            IfStatement ifStatement => BindIfStatement(ifStatement),
            ExpressionStatement es => BindExpression(es.Expression),
            _ => throw new NotImplementedException($"Binding for {statement.Kind} not implemented yet.")
        };
    }

    private BoundNode BindBlockStatement(BlockStatement syntax)
    {
        var statements = new List<BoundNode>();

        _scope = new BoundScope(_scope);

        foreach (var statement in syntax.Statements)
            statements.Add(Bind(statement));

        if (_scope.Parent != null)
            _scope = _scope.Parent;

        return new BoundBlockStatement(statements);
    }

    private BoundNode BindVariableDeclarationStatement(VariableDeclarationStatement declaration)
    {
        var name = declaration.Variable.Text;
        var initializer = declaration.Initializer is not null ? BindExpression(declaration.Initializer) : null;
        var type = TypeSymbol.Parse(declaration.TypeIdentifier.Text);
        if (type is null) // Remove that check when add custom types
        {
            var incorrectTypeError =
                $"Variable declaration {declaration.Variable.Text} has no correct type {declaration.TypeIdentifier.Text}.";
            Diagnostics.Add(incorrectTypeError);
            return new BoundErrorNode(incorrectTypeError);
        }

        var variable = new BoundVariableExpression(name, type);

        if (initializer != null &&
            initializer.Type != TypeSymbol.Any && type != TypeSymbol.Any &&
            initializer.Type != type)
        {
            var assignWrongTypeError =
                $"Variable '{name}' with type {type.Name} cannot be assigned to type '{initializer.Type.Name}'.";
            Diagnostics.Add(assignWrongTypeError);
            return new BoundErrorNode(assignWrongTypeError);
        }

        if (_scope.TryDeclare(name, type))
            return new BoundVariableDeclarationStatement(variable, initializer);

        var alreadyDeclaredError = $"Cannot declare variable with name {name} because it is already declared.";
        Diagnostics.Add(alreadyDeclaredError);
        return new BoundErrorNode(alreadyDeclaredError);
    }

    private BoundNode BindAssignmentStatement(AssignmentStatement assignment)
    {
        var name = assignment.IdentifierToken.Text;
        var expression = BindExpression(assignment.Expression);
        if (_scope.TryLookup(name, out var existingType))
        {
            if (existingType != TypeSymbol.Any)
            {
                var convertedExpression = BindConversion(expression, existingType);

                if (existingType != convertedExpression.Type)
                {
                    var errText =
                        $"Cannot assign type '{convertedExpression.Type}' to variable '{name}' of type '{existingType}'.";
                    Diagnostics.Add(errText);
                    return new BoundErrorNode(errText);
                }

                expression = convertedExpression;
            }
        }
        else
        {
            _scope.TryDeclare(name, TypeSymbol.Any);
        }

        var variable = new BoundVariableExpression(name, expression.Type);
        return new BoundAssignmentStatement(variable, expression);
    }

    private BoundNode BindIfStatement(IfStatement statement)
    {
        var condition = BindExpression(statement.ConditionExpression);

        if (condition.Type != TypeSymbol.Bool)
        {
            var errText =
                $"If statement has not a boolean condition at {statement.IfKeyword.End}, {statement.Colon.Start}.";
            Diagnostics.Add(errText);
            return new BoundErrorNode(errText);
        }

        var thenStatement = Bind(statement.ThenStatement);

        BoundNode? elseStatement = statement.ElseStatement == null
            ? null
            : Bind(statement.ElseStatement);

        return new BoundIfStatement(condition, thenStatement, elseStatement);
    }


    private BoundNode BindExpression(Expression syntax)
    {
        return syntax switch
        {
            ParenthesizedExpression p => BindExpression(p.Expression),
            BooleanExpression b => new BoundLiteralExpression(b.Value),
            NumberExpression n => BindNumberExpression(n),
            StringExpression str => BindStringExpression(str),
            UnaryExpression un => BindUnaryExpression(un),
            PostfixUnaryExpression pstfun => BindPostfixUnaryExpression(pstfun),
            BinaryExpression b => BindBinaryExpression(b),
            CallExpression call => BindCallExpression(call),
            VariableExpression v => BindVariableExpression(v),
            _ => throw new Exception($"Unexpected syntax {syntax.Kind}")
        };
    }

    private BoundNode BindCallExpression(CallExpression syntax)
    {
        if (syntax.Function is VariableExpression vExpr)
        {
            var name = vExpr.VariableName.Text;
            var builtin = BuiltInFunctions.GetAll().FirstOrDefault(f => f.Name == name);
            if (builtin != null)
            {
                var boundArgs = syntax.Arguments.Select(BindExpression).ToList();
                if (builtin.Parameters.Any(p =>
                        p.Type != TypeSymbol.Any &&
                        boundArgs[builtin.Parameters.IndexOf(p)].Type != TypeSymbol.Any &&
                        p.Type != boundArgs[builtin.Parameters.IndexOf(p)].Type
                    ))
                {
                    var errText =
                        $"Invalid args for function {name}: {string.Join(", ", boundArgs.Select(p => p.ToString()))}";
                    Diagnostics.Add(errText);
                    return new BoundErrorNode(errText);
                }

                return new BoundCallExpression(builtin, boundArgs);
            }

            {
                Diagnostics.Add($"Function {vExpr.VariableName} is not supported");
            }
        }

        Diagnostics.Add("Non-declared functions are not supported for now");
        return new BoundErrorNode("");
    }

    private BoundNode BindNumberExpression(NumberExpression syntax)
    {
        object value;
        var text = syntax.NumberToken.Text;
        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            var hexText = text.Substring(2);

            if (int.TryParse(hexText, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int hexValue))
            {
                return new BoundLiteralExpression(hexValue);
            }

            var errText = $"Invalid hex number: {text}";
            Diagnostics.Add(errText);
            return new BoundErrorNode(errText);
        }

        if (int.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out int intValue))
            value = intValue;
        else if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out double doubleValue))
            value = doubleValue;
        else
        {
            var errText =
                $"Unable to parse number in number expression: {syntax.NumberToken.Start}, {syntax.NumberToken.End}";
            Diagnostics.Add(errText);
            return new BoundErrorNode(errText);
        }

        return new BoundLiteralExpression(value);
    }

    private static BoundNode BindStringExpression(StringExpression syntax)
    {
        var text = syntax.StringToken.Text;
        return new BoundLiteralExpression(text);
    }

    private BoundNode BindVariableExpression(VariableExpression syntax)
    {
        var name = syntax.VariableName.Text;
        if (!_scope.TryLookup(name, out var type))
        {
            var errText = $"Variable '{name}' does not exist.";
            Diagnostics.Add(errText);
            return new BoundErrorNode(errText);
        }

        return new BoundVariableExpression(name, type);
    }

    private BoundNode BindUnaryExpression(UnaryExpression syntax)
    {
        var boundOperand = BindExpression(syntax.Operand);

        if (boundOperand.Type == TypeSymbol.Error)
            return new BoundErrorNode(""); // Replace recursion

        var operatorKind = BoundUnaryOperator.GetPrefixOperatorKind(syntax.OperatorToken.Kind);
        if (operatorKind is null)
        {
            var errText = $"Unary operator '{syntax.OperatorToken.Text}' is not supported.";
            Diagnostics.Add(errText);
            return new BoundErrorNode(errText);
        }

        var boundOperator = BoundUnaryOperator.Bind(operatorKind.Value, boundOperand.Type);
        if (boundOperator == null)
        {
            var message =
                $"Unary operator '{syntax.OperatorToken.Text}' is not defined for type '{boundOperand.Type}'.";
            Diagnostics.Add(message);
            return new BoundErrorNode(message);
        }

        return new BoundUnaryExpression(boundOperator, boundOperand);
    }

    private BoundNode BindPostfixUnaryExpression(PostfixUnaryExpression syntax)
    {
        var boundOperand = BindExpression(syntax.Operand);

        if (boundOperand.Type == TypeSymbol.Error)
            return new BoundErrorNode(""); // Replace recursion

        var operatorKind = BoundUnaryOperator.GetPostfixOperatorKind(syntax.OperatorToken.Kind);
        if (operatorKind is null)
        {
            var errText = $"Postfix unary operator '{syntax.OperatorToken.Text}' is not supported.";
            Diagnostics.Add(errText);
            return new BoundErrorNode(errText);
        }

        var boundOperator = BoundUnaryOperator.Bind(operatorKind.Value, boundOperand.Type);
        if (boundOperator == null)
        {
            var message =
                $"Postfix unary operator '{syntax.OperatorToken.Text}' is not defined for type '{boundOperand.Type}'.";
            Diagnostics.Add(message);
            return new BoundErrorNode(message);
        }

        return new BoundUnaryExpression(boundOperator, boundOperand);
    }

    private BoundNode BindBinaryExpression(BinaryExpression syntax)
    {
        var boundLeft = BindExpression(syntax.Left);
        var boundRight = BindExpression(syntax.Right);

        BoundBinaryOperatorKind kind = BoundBinaryOperator.GetOperatorKind(syntax.OperatorToken.Kind)
                                       ?? throw new Exception("Unknown op");

        var op = BoundBinaryOperator.Bind(kind, boundLeft.Type, boundRight.Type);

        if (op == null)
        {
            var errText =
                $"Binary operator '{syntax.OperatorToken.Text}' is not defined for types '{boundLeft.Type}' and '{boundRight.Type}'.";
            Diagnostics.Add(errText);
            return new BoundErrorNode(errText);
        }

        return new BoundBinaryExpression(boundLeft, op, boundRight);
    }

    private BoundNode BindConversion(BoundNode expression, TypeSymbol targetType)
    {
        if (expression.Type == targetType)
            return expression;

        if (expression.Type == TypeSymbol.Any)
            return new BoundConversionExpression(targetType, expression);

        if (targetType == TypeSymbol.Any)
            return new BoundConversionExpression(targetType, expression);

        if (expression.Type.IsNumeric && targetType == TypeSymbol.Bool)
        {
            return new BoundConversionExpression(TypeSymbol.Bool, expression);
        }

        if (expression.Type == TypeSymbol.Int && targetType == TypeSymbol.Double)
        {
            return new BoundConversionExpression(TypeSymbol.Double, expression);
        }

        return expression;
    }

    public static void PrettyPrint(
        BoundNode node,
        string indent = "",
        bool isFirst = false,
        bool isLast = true,
        string optionalLabel = "")
    {
        var marker = isLast ? "└── " : "├── ";

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write(indent);
        if (!isFirst) Console.Write(marker);
        Console.ResetColor();

        PrintNodeInfo(node, optionalLabel);

        indent += isLast ? "    " : "│   ";

        switch (node)
        {
            case BoundIfStatement ifStatement:
                PrettyPrint(ifStatement.Condition, indent, isFirst: false, isLast: false, optionalLabel: "Condition: ");
                var isElseStatement = ifStatement.ElseStatement != null;
                PrettyPrint(ifStatement.ThenStatement, indent, isLast: !isElseStatement,
                    optionalLabel: "Then Statement: ");
                if (isElseStatement)
                    PrettyPrint(ifStatement.ElseStatement!, indent, isLast: true, optionalLabel: "Else Statement: ");
                break;

            case BoundBlockStatement blockStatement:
                for (int i = 0; i < blockStatement.Statements.Count; i++)
                    PrettyPrint(blockStatement.Statements[i], indent, i == blockStatement.Statements.Count - 1);
                break;

            case BoundVariableDeclarationStatement vd:
                var isInitialized = vd.Initializer is not null;
                PrettyPrint(vd.Variable, indent, isLast: !isInitialized);
                if (isInitialized)
                    PrettyPrint(vd.Initializer!, indent, isLast: true);
                break;

            case BoundAssignmentStatement a:
                PrettyPrint(a.Variable, indent, isLast: false);
                PrettyPrint(a.Expression, indent, isLast: true);
                break;

            case BoundBinaryExpression b:
                PrettyPrint(b.Left, indent, isLast: false);
                PrettyPrint(b.Right, indent, isLast: true);
                break;

            case BoundCallExpression call:
                for (int i = 0; i < call.Arguments.Count; i++)
                    PrettyPrint(call.Arguments[i], indent, isLast: i == call.Arguments.Count - 1);
                break;
        }
    }

    private static void PrintNodeInfo(BoundNode node, string optionalLabel = "")
    {
        switch (node)
        {
            case BoundBinaryExpression b:
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write(optionalLabel);
                Console.Write($"BinaryExpression ({b.Op.Kind})");
                break;
            case BoundLiteralExpression l:
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write(optionalLabel);
                Console.Write($"Literal ({l.Value})");
                break;
            case BoundVariableExpression v:
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write(optionalLabel);
                Console.Write($"Variable ({v.Name})");
                break;
            case BoundCallExpression call:
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.Write(optionalLabel);
                Console.Write($"Function call ({call.Function.Name})");
                break;
            case BoundVariableDeclarationStatement vd:
                Console.ForegroundColor = ConsoleColor.DarkMagenta;
                Console.Write(optionalLabel);
                Console.Write($"Variable declaration ({vd.Variable.Name})");
                break;
            case BoundAssignmentStatement a:
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.Write(optionalLabel);
                Console.Write($"Assignment ({a.Variable.Name})");
                break;
            case BoundIfStatement:
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.Write(optionalLabel);
                Console.Write("If statement");
                break;
            case BoundBlockStatement:
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write(optionalLabel);
                Console.Write("Block statement");
                break;
            case BoundErrorNode err:
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write(optionalLabel);
                Console.Write($"{err.ErrorText}");
                break;
            default:
                Console.Write(optionalLabel);
                Console.Write(node.GetType().Name);
                break;
        }

        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine($" : {node.Type}");
        Console.ResetColor();
    }
}