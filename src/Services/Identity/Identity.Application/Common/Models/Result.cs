namespace Identity.Application.Common.Models;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string Error { get; }
    public List<string> Errors { get; }

    protected Result(bool isSuccess, string error, List<string>? errors = null)
    {
        IsSuccess = isSuccess;
        Error = error;
        Errors = errors ?? new List<string>();
    }

    public static Result Success() => new(true, string.Empty);
    public static Result Failure(string error) => new(false, error);
    public static Result Failure(List<string> errors) => new(false, string.Join("; ", errors), errors);
}

public class Result<T> : Result
{
    public T? Value { get; }

    protected Result(T? value, bool isSuccess, string error, List<string>? errors = null)
        : base(isSuccess, error, errors)
    {
        Value = value;
    }

    public static Result<T> Success(T value) => new(value, true, string.Empty);
    public static new Result<T> Failure(string error) => new(default, false, error);
    public static new Result<T> Failure(List<string> errors) => new(default, false, string.Join("; ", errors), errors);

    public static implicit operator Result<T>(T value) => Success(value);
}
