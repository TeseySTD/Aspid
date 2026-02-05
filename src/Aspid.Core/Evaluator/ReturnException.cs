namespace Aspid.Core.Evaluator;

internal sealed class ReturnException(object? value) : Exception
{
    public object? Value { get; } = value;

    public override string StackTrace => string.Empty; 
}
