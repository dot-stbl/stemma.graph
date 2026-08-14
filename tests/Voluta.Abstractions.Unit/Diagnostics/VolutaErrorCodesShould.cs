using Shouldly;
using Voluta.Abstractions.Diagnostics;
using Xunit;

namespace Voluta.Abstractions.Unit.Diagnostics;

public sealed class VolutaErrorCodesShould
{
    [Fact(DisplayName = "Given the error catalog, when All is enumerated, then codes are unique and non-empty")]
    public void HaveUniqueNonEmptyCodes()
    {
        VolutaErrorCodes.All.ShouldNotBeEmpty();
        VolutaErrorCodes.All.ShouldAllBe(static code => !string.IsNullOrWhiteSpace(code));
        VolutaErrorCodes.All.Distinct(StringComparer.Ordinal).Count().ShouldBe(VolutaErrorCodes.All.Count);
    }

    [Fact(DisplayName = "Given published codes, when inspected, then each uses dot.case segments")]
    public void UseDotCaseSegments()
    {
        foreach (var code in VolutaErrorCodes.All)
        {
            code.ShouldContain(".");
            code.ShouldNotContain(" ");
            code.ShouldBe(code.ToLowerInvariant());
        }
    }

    [Theory(DisplayName = "Given a known failure class, when reading the const, then matches the published string")]
    [InlineData(nameof(VolutaErrorCodes.GraphOutOfSteps), "graph.out_of_steps")]
    [InlineData(nameof(VolutaErrorCodes.GraphRunFailed), "graph.run_failed")]
    [InlineData(nameof(VolutaErrorCodes.GraphInvalidResume), "graph.invalid_resume")]
    [InlineData(nameof(VolutaErrorCodes.ChannelConcurrentUpdate), "channel.concurrent_update")]
    [InlineData(nameof(VolutaErrorCodes.CheckpointPutFailed), "checkpoint.put_failed")]
    [InlineData(nameof(VolutaErrorCodes.CheckpointUnsupportedFormatVersion), "checkpoint.unsupported_format_version")]
    [InlineData(nameof(VolutaErrorCodes.CommandInvalidKind), "command.invalid_kind")]
    public void ExposeStableLiterals(string fieldName, string expected)
    {
        var value = typeof(VolutaErrorCodes).GetField(fieldName)?.GetValue(null) as string;

        value.ShouldBe(expected);
        VolutaErrorCodes.All.ShouldContain(expected);
    }
}
