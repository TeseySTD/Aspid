using Core.Syntax;

while (true)
{
    Console.Write(">>");
    string? input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input))
    {
        Console.WriteLine();
        continue;
    }

    var tokens = Lexer.Tokenize(input);
    Console.WriteLine(string.Join(", ", tokens.Select(t => t.ToString())));
}