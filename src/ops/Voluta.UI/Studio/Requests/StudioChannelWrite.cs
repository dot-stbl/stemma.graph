namespace Voluta.UI.Studio;

/// <summary>
///     One channel write on the Studio wire.
/// </summary>
public sealed class StudioChannelWrite
{
    /// <summary>
    ///     Channel name.
    /// </summary>
    public required string ChannelName { get; init; }

    /// <summary>
    ///     Value (JSON-deserialized object; typically string/number/bool/object).
    /// </summary>
    public object? Value { get; init; }
}
