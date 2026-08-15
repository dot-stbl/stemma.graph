using Voluta.Abstractions.Channels;
using Voluta.Abstractions.State;

namespace Voluta.Generators.Unit.Fixtures;

/// <summary>
/// Sample [GraphState] model used by generator consumer tests.
/// </summary>
[GraphState]
public partial class AgentState
{
    /// <summary>
    /// Append-reduced message list channel.
    /// </summary>
    [Channel(ChannelKind.Append)]
    public IList<object?> Messages { get; set; } = [];

    /// <summary>
    /// LastValue status channel.
    /// </summary>
    [Channel(ChannelKind.LastValue)]
    public string? Status { get; set; }
}
