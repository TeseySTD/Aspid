using System.Globalization;
using Aspid.Core.Syntax;

namespace Aspid.Core.Binding;

public class Binder
{
    private readonly Dictionary<string, TypeSymbol> _variables;
    public List<string> Diagnostics { get; } = new();

    public Binder(Dictionary<string, TypeSymbol> variables)
    {
        _variables = variables;
    }

    public BoundNode Bind(Statement statement)
    {
        return statement switch
        {
            VariableDeclarationStatement declaration => BindVariableDeclarationStatement(declaration),
            AssignmentStatement assignment => BindAssignmentStatement(assignment),
            ExpressionStatement es => BindExpression(es.Expression),
            _ => throw new NotImplementedException($"Binding for {statement.Kind} not implemented yet.")
        };
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

        if (initializer != null && initializer.Type != type)
        {
            var assignWrongTypeError =
                $"Variable '{name}' with type {type.Name} cannot be assigned to type '{initializer.Type.Name}'.";
            Diagnostics.Add(assignWrongTypeError);
            return new BoundErrorNode(assignWrongTypeError);
        }

        if (_variables.TryAdd(name, type))
            return new BoundVariableDeclarationStatement(variable, initializer);

        var alreadyDeclaredError = $"Cannot declare variable with name {name} because it is already declared.";
        Diagnostics.Add(alreadyDeclaredError);
        return new BoundErrorNode(alreadyDeclaredError);
    }

    private BoundNode BindAssignmentStatement(AssignmentStatement assignment)
    {
        var name = assignment.IdentifierToken.Text;
        var expression = BindExpression(assignment.Expression);

        if (_variables.TryGetValue(name, out var existingType))
        {
            if (existingType != expression.Type)
            {
                var errText = $"Cannot assign type '{expression.Type}' to variable '{name}' of type '{existingType}'.";
                Diagnostics.Add(errText);
                return new BoundErrorNode(errText);
            }
        }
        else
        {
            _variables[name] = expression.Type;
        }

        var variable = new BoundVariableExpression(name, expression.Type);
        return new BoundAssignmentStatement(variable, expression);
    }

    private BoundNode BindExpression(Expression syntax)
    {
        return syntax switch
        {
            ParenthesizedExpression p => BindExpression(p.Expression),
            NumberExpression n => BindNumberExpression(n),
            StringExpression str => BindStringExpression(str),
            UnaryExpression un => BindUnaryExpression(un),
            BinaryExpression b => BindBinaryExpression(b),
            VariableExpression v => BindVariableExpression(v),
            _ => throw new Exception($"Unexpected syntax {syntax.Kind}")
        };
    }


    private BoundNode BindNumberExpression(NumberExpression syntax)
    {
        object value;
        var text = syntax.NumberToken.Text;

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
        if (!_variables.TryGetValue(name, out var type))
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
        
        var operatorKind = BoundUnaryOperator.GetOperatorKind(syntax.OperatorToken.Kind);
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


    public static void PrettyPrint(BoundNode node, string indent = "", bool isFirst = false, bool isLast = true)
    {
        var marker = isLast ? "└── " : "├── ";

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write(indent);
        if (!isFirst) Console.Write(marker);
        Console.ResetColor();

        PrintNodeInfo(node);

        indent += isLast ? "    " : "│   ";

        switch (node)
        {
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
        }
    }

    private static void PrintNodeInfo(BoundNode node)
    {
        switch (node)
        {
            case BoundBinaryExpression b:
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"BinaryExpression ({b.Op.Kind})");
                break;
            case BoundLiteralExpression l:
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"Literal ({l.Value})");
                break;
            case BoundVariableExpression v:
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"Variable ({v.Name})");
                break;
            case BoundVariableDeclarationStatement vd:
                Console.ForegroundColor = ConsoleColor.DarkMagenta;
                Console.Write($"Variable declaration ({vd.Variable.Name})");
                break;
            case BoundAssignmentStatement a:
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.Write($"Assignment ({a.Variable.Name})");
                break;
            case BoundErrorNode err:
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write($"{err.ErrorText}");
                break;
            default:
                Console.Write(node.GetType().Name);
                break;
        }

        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine($" : {node.Type}");
        Console.ResetColor();
    }
}