using Voluta.Abstractions.Runtime;

namespace Voluta.UI.Studio;

/// <summary>
///     Maps Studio resume request kind/payload onto the closed HITL <see cref="Command" /> taxonomy.
/// </summary>
public static class StudioResumeCommand
{
    /// <summary>
    ///     Resolves a resume <see cref="Command" /> from wire fields.
    /// </summary>
    /// <param name="kind">Command kind (default approve).</param>
    /// <param name="payload">Optional opaque payload.</param>
    /// <param name="values">Optional channel merges for approve/reject/update.</param>
    /// <param name="resumes">Optional per-task resume map (multi-interrupt).</param>
    /// <returns>Validated command factories for known kinds.</returns>
    public static Command Resolve(
        string? kind,
        object? payload = null,
        IReadOnlyDictionary<string, object?>? values = null,
        IReadOnlyDictionary<string, object?>? resumes = null)
    {
        var resolvedKind = string.IsNullOrWhiteSpace(kind) ? Command.Kinds.Approve : kind;
        return resumes is { Count: > 0 }
            ? StudioResumeCommandFactories.ResolveMulti(resolvedKind, values, resumes)
            : StudioResumeCommandFactories.ResolveSingle(resolvedKind, payload, values);
    }
}

/// <summary>
///     Single- and multi-interrupt command factories for Studio resume.
/// </summary>
file static class StudioResumeCommandFactories
{
    public static Command ResolveMulti(
        string resolvedKind,
        IReadOnlyDictionary<string, object?>? values,
        IReadOnlyDictionary<string, object?> resumes)
    {
        return resolvedKind switch
        {
            Command.Kinds.Approve => values is { Count: > 0 }
                ? Command.ApproveResumes(resumes, values)
                : Command.ApproveResumes(resumes),
            Command.Kinds.Reject => values is { Count: > 0 }
                ? Command.RejectResumes(resumes, values)
                : Command.RejectResumes(resumes),
            Command.Kinds.Update => values is { Count: > 0 }
                ? Command.UpdateResumes(values, resumes)
                : throw new ArgumentException(
                    "Resume kind 'update' requires a non-empty values map (channel writes)."),
            _ => throw new ArgumentException(
                $"Unknown resume command kind '{resolvedKind}'. Expected one of: {Command.Kinds.Approve}, {Command.Kinds.Reject}, {Command.Kinds.Update}."),
        };
    }

    public static Command ResolveSingle(
        string resolvedKind,
        object? payload,
        IReadOnlyDictionary<string, object?>? values)
    {
        return resolvedKind switch
        {
            Command.Kinds.Approve => Command.Approve(payload, values),
            Command.Kinds.Reject => Command.Reject(payload, values),
            Command.Kinds.Update => values is { Count: > 0 }
                ? Command.Update(values, payload)
                : throw new ArgumentException(
                    "Resume kind 'update' requires a non-empty values map (channel writes)."),
            _ => throw new ArgumentException(
                $"Unknown resume command kind '{resolvedKind}'. Expected one of: {Command.Kinds.Approve}, {Command.Kinds.Reject}, {Command.Kinds.Update}."),
        };
    }
}
