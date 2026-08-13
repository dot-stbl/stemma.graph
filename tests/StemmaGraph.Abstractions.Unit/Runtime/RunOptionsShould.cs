// SPDX-License-Identifier: MIT
// Copyright (c) Stemma contributors

using Shouldly;
using StemmaGraph.Abstractions.Runtime;
using StemmaGraph.Abstractions.Streaming;
using Xunit;

namespace StemmaGraph.Abstractions.Unit.Runtime;

public sealed class RunOptionsShould
{
    [Fact(DisplayName = "Given ThreadId only, when constructed, then StreamMode defaults to Updates")]
    public void DefaultStreamModeToUpdates()
    {
        var options = new RunOptions { ThreadId = "t-1" };

        options.ThreadId.ShouldBe("t-1");
        options.StreamMode.ShouldBe(StreamMode.Updates);
    }

    [Fact(DisplayName = "Given Command fields, when constructed, then preserves kind, payload, and values")]
    public void CarryCommandFields()
    {
        var values = new Dictionary<string, object?>(StringComparer.Ordinal) { ["approved"] = true };

        var command = new Command
        {
            Kind = "approve",
            Payload = "ok",
            Values = values,
        };

        command.Kind.ShouldBe("approve");
        command.Payload.ShouldBe("ok");
        command.Values.ShouldBeSameAs(values);
    }
}
