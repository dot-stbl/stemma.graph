namespace Voluta.Abstractions.Store;

/// <summary>
///     One entry in a cross-thread <see cref="IVolutaStore" /> (namespace + key + value).
/// </summary>
public sealed class StoreItem
{
    /// <summary>
    ///     Hierarchical namespace segments (e.g. <c>["users", "memories"]</c>). Empty is the root.
    /// </summary>
    public required IReadOnlyList<string> Namespace { get; init; }

    /// <summary>Item key within the namespace (unique per namespace).</summary>
    public required string Key { get; init; }

    /// <summary>Stored value (any CLR object for InMemory; durable providers may constrain types later).</summary>
    public object? Value { get; init; }

    /// <summary>UTC instant of the last successful Put (host clock at write time).</summary>
    public DateTimeOffset UpdatedAt { get; init; }
}
