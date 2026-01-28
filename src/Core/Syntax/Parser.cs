namespace Core.Syntax;

public class Parser(string text)
{
    private readonly Lexer.Token[] _tokens = Lexer.Tokenize(text);
    private int _position;

    private Lexer.Token Current => Peek(0);

    private Lexer.Token Peek(int offset)
    {
        var index = _position + offset;
        if (index >= _tokens.Length) return _tokens[^1];
        return _tokens[index];
    }

    private Lexer.Token NextToken()
    {
        var current = Current;
        _position++;
        return current;
    }

    private Lexer.Token? Match(Lexer.LexerTokenKind kind)
    {
        if (Current.Kind == kind) return NextToken();
        return null;
    }

    public SyntaxNode Parse()
    {
        var expression = ParseExpression();

        if (Current.Kind != Lexer.LexerTokenKind.EndOfFile)
        {
            throw new Exception($"Unexpected token <{Current.Kind}> after expression.");
        }

        return expression;
    }


    private static int GetBinaryOperatorPrecedence(Lexer.LexerTokenKind kind)
    {
        return kind switch
        {
            Lexer.LexerTokenKind.Star or Lexer.LexerTokenKind.Div => 2,
            Lexer.LexerTokenKind.Plus or Lexer.LexerTokenKind.Minus => 1,
            _ => 0
        };
    }

    private Expression ParseExpression()
    {
        return ParseAssignmentExpression();
    }

    private Expression ParseAssignmentExpression()
    {
        var left = ParseBinaryExpression();

        if (Current.Kind == Lexer.LexerTokenKind.Eq)
        {
            if (left is VariableExpression variable)
            {
                NextToken();

                var right = ParseAssignmentExpression();

                return new AssignmentExpression(variable, right);
            }

            throw new Exception("Cannot assign to something that is not a variable");
        }

        return left;
    }

    private Expression ParseBinaryExpression(int parentPrecedence = 0)
    {
        var left = ParsePrimary();

        while (true)
        {
            var precedence = GetBinaryOperatorPrecedence(Current.Kind);
            if (precedence == 0 || precedence <= parentPrecedence)
                break;

            var operatorToken = NextToken();
            var right = ParseBinaryExpression(precedence);
            left = new BinaryExpression(left, operatorToken, right);
        }

        return left;
    }


    private Expression ParsePrimary()
    {
        switch (Current.Kind)
        {
            case Lexer.LexerTokenKind.OParen:
                var left = NextToken();
                var expression = ParseExpression();
                var right = Match(Lexer.LexerTokenKind.CParen)
                            ?? throw new Exception("Missing closing parenthesis");
                return new ParenthesizedExpression(left, expression, right);

            case Lexer.LexerTokenKind.Number:
                return new NumberExpression(NextToken());

            case Lexer.LexerTokenKind.Id:
                return new VariableExpression(NextToken());

            default:
                throw new Exception($"Unexpected token <{Current.Kind}>");
        }
    }


    public static void PrettyPrint(SyntaxNode node, string indent = "", bool isFirst = false, bool isLast = true)
    {
        var marker = isLast ? "└──" : "├──";
        Console.Write(indent);
        if (!isFirst) Console.Write(marker);
        Console.Write(node.Kind);

        switch (node)
        {
            case NumberExpression numberExpression:
                Console.Write(" ");
                Console.Write(numberExpression.NumberToken.Text);
                break;
            case BinaryExpression binaryExpression:
                Console.Write(" ");
                Console.Write(binaryExpression.OperatorToken.Text);
                break;
            case VariableExpression variableExpression:
                Console.Write(" ");
                Console.Write(variableExpression.VariableName.Text);
                break;
        }

        Console.WriteLine();

        indent += isLast ? "    " : "│   ";

        var lastChild = node.GetChildren().LastOrDefault();
        foreach (var child in node.GetChildren())
        {
            PrettyPrint(child, indent, isLast: child == lastChild);
        }
    }
}