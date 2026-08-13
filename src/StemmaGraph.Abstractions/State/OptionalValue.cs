namespace StemmaGraph.Abstractions.State;

/// <summary>
///     Marks whether a partial update property was set. Default is unset (no write).
///     An explicit null value is a clear, not “unchanged”.
/// </summary>
/// <typeparam name="T">Property value type.</typeparam>
/// <remarks>
///     Creates a present (set) optional value, including explicit null for reference types.
/// </remarks>
/// <param name="value">Value to write when converted to a channel write.</param>
public readonly struct OptionalValue<T>(T value)
{
    /// <summary>
    ///     True when the property was assigned on the partial update (including set-to-null).
    /// </summary>
    public bool IsSet { get; } = true;

    /// <summary>
    ///     The assigned value when <see cref="IsSet" /> is true.
    /// </summary>
    public T Value { get; } = value;

    /// <summary>
    ///     Creates a present optional. Prefer this over implicit conversion when
    ///     <typeparamref name="T" /> is an interface (C# disallows user-defined conversions involving interfaces).
    /// </summary>
    /// <param name="value">Value to write.</param>
    /// <returns>A set optional wrapping <paramref name="value" />.</returns>
    public static OptionalValue<T> Of(T value)
    {
        return new OptionalValue<T>(value);
    }

    /// <summary>
    ///     Implicitly marks a non-interface value as present (string, class, value type).
    ///     Not used when <typeparamref name="T" /> is an interface type.
    /// </summary>
    /// <param name="value">Value to write.</param>
    public static implicit operator OptionalValue<T>(T value) => new(value);
}
