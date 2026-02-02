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
            Lexer.LexerTokenKind.Star or Lexer.LexerTokenKind.Div => 3,
            Lexer.LexerTokenKind.Plus or Lexer.LexerTokenKind.Minus => 2,
            Lexer.LexerTokenKind.EqEq or Lexer.LexerTokenKind.NotEq => 1,
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
            if (Current.Kind == Lexer.LexerTokenKind.NewLine)
            {
                NextToken();
                continue;
            }

            statements.Add(ParseStatement());
        }

        return statements;
    }

    private Statement ParseStatement()
    {
        if (Current.Kind == Lexer.LexerTokenKind.Indent)
            return ParseBlockStatement();
        if (Current.Kind == Lexer.LexerTokenKind.NewLine)
        {
            NextToken();
            return ParseStatement();
        }

        if (Current.Kind == Lexer.LexerTokenKind.Id &&
            Peek(1).Kind == Lexer.LexerTokenKind.Colon)
            return ParseVariableDeclaration();

        if ((Current.Kind == Lexer.LexerTokenKind.Id && Peek(1).Kind == Lexer.LexerTokenKind.Eq) ||
            (Current.Kind == Lexer.LexerTokenKind.Id && Peek(1).Kind == Lexer.LexerTokenKind.OBracket))
            return ParseAssignmentStatement();

        if (Current.Kind == Lexer.LexerTokenKind.If)
            return ParseIfStatement();

        var expression = ParseExpression();

        if (Current.Kind == Lexer.LexerTokenKind.NewLine)
            NextToken(); // Eat newline after statement if it is

        return new ExpressionStatement(expression);
    }

    private BlockStatement ParseBlockStatement()
    {
        var indentToken = Match(Lexer.LexerTokenKind.Indent) ?? throw new Exception("Expected Indent");
        var statements = new List<Statement>();

        while (Current.Kind != Lexer.LexerTokenKind.Dedent && Current.Kind != Lexer.LexerTokenKind.EndOfFile)
        {
            while (Current.Kind == Lexer.LexerTokenKind.NewLine)
                NextToken();

            if (Current.Kind == Lexer.LexerTokenKind.Dedent) break;

            statements.Add(ParseStatement());
        }

        var dedentToken = Match(Lexer.LexerTokenKind.Dedent) ?? throw new Exception("Expected Dedent");

        return new BlockStatement(indentToken, statements, dedentToken);
    }

    private Statement ParseVariableDeclaration()
    {
        var identifier = Match(Lexer.LexerTokenKind.Id)
                         ?? throw new Exception("Expected identifier");

        Match(Lexer.LexerTokenKind.Colon);

        var typeIdentifier = Match(Lexer.LexerTokenKind.Id)
                             ?? throw new Exception("Expected type identifier");
        // For array types
        while (Current.Kind == Lexer.LexerTokenKind.OBracket)
        {
            NextToken();

            if (Match(Lexer.LexerTokenKind.CBracket) is null)
            {
                throw new Exception("Expected ']' in array type declaration.");
            }

            typeIdentifier = typeIdentifier with { Text = typeIdentifier.Text + "[]" };
        }


        var eq = Match(Lexer.LexerTokenKind.Eq);

        if (eq is null)
            return new VariableDeclarationStatement(identifier, typeIdentifier,
                null); // Allow to only init variables e.g - count: int

        var initializer = ParseExpression();

        return new VariableDeclarationStatement(identifier, typeIdentifier, initializer);
    }

    private Statement ParseAssignmentStatement()
    {
        var isArrayAssignment = Current.Kind == Lexer.LexerTokenKind.Id &&
                                Peek(1).Kind == Lexer.LexerTokenKind.OBracket;
        Expression id = ParseExpression();
        if (isArrayAssignment && id is not ArrayAccessExpression)
            throw new Exception("Expected an array access expression.");
        
        Match(Lexer.LexerTokenKind.Eq);
        var right = ParseExpression();
        return new AssignmentStatement(id, right);
    }

    private Statement ParseIfStatement()
    {
        var ifKeyword = Match(Lexer.LexerTokenKind.If) ?? throw new Exception("Expected If keyword");
        var condition = ParseExpression();
        var colon = Match(Lexer.LexerTokenKind.Colon) ?? throw new Exception("Expected Colon");

        if (Current.Kind == Lexer.LexerTokenKind.NewLine)
            NextToken();

        var thenStatement = ParseStatement();

        Statement? elseStatement = null;

        if (Current.Kind == Lexer.LexerTokenKind.Else)
        {
            NextToken(); // Skip else
            if (Current.Kind == Lexer.LexerTokenKind.Colon) Match(Lexer.LexerTokenKind.Colon);
            if (Current.Kind == Lexer.LexerTokenKind.NewLine) NextToken();

            elseStatement = ParseStatement();
        }

        return new IfStatement(ifKeyword, colon, condition, thenStatement, elseStatement);
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
            Current.Kind == Lexer.LexerTokenKind.Minus ||
            Current.Kind == Lexer.LexerTokenKind.Not ||
            Current.Kind == Lexer.LexerTokenKind.PlusPlus ||
            Current.Kind == Lexer.LexerTokenKind.MinusMinus)
        {
            var operatorToken = NextToken();
            var operand = ParseUnaryExpression();
            var isIncrementOrDecrement = operatorToken.Kind == Lexer.LexerTokenKind.PlusPlus ||
                                         operatorToken.Kind == Lexer.LexerTokenKind.MinusMinus;
            if (isIncrementOrDecrement && operand is not VariableExpression)
                throw new Exception("Variable must be after prefix increment or decrement.");

            return new UnaryExpression(operatorToken, operand);
        }

        return ParsePostfixExpression();
    }

    private Expression ParsePostfixExpression()
    {
        var expression = ParsePrimary();

        while (true)
        {
            if (Current.Kind == Lexer.LexerTokenKind.OParen)
            {
                expression = ParseCallExpression(expression);
            }
            else if (Current.Kind == Lexer.LexerTokenKind.OBracket)
            {
                NextToken(); // '['
                var index = ParseExpression();
                if (Current.Kind != Lexer.LexerTokenKind.CBracket)
                    throw new Exception("Expected ']'");
                NextToken(); // ']'
                expression = new ArrayAccessExpression(expression, index);
            }
            else if (Current.Kind == Lexer.LexerTokenKind.PlusPlus)
            {
                var operatorToken = NextToken();
                if (expression is not VariableExpression)
                    throw new Exception("Variable must be before postfix increment.");
                expression = new PostfixUnaryExpression(operatorToken, expression);
            }
            else if (Current.Kind == Lexer.LexerTokenKind.MinusMinus)
            {
                var operatorToken = NextToken();
                if (expression is not VariableExpression)
                    throw new Exception("Variable must be before postfix decrement.");
                expression = new PostfixUnaryExpression(operatorToken, expression);
            }
            else
            {
                break;
            }
        }

        return expression;
    }

    private Expression ParsePrimary()
    {
        return ParseAtom();
    }

    private Expression ParseAtom()
    {
        switch (Current.Kind)
        {
            case Lexer.LexerTokenKind.True:
                return new BooleanExpression(NextToken(), true);
            case Lexer.LexerTokenKind.False:
                return new BooleanExpression(NextToken(), false);
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

            case Lexer.LexerTokenKind.OBracket:
                return ParseArrayExpression();

            case Lexer.LexerTokenKind.Id:
                return new VariableExpression(NextToken());

            default:
                throw new Exception($"Unexpected token <{Current.Kind}> in expression.");
        }
    }

    private Expression ParseArrayExpression()
    {
        NextToken(); // Eat '['
        var elements = new List<Expression>();

        if (Current.Kind != Lexer.LexerTokenKind.CBracket)
        {
            while (true)
            {
                elements.Add(ParseExpression());
                if (Current.Kind != Lexer.LexerTokenKind.Comma) break;
                NextToken();
            }
        }

        if (Current.Kind != Lexer.LexerTokenKind.CBracket)
            throw new Exception("Expected ']'");
        NextToken();
        return new ArrayExpression(elements);
    }

    private CallExpression ParseCallExpression(Expression function)
    {
        NextToken(); // Eat '('
        var arguments = new List<Expression>();

        if (Current.Kind != Lexer.LexerTokenKind.CParen)
        {
            while (true)
            {
                arguments.Add(ParseExpression());

                if (Current.Kind != Lexer.LexerTokenKind.Comma)
                    break;

                NextToken(); // Eat ','
            }
        }

        if (Match(Lexer.LexerTokenKind.CParen) is null)
            throw new Exception("Expected ')'");

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
            case BooleanExpression b:
                Console.Write(" ");
                Console.Write(b.Value.ToString().ToLower());
                break;
            case CallExpression:
                Console.Write(" (Function Call)");
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