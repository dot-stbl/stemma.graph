using StemmaGraph.Abstractions.Channels;

namespace StemmaGraph.Abstractions.State;

/// <summary>
///     Declares a state property as a named channel with a merge kind.
/// </summary>
/// <param name="kind">LastValue or Append reducer for multi-writer supersteps.</param>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class ChannelAttribute(ChannelKind kind) : Attribute
{
    /// <summary>
    ///     Channel merge kind (LastValue or Append).
    /// </summary>
    public ChannelKind Kind { get; } = kind;

    /// <summary>
    ///     Optional wire channel name. When null or empty, the property name is used.
    /// </summary>
    public string? Name { get; init; }
}
