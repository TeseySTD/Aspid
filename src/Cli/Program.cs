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

    Parser parser = new Parser(input);
    Parser.PrettyPrint(parser.Parse(), isFirst: true);
}