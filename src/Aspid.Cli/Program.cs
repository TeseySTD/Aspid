using Aspid.Core.Syntax;

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
    var statements = parser.Parse();
    foreach (var statement in statements)
        Parser.PrettyPrint(statement, isFirst: true);
}