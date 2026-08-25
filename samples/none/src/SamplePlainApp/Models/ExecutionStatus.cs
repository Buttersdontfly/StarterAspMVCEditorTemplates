namespace SamplePlainApp.Models;

/// <summary>
/// USAGE: VALUETASK no heap allocation
/// Represents the outcome of an operation, including its status and an optional message.
/// 
/// This struct provides a simple, consistent way to communicate whether an
/// operation succeeded, failed, or returned no result. Instead of throwing exceptions or
/// returning raw values, methods can return an ExecutionStatus to make outcomes explicit.
///
/// Key features:
/// - Encapsulates a <see cref="State"/> (e.g., Ok, Error, NotFound) and a human-readable message.
/// - Can be implicitly evaluated as a boolean:
///     true  -> State == Ok
///     false -> any other state
/// - Provides factory methods for common outcomes (Success, Error, NotFound).
///
/// This pattern helps standardize error handling.
/// </summary>
public readonly record struct ExecutionStatus
{
#pragma warning disable CA1000
#pragma warning disable CA2225
    public State State { get; }
    public string Message { get; }

    private ExecutionStatus(State state, string message)
    {
        State = state;
        Message = message;
    }

    public static implicit operator bool(ExecutionStatus status)
    {
        return status.State == State.Ok;
    }

    public static ExecutionStatus Success(string message = "Success")
    {
        return new(State.Ok, message);
    }

    public static ExecutionStatus Error(string message)
    {
        return new(State.Error, message);
    }

    public static ExecutionStatus NotFound(string message)
    {
        return new(State.NotFound, message);
    }
#pragma warning restore CA1000
#pragma warning restore CA2225
}

/// <summary>
/// USAGE: VALUETASK no heap allocation
/// Represents the outcome of an operation, including its status and an optional message.
/// 
/// This struct provides a simple, consistent way to communicate whether an
/// operation succeeded, failed, or returned no result. Instead of throwing exceptions or
/// returning raw values, methods can return an ExecutionStatus to make outcomes explicit.
///
/// Key features:
/// - Encapsulates a <see cref="State"/> (e.g., Ok, Error, NotFound) and a human-readable message.
/// - Can be implicitly evaluated as a boolean:
///     true  -> State == Ok
///     false -> any other state
/// - Provides factory methods for common outcomes (Success, Error, NotFound).
///
/// The generic version (<see cref="ExecutionStatus{T}"/>) additionally carries a result value:
/// - The Result property is only accessible when the state is Ok; otherwise it throws.
/// - Use TryGetResult(out T result) for safe access without exceptions.
/// 
/// This pattern helps standardize error handling.
/// </summary>
public readonly record struct ExecutionStatus<T>
{
#pragma warning disable CA1000
#pragma warning disable CA2225
    public State State { get; }
    public string Message { get; }
    private readonly T? _result;

    public T Result => State == State.Ok && _result is not null
        ? _result
        : throw new InvalidOperationException($"Cannot access Result when State is {State}");

    private ExecutionStatus(State state, string message, T? result)
    {
        State = state;
        Message = message;
        _result = result;
    }

    public static implicit operator bool(ExecutionStatus<T> status)
    {
        return status.State == State.Ok;
    }

    public static ExecutionStatus<T> Success(T result)
    {
        return new(State.Ok, "Success", result);
    }

    public static ExecutionStatus<T> Error(string message)
    {
        return new(State.Error, message, default);
    }

    public static ExecutionStatus<T> NotFound(string message)
    {
        return new(State.NotFound, message, default);
    }

    public bool TryGetResult(out T result)
    {
        if (State == State.Ok && _result is not null)
        {
            result = _result;
            return true;
        }
        result = default!;
        return false;
    }
#pragma warning restore CA1000
#pragma warning restore CA2225
}
