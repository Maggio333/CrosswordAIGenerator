namespace CrosswordAIGenerator.Core.Domain.Common;

/// <summary>
/// Railway Oriented Programming - Result pattern
/// Reprezentuje wynik operacji, który może być sukcesem (z wartością) lub błędem
/// </summary>
public class Result<TValue, TError>
{
    private readonly TValue? _value;
    private readonly TError? _error;

    private Result(TValue? value, TError? error)
    {
        _value = value;
        _error = error;
    }

    /// <summary>
    /// Czy wynik jest sukcesem
    /// </summary>
    public bool IsSuccess => _error == null;

    /// <summary>
    /// Czy wynik jest błędem
    /// </summary>
    public bool IsFailure => _error != null;

    /// <summary>
    /// Wartość (tylko gdy IsSuccess == true)
    /// </summary>
    public TValue Value
    {
        get
        {
            if (IsFailure)
                throw new InvalidOperationException("Cannot get value from failed result");
            return _value!;
        }
    }

    /// <summary>
    /// Błąd (tylko gdy IsFailure == true)
    /// </summary>
    public TError Error
    {
        get
        {
            if (IsSuccess)
                throw new InvalidOperationException("Cannot get error from successful result");
            return _error!;
        }
    }

    /// <summary>
    /// Tworzy wynik sukcesu
    /// </summary>
    public static Result<TValue, TError> Success(TValue value) => new(value, default);

    /// <summary>
    /// Tworzy wynik błędu
    /// </summary>
    public static Result<TValue, TError> Failure(TError error) => new(default, error);

    /// <summary>
    /// Mapuje wartość sukcesu na inną wartość
    /// </summary>
    public Result<TNewValue, TError> Map<TNewValue>(Func<TValue, TNewValue> func)
    {
        if (IsSuccess)
            return Result<TNewValue, TError>.Success(func(_value!));
        return Result<TNewValue, TError>.Failure(_error!);
    }

    /// <summary>
    /// Bind (flat map) - łączy wyniki w pipeline
    /// </summary>
    public Result<TNewValue, TError> Bind<TNewValue>(Func<TValue, Result<TNewValue, TError>> func)
    {
        if (IsSuccess)
            return func(_value!);
        return Result<TNewValue, TError>.Failure(_error!);
    }

    /// <summary>
    /// Wykonuje akcję gdy sukces
    /// </summary>
    public Result<TValue, TError> OnSuccess(Action<TValue> action)
    {
        if (IsSuccess)
            action(_value!);
        return this;
    }

    /// <summary>
    /// Wykonuje akcję gdy błąd
    /// </summary>
    public Result<TValue, TError> OnFailure(Action<TError> action)
    {
        if (IsFailure)
            action(_error!);
        return this;
    }
}

