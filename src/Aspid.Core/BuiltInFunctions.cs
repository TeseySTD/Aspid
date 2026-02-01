using Aspid.Core.Binding;

namespace Aspid.Core;

public static class BuiltInFunctions
{
    public static readonly Dictionary<FunctionSymbol, Func<object[], object?>> Implementations = new();

    public static readonly FunctionSymbol Print = new(
        "print",
        [new("str", TypeSymbol.String)],
        TypeSymbol.Void
    );

    public static readonly FunctionSymbol Input = new(
        "input",
        [],
        TypeSymbol.String
    );

    public static readonly FunctionSymbol Random = new(
        "random",
        [
            new("min", TypeSymbol.Int),
            new("max", TypeSymbol.Int)
        ],
        TypeSymbol.Int
    );

    static BuiltInFunctions()
    {
        Implementations[Print] = args =>
        {
            Console.WriteLine(args[0]);
            return null;
        };

        Implementations[Input] = args =>
        {
            var input = Console.ReadLine();
            return input ?? string.Empty;
        };

        Implementations[Random] = args =>
        {
            var min = (int)args[0];
            var max = (int)args[1];
            return System.Random.Shared.Next(minValue: min, maxValue: max);
        };
    }

    public static IEnumerable<FunctionSymbol> GetAll() => Implementations.Keys;
}