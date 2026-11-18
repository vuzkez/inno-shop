namespace Inno_Shop.Products.Application.Common
{
    /// <summary>Same error categories the Users service uses, so the API maps both consistently.</summary>
    public enum ErrorType
    {
        None,
        Validation,
        NotFound,
        Conflict,
        Forbidden,
        Unauthorized
    }

    /// <summary>
    /// Outcome of a command that returns nothing. Mirrors <c>UserResult</c> from the Users service
    /// instead of throwing, so expected failures stay part of the method signature.
    /// </summary>
    public class Result
    {
        public bool IsSuccess { get; init; }

        public List<string> Errors { get; init; } = new();

        public ErrorType Type { get; init; } = ErrorType.None;

        public static Result Success() => new() { IsSuccess = true };

        public static Result Failure(string error, ErrorType type = ErrorType.Validation) =>
            new() { IsSuccess = false, Errors = new List<string> { error }, Type = type };

        public static Result Failure(List<string> errors, ErrorType type = ErrorType.Validation) =>
            new() { IsSuccess = false, Errors = errors, Type = type };
    }

    /// <summary>Outcome of a command or query that returns a value on success.</summary>
    public sealed class Result<T> : Result
    {
        public T? Value { get; init; }

        public static Result<T> Success(T value) => new() { IsSuccess = true, Value = value };

        public static new Result<T> Failure(string error, ErrorType type = ErrorType.Validation) =>
            new() { IsSuccess = false, Errors = new List<string> { error }, Type = type };

        public static new Result<T> Failure(List<string> errors, ErrorType type = ErrorType.Validation) =>
            new() { IsSuccess = false, Errors = errors, Type = type };
    }
}
