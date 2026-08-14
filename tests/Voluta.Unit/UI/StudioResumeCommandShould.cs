using Shouldly;
using Voluta.Abstractions.Runtime;
using Voluta.UI.Studio;
using Xunit;

namespace Voluta.Unit.UI;

public sealed class StudioResumeCommandShould
{
    [Fact(DisplayName = "Given null kind, when Resolve, then approve with optional payload")]
    public void DefaultApprove()
    {
        var command = StudioResumeCommand.Resolve(null, "ok");

        command.Kind.ShouldBe(Command.Kinds.Approve);
        command.Payload.ShouldBe("ok");
    }

    [Fact(DisplayName = "Given reject kind, when Resolve, then reject command")]
    public void RejectKind()
    {
        var command = StudioResumeCommand.Resolve(Command.Kinds.Reject, "nope");

        command.Kind.ShouldBe(Command.Kinds.Reject);
        command.Payload.ShouldBe("nope");
    }

    [Fact(DisplayName = "Given update without values, when Resolve, then throws")]
    public void UpdateRequiresValues()
    {
        var exception = Should.Throw<ArgumentException>(
            () => StudioResumeCommand.Resolve(Command.Kinds.Update, null));

        exception.Message.ShouldContain("values");
    }

    [Fact(DisplayName = "Given update with values, when Resolve, then update command")]
    public void UpdateWithValues()
    {
        var values = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["status"] = "synthesize",
        };

        var command = StudioResumeCommand.Resolve(Command.Kinds.Update, null, values);

        command.Kind.ShouldBe(Command.Kinds.Update);
        command.Values.ShouldNotBeNull();
        command.Values!["status"].ShouldBe("synthesize");
    }

    [Fact(DisplayName = "Given unknown kind, when Resolve, then throws")]
    public void UnknownKind()
    {
        Should.Throw<ArgumentException>(() => StudioResumeCommand.Resolve("explode"));
    }

    [Fact(DisplayName = "Given multi-interrupt resumes, when Resolve approve, then Resumes set")]
    public void MultiInterruptApprove()
    {
        var resumes = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["task-a"] = "ok",
        };

        var command = StudioResumeCommand.Resolve(Command.Kinds.Approve, resumes: resumes);

        command.Kind.ShouldBe(Command.Kinds.Approve);
        command.Resumes.ShouldNotBeNull();
        command.Resumes!["task-a"].ShouldBe("ok");
    }
}
