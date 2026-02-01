using Aspid.Core.Binding;
using Aspid.Core.Evaluator;
using Aspid.Core.Syntax;

var globalScope = new BoundScope(null);
var globalVariables = new Dictionary<string, object>();

var binder = new Binder(globalScope);
var evaluator = new Evaluator(globalVariables);

if (args.Length > 0)
{
    var fileName = args[0];
    if (File.Exists(fileName))
    {
        var text = File.ReadAllText(fileName);
        Run(text);
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Error: File '{fileName}' not found.");
        Console.ResetColor();
    }
}
else
    RunRepl();

void Run(string text)
{
    var parser = new Parser(text);
    var statements = parser.Parse();

    foreach (var statement in statements)
    {
        binder.Diagnostics.Clear();

        var boundNode = binder.Bind(statement);

        if (binder.Diagnostics.Any())
        {
            Console.ForegroundColor = ConsoleColor.Red;
            foreach (var diagnostic in binder.Diagnostics)
            {
                Console.WriteLine(diagnostic);
            }

            Console.ResetColor();
        }
        else
        {
            try
            {
                var result = evaluator.Evaluate(boundNode);

                if (result != null && boundNode.Type != TypeSymbol.Void)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(result);
                    Console.ResetColor();
                }
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Runtime Error: {e.Message}");
                Console.ResetColor();
            }
        }
    }
}

void RunRepl()
{
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
        {
            binder.Diagnostics.Clear();

            var boundNode = binder.Bind(statement);

            if (binder.Diagnostics.Any())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                foreach (var diagnostic in binder.Diagnostics)
                {
                    Console.WriteLine(diagnostic);
                }

                Console.ResetColor();
            }
            else
            {
                try
                {
                    var result = evaluator.Evaluate(boundNode);

                    if (result != null && boundNode.Type != TypeSymbol.Void)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(result);
                        Console.ResetColor();
                    }
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Runtime Error: {e.Message}");
                    Console.ResetColor();
                }
            }
        }
    }
}