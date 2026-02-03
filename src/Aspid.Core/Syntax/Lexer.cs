namespace Aspid.Core.Syntax;

public static class Lexer
{
    public record Token(string Text, LexerTokenKind Kind, int Start, int End)
    {
        public override string ToString() => $"{Kind} {{{Text}}}";
    }

    public enum LexerTokenKind
    {
        // Values
        Id,
        Number,
        String,

        // Puncts (Simple)
        Eq,
        Plus,
        Minus,
        Star,
        Div,
        Colon,
        OParen,
        CParen,
        OBracket,
        CBracket,
        Comma,
        Not,
        Indent,
        Dedent,
        NewLine,

        // Puncts (Compound)
        EqEq,
        NotEq,
        PlusEq,
        MinusEq,
        PlusPlus,
        MinusMinus,

        // Keywords
        True,
        False,
        If,
        Else,
        Do,
        While,

        // Terminal
        UndefinedToken,
        EndOfFile
    }

    private static readonly List<(string Text, LexerTokenKind Kind)> PunctsDefinition = new()
    {
        ("==", LexerTokenKind.EqEq),
        ("!=", LexerTokenKind.NotEq),
        ("+=", LexerTokenKind.PlusEq),
        ("-=", LexerTokenKind.MinusEq),
        ("++", LexerTokenKind.PlusPlus),
        ("--", LexerTokenKind.MinusMinus),
        ("=", LexerTokenKind.Eq),
        ("+", LexerTokenKind.Plus),
        ("-", LexerTokenKind.Minus),
        ("*", LexerTokenKind.Star),
        ("/", LexerTokenKind.Div),
        ("(", LexerTokenKind.OParen),
        (")", LexerTokenKind.CParen),
        ("[", LexerTokenKind.OBracket),
        ("]", LexerTokenKind.CBracket),
        (":", LexerTokenKind.Colon),
        (",", LexerTokenKind.Comma),
        ("!", LexerTokenKind.Not)
    };

    private static readonly (string Text, LexerTokenKind Kind)[] SortedPuncts;

    static Lexer()
    {
        SortedPuncts = PunctsDefinition
            .OrderByDescending(op => op.Text.Length)
            .ThenBy(op => op.Text)
            .ToArray();
    }

    public static Token[] Tokenize(string text)
    {
        var tokens = new List<Token>();
        var indentStack = new Stack<int>();
        indentStack.Push(0);

        int position = 0;
        bool isStartOfLine = true;

        while (position < text.Length)
        {
            if (isStartOfLine)
            {
                int currentIndent = 0;
                int indentStart = position;
                int spaceCount = 0; 

                while (position < text.Length)
                {
                    char c = text[position];

                    if (c == '\t')
                    {
                        currentIndent++;
                        position++;
                        spaceCount = 0; // Tab drops spaces count
                    }
                    else if (c == ' ')
                    {
                        position++;
                        spaceCount++;
                        if (spaceCount == 4)
                        {
                            currentIndent++;
                            spaceCount = 0;
                        }
                    }
                    else
                    {
                        break; 
                    }
                }

                bool isComment = position < text.Length && text[position] == '#';
                
                // Ignore empty lines and comments
                if (position < text.Length && (text[position] == '\n' || text[position] == '\r' || isComment))
                {
                    if (isComment)
                    {
                        while (position < text.Length && text[position] != '\n' && text[position] != '\r')
                        {
                            position++;
                        }
                    }
                }
                else if (position < text.Length)
                {
                    int lastIndent = indentStack.Peek();

                    if (currentIndent > lastIndent)
                    {
                        // INDENT
                        while (currentIndent > lastIndent)
                        {
                            indentStack.Push(++lastIndent);
                            tokens.Add(new Token("", LexerTokenKind.Indent, indentStart, position));
                        }
                    }
                    else if (currentIndent < lastIndent)
                    {
                        // DEDENT
                        while (currentIndent < indentStack.Peek())
                        {
                            indentStack.Pop();
                            tokens.Add(new Token("", LexerTokenKind.Dedent, indentStart, position));
                        }

                        if (currentIndent != indentStack.Peek())
                            throw new Exception("Indentation error");
                    }
                }

                isStartOfLine = false;
            }

            if (position >= text.Length) break;

            char current = text[position];

            // NewLine handling
            if (current == '\n' || (current == '\r' && position + 1 < text.Length && text[position + 1] == '\n'))
            {
                if (current == '\r') position++;

                position++; // Skip \n

                tokens.Add(new Token("\n", LexerTokenKind.NewLine, position - 1, position));
                isStartOfLine = true;
                continue;
            }

            // Skip spaces
            if (char.IsWhiteSpace(current))
            {
                position++;
                continue;
            }

            // Numbers
            if (char.IsDigit(current))
            {
                tokens.Add(ParseNumber(text, position, ref position));
                continue;
            }

            // Strings handling
            switch (current)
            {
                case '"':
                    tokens.Add(ParseString(text, position, ref position));
                    continue;
                case 'f' when position + 1 < text.Length && text[position + 1] == '"':
                    tokens.AddRange(ParseFormatedString(text, position, ref position));
                    continue;
            }

            // Comments
            if (current == '#')
            {
                while (position < text.Length && text[position] != '\n' && text[position] != '\r')
                {
                    position++;
                }

                continue;
            }

            // Identifiers
            if (char.IsLetter(current) || current == '_')
            {
                tokens.Add(ParseId(text, position, ref position));
                continue;
            }

            if (TryMatchPunct(text, position, out var opToken, out int length))
            {
                tokens.Add(opToken);
                position += length;
                continue;
            }

            tokens.Add(new Token(current.ToString(), LexerTokenKind.UndefinedToken, position, position + 1));
            position++;
        }

        while (indentStack.Peek() > 0)
        {
            indentStack.Pop();
            tokens.Add(new Token("", LexerTokenKind.Dedent, position, position));
        }

        tokens.Add(new Token("\0", LexerTokenKind.EndOfFile, position, position));
        return tokens.ToArray();
    }

    private static bool TryMatchPunct(string text, int position, out Token token, out int length)
    {
        foreach (var op in SortedPuncts)
        {
            if (position + op.Text.Length > text.Length)
                continue;

            if (string.CompareOrdinal(text, position, op.Text, 0, op.Text.Length) == 0)
            {
                token = new Token(op.Text, op.Kind, position, position + op.Text.Length);
                length = op.Text.Length;
                return true;
            }
        }

        token = null!;
        length = 0;
        return false;
    }

    private static Token ParseId(string text, int start, ref int position)
    {
        while (position < text.Length && (char.IsLetterOrDigit(text[position]) || text[position] == '_'))
        {
            position++;
        }

        string value = text.Substring(start, position - start);
        var kind = value switch
        {
            "if" => LexerTokenKind.If,
            "else" => LexerTokenKind.Else,
            "true" => LexerTokenKind.True,
            "false" => LexerTokenKind.False,
            "do" => LexerTokenKind.Do,
            "while" => LexerTokenKind.While,
            _ => LexerTokenKind.Id
        };

        return new Token(value, kind, start, position);
    }


    private static Token ParseNumber(string text, int start, ref int position)
    {
        bool hasDot = false;
        bool isHex = false;

        if (position + 1 < text.Length && text[position] == '0' &&
            (text[position + 1] == 'x' || text[position + 1] == 'X'))
        {
            isHex = true;
            position += 2; // Skip '0x'
        }

        while (position < text.Length)
        {
            char c = text[position];

            if (isHex)
            {
                bool isHexDigit = char.IsDigit(c) ||
                                  (c >= 'a' && c <= 'f') ||
                                  (c >= 'A' && c <= 'F');

                if (!isHexDigit) break;
            }
            else
            {
                if (c == '.')
                {
                    if (hasDot) break; // Second dot is not allowed
                    hasDot = true;
                }
                else if (!char.IsDigit(c))
                {
                    break;
                }
            }

            position++;
        }

        string value = text.Substring(start, position - start);
        return new Token(value, LexerTokenKind.Number, start, position);
    }

    private static Token ParseString(string text, int start, ref int position)
    {
        position++;

        while (position < text.Length && text[position] != '"')
        {
            position++;
        }

        if (position >= text.Length)
        {
            throw new Exception("Unterminated string literal");
        }

        position++; // Skip '"'

        string value = text.Substring(start + 1, position - start - 2); // Skip quotes

        return new Token(value, LexerTokenKind.String, start, position);
    }


    private static List<Token> ParseFormatedString(string text, int start, ref int position)
    {
        List<Token> resultTokens =
        [
            new("(", LexerTokenKind.OParen, start, start + 1) // Virtual '('
        ];

        position += 2; // Skip f"

        var chunkStart = position;

        while (position < text.Length)
        {
            char c = text[position];

            // End of line 
            if (c == '"')
            {
                if (position > chunkStart)
                {
                    string strContent = text.Substring(chunkStart, position - chunkStart);
                    resultTokens.Add(new Token(strContent, LexerTokenKind.String, chunkStart, position));
                }
                else
                {
                    // If was no text e.g f"{a}" add empty string 
                    resultTokens.Add(new Token("", LexerTokenKind.String, position, position));
                }

                break;
            }

            if (c == '{')
            {
                if (position > chunkStart)
                {
                    string strContent = text.Substring(chunkStart, position - chunkStart);
                    resultTokens.Add(new Token(strContent, LexerTokenKind.String, chunkStart, position));

                    // Replace '{' with + to make explicit concatenation
                    resultTokens.Add(new Token("+", LexerTokenKind.Plus, position, position + 1));
                }

                int braceStart = position;
                position++; // Skip {

                int expressionStart = position;
                while (position < text.Length && text[position] != '}')
                {
                    position++;
                }

                if (position >= text.Length) throw new Exception("Unclosed brace in f-string");

                int expressionEnd = position;

                string expressionText = text.Substring(expressionStart, expressionEnd - expressionStart);

                var subTokens = Tokenize(expressionText);

                // Add parenthesis around expressions ( {expr} -> (expr) )
                resultTokens.Add(new Token("(", LexerTokenKind.OParen, braceStart, braceStart + 1));

                foreach (var t in subTokens)
                {
                    if (t.Kind == LexerTokenKind.EndOfFile) continue;
                    // Change position of token to original one
                    resultTokens.Add(t with
                    {
                        Start = t.Start + expressionStart,
                        End = t.End + expressionStart
                    });
                }

                resultTokens.Add(new Token(")", LexerTokenKind.CParen, position, position + 1));

                if (position + 1 < text.Length)
                {
                    resultTokens.Add(new Token("+", LexerTokenKind.Plus, position, position + 1));
                }


                chunkStart = position + 1;
            }

            position++;
        }

        if (position >= text.Length) throw new Exception("Unterminated f-string");

        position++; // Skip "

        // Virtual ')'
        resultTokens.Add(new Token(")", LexerTokenKind.CParen, position - 1, position));

        return resultTokens;
    }
}