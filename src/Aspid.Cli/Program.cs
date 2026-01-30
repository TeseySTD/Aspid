using Aspid.Core.Binding;
using Aspid.Core.Syntax;

Binder binder = new Binder(new());

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
    var boundNodes = statements.Select(binder.Bind);
    foreach (var node in boundNodes)
        Binder.PrettyPrint(node, isFirst: true);
}