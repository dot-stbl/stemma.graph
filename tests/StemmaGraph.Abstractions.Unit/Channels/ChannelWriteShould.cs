// SPDX-License-Identifier: MIT
// Copyright (c) Stemma contributors

using Shouldly;
using StemmaGraph.Channels;
using Xunit;

namespace StemmaGraph.Abstractions.Unit.Channels;

public sealed class ChannelWriteShould
{
    [Fact(DisplayName = "Given name and value, when constructed, then exposes both")]
    public void ExposeNameAndValue()
    {
        var write = new ChannelWrite("status", "running");

        write.ChannelName.ShouldBe("status");
        write.Value.ShouldBe("running");
    }

    [Fact(DisplayName = "Given explicit null value, when constructed, then Value is null (clear, not omit)")]
    public void AllowExplicitNullValue()
    {
        var write = new ChannelWrite("status", null);

        write.ChannelName.ShouldBe("status");
        write.Value.ShouldBeNull();
    }

    [Fact(DisplayName = "When ChannelKind members are listed, then LastValue and Append exist")]
    public void ExposeBuiltInChannelKinds()
    {
        Enum.GetNames<ChannelKind>().ShouldContain("LastValue");
        Enum.GetNames<ChannelKind>().ShouldContain("Append");
        ChannelKind.LastValue.ShouldNotBe(ChannelKind.Append);
    }
}
