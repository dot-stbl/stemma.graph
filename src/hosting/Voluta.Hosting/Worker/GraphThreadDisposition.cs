namespace Voluta.Hosting.Worker;

/// <summary>
///     How a worker turn ended (park / complete / fail / cancel).
/// </summary>
public enum GraphThreadDisposition
{
    /// <summary>HITL interrupt — checkpoint holds the thread; wait for a resume wake.</summary>
    Parked,

    /// <summary>Terminal success.</summary>
    Completed,

    /// <summary>Terminal failure — last-good checkpoint remains; do not HITL-resume.</summary>
    Failed,

    /// <summary>Cooperative cancel of this turn (host shutdown or token).</summary>
    Cancelled,
}
