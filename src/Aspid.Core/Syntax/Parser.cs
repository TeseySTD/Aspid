namespace Aspid.Core.Syntax;

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

    private static int GetBinaryOperatorPrecedence(Lexer.LexerTokenKind kind)
    {
        return kind switch
        {
            Lexer.LexerTokenKind.Star or Lexer.LexerTokenKind.Div => 2,
            Lexer.LexerTokenKind.Plus or Lexer.LexerTokenKind.Minus => 1,
            _ => 0
        };
    }

    public IEnumerable<Statement> Parse()
    {
        var statements = ParseCompilationUnit();

        if (Current.Kind != Lexer.LexerTokenKind.EndOfFile)
        {
            throw new Exception($"Unexpected token <{Current.Kind}> after expression.");
        }

        return statements;
    }

    private IEnumerable<Statement> ParseCompilationUnit()
    {
        var statements = new List<Statement>();
        while (Current.Kind != Lexer.LexerTokenKind.EndOfFile)
        {
            statements.Add(ParseStatement());
        }

        return statements;
    }

    private Statement ParseStatement()
    {
        if (Current.Kind == Lexer.LexerTokenKind.Id &&
            Peek(1).Kind == Lexer.LexerTokenKind.Colon)
        {
            return ParseVariableDeclaration();
        }

        if (Current.Kind == Lexer.LexerTokenKind.Id &&
            Peek(1).Kind == Lexer.LexerTokenKind.Eq)
        {
            return ParseAssignmentStatement();
        }

        var expression = ParseExpression();

        return new ExpressionStatement(expression);
    }

    private Statement ParseVariableDeclaration()
    {
        var identifier = Match(Lexer.LexerTokenKind.Id)
                         ?? throw new Exception("Expected identifier");

        Match(Lexer.LexerTokenKind.Colon);

        var typeIdentifier = Match(Lexer.LexerTokenKind.Id)
                             ?? throw new Exception("Expected type identifier");

        var eq = Match(Lexer.LexerTokenKind.Eq);

        if (eq is null)
            return new VariableDeclarationStatement(identifier, typeIdentifier,
                null); // Allow to only init variables e.g - count: int

        var initializer = ParseExpression();

        return new VariableDeclarationStatement(identifier, typeIdentifier, initializer);
    }

    private Statement ParseAssignmentStatement()
    {
        var identifier = Match(Lexer.LexerTokenKind.Id) ?? throw new Exception("Expected Id");

        Match(Lexer.LexerTokenKind.Eq);

        var right = ParseExpression();

        return new AssignmentStatement(identifier, right);
    }


    private Expression ParseExpression()
    {
        return ParseBinaryExpression();
    }

    private Expression ParseBinaryExpression(int parentPrecedence = 0)
    {
        var left = ParseUnaryExpression();

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

    private Expression ParseUnaryExpression()
    {
        if (Current.Kind == Lexer.LexerTokenKind.Plus ||
            Current.Kind == Lexer.LexerTokenKind.Minus)
        {
            var operatorToken = NextToken();
            var operand = ParseUnaryExpression(); 

            return new UnaryExpression(operatorToken, operand);
        }

        return ParsePrimary();
    }

    private Expression ParsePrimary()
    {
        Expression left = ParseAtom();

        while (Current.Kind == Lexer.LexerTokenKind.OParen)
        {
            left = ParseCallExpression(left);
        }

        return left;
    }


    private Expression ParseAtom()
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

            case Lexer.LexerTokenKind.String:
                return new StringExpression(NextToken());

            case Lexer.LexerTokenKind.Id:
                return new VariableExpression(NextToken());

            default:
                throw new Exception($"Unexpected token <{Current.Kind}>");
        }
    }

    private Expression ParseCallExpression(Expression function)
    {
        NextToken();
        var arguments = new List<Expression>();

        if (Current.Kind != Lexer.LexerTokenKind.CParen)
        {
            while (true)
            {
                arguments.Add(ParseExpression());

                if (Current.Kind != Lexer.LexerTokenKind.Comma)
                    break;

                NextToken();
            }
        }

        if (Match(Lexer.LexerTokenKind.CParen) is null) throw new Exception("Expected ')'");

        return new CallExpression(function, arguments);
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