// SPDX-License-Identifier: MIT
// Copyright (c) Stemma contributors

using Shouldly;
using StemmaGraph.Abstractions.Channels;
using StemmaGraph.Abstractions.Streaming;
using Xunit;

namespace StemmaGraph.Abstractions.Unit.Streaming;

public sealed class StreamEventShould
{
    [Fact(DisplayName = "When constructed with defaults, then empty lists and None kind")]
    public void UseEmptyDefaults()
    {
        var item = new StreamEvent();

        item.Mode.ShouldBe(StreamMode.Values);
        item.Kind.ShouldBe(StreamEventKind.None);
        item.Step.ShouldBe(0);
        item.NodeNames.ShouldBeEmpty();
        item.Writes.ShouldBeEmpty();
        item.State.ShouldBeNull();
        item.Payload.ShouldBeNull();
    }

    [Fact(DisplayName = "Given updates fields, when constructed, then preserves mode, nodes, and writes")]
    public void CarryUpdatesPayload()
    {
        var writes = new[] { new ChannelWrite("messages", "x") };

        var item = new StreamEvent
        {
            Mode = StreamMode.Updates,
            Kind = StreamEventKind.Updates,
            Step = 2,
            NodeNames = ["tools"],
            Writes = writes,
        };

        item.Mode.ShouldBe(StreamMode.Updates);
        item.Kind.ShouldBe(StreamEventKind.Updates);
        item.Step.ShouldBe(2);
        item.NodeNames.ShouldBe(["tools"]);
        item.Writes.ShouldBeSameAs(writes);
    }

    [Fact(DisplayName = "When StreamMode is enumerated, then includes Values, Updates, Events")]
    public void ExposeStreamModes()
    {
        var names = Enum.GetNames<StreamMode>();

        names.ShouldContain(nameof(StreamMode.Values));
        names.ShouldContain(nameof(StreamMode.Updates));
        names.ShouldContain(nameof(StreamMode.Events));
    }
}
