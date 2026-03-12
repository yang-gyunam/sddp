using System.Diagnostics.CodeAnalysis;

namespace Sddp.Abstractions.Base;

/// <summary>
/// Represents the outcome of an operation that returns no value.
/// </summary>
public readonly struct Result
{
    private readonly DomainError? _error;

    private Result(DomainError? error)
    {
        _error = error;
    }

    [MemberNotNullWhen(true, nameof(_error))]
    public bool IsFailure => _error is not null;

    public bool IsSuccess => _error is null;

    public DomainError Error => _error ?? throw new InvalidOperationException("Cannot access Error on a successful result");

    public static Result Success() => new(null);

    public static Result Failure(DomainError error) => new(error);

    public static implicit operator Result(DomainError error) => Failure(error);
}

/// <summary>
/// Represents the outcome of an operation that returns a value.
/// </summary>
public readonly struct Result<T>
{
    private readonly T? _value;
    private readonly DomainError? _error;

    private Result(T value)
    {
        _value = value;
        _error = null;
    }

    private Result(DomainError error)
    {
        _value = default;
        _error = error;
    }

    [MemberNotNullWhen(true, nameof(_error))]
    public bool IsFailure => _error is not null;

    public bool IsSuccess => _error is null;

    public T Value => !IsFailure ? _value! : throw new InvalidOperationException("Cannot access Value on a failed result");

    public DomainError Error => _error ?? throw new InvalidOperationException("Cannot access Error on a successful result");

    public static Result<T> Success(T value) => new(value);

    public static Result<T> Failure(DomainError error) => new(error);

    public static implicit operator Result<T>(T value) => Success(value);

    public static implicit operator Result<T>(DomainError error) => Failure(error);
}
