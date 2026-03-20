namespace Tungsten.Api.Common;

public class Result<T>
{
    public T Value { get; }
    public string? Error { get; }
    public bool IsSuccess { get; }

    private Result(T value) { Value = value; IsSuccess = true; }
    private Result(string error) { Value = default!; Error = error; IsSuccess = false; }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(string error) => new(error);
}

public class Result
{
    public string? Error { get; }
    public bool IsSuccess { get; }

    private Result() { IsSuccess = true; }
    private Result(string error) { Error = error; IsSuccess = false; }

    public static Result Success() => new();
    public static Result Failure(string error) => new(error);
}
