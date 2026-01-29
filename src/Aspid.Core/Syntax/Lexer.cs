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

        // Puncts (Compound)
        EqEq,
        NotEq,
        PlusEq,
        MinusEq,

        // Terminal
        UndefinedToken,
        EndOfFile
    }

    private static readonly List<(string Text, LexerTokenKind Kind)> OperatorsDefinition = new()
    {
        ("==", LexerTokenKind.EqEq),
        ("!=", LexerTokenKind.NotEq),
        ("+=", LexerTokenKind.PlusEq),
        ("-=", LexerTokenKind.MinusEq),
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
        (",", LexerTokenKind.Comma)
    };

    private static readonly (string Text, LexerTokenKind Kind)[] SortedOperators;

    static Lexer()
    {
        SortedOperators = OperatorsDefinition
            .OrderByDescending(op => op.Text.Length)
            .ThenBy(op => op.Text)
            .ToArray();
    }

    public static Token[] Tokenize(string text)
    {
        var tokens = new List<Token>();
        int position = 0;

        while (position < text.Length)
        {
            char current = text[position];

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

            // Identifiers (Variables, Keywords)
            if (char.IsLetter(current) || current == '_')
            {
                tokens.Add(ParseId(text, position, ref position));
                continue;
            }

            if (current == '"')
            {
                tokens.Add(ParseString(text, position, ref position));
                continue;
            }

            if (TryMatchOperator(text, position, out var opToken, out int length))
            {
                tokens.Add(opToken);
                position += length;
                continue;
            }

            tokens.Add(new Token(current.ToString(), LexerTokenKind.UndefinedToken, position, position + 1));
            position++;
        }

        tokens.Add(new Token("\0", LexerTokenKind.EndOfFile, position, position));
        return tokens.ToArray();
    }

    private static bool TryMatchOperator(string text, int position, out Token token, out int length)
    {
        foreach (var op in SortedOperators)
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
        return new Token(value, LexerTokenKind.Id, start, position);
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

        position++; 

        string value = text.Substring(start + 1, position - start - 2); // Skip quotes

        return new Token(value, LexerTokenKind.String, start, position);
    }
}