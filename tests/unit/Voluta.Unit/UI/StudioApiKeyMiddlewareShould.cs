using Microsoft.AspNetCore.Http;
using Shouldly;
using Voluta.UI.Studio;
using Xunit;

namespace Voluta.Unit.UI;

public sealed class StudioApiKeyMiddlewareShould
{
    [Fact(DisplayName = "Given null required key, when IsAuthorized, then true")]
    public void AuthOffWhenNoKey()
    {
        var context = new DefaultHttpContext();

        StudioApiKeyMiddleware.IsAuthorized(context.Request, null).ShouldBeTrue();
        StudioApiKeyMiddleware.IsAuthorized(context.Request, "").ShouldBeTrue();
    }

    [Fact(DisplayName = "Given matching X-Api-Key, when IsAuthorized, then true")]
    public void HeaderMatch()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[StudioApiKeyMiddleware.HeaderName] = "secret-key";

        StudioApiKeyMiddleware.IsAuthorized(context.Request, "secret-key").ShouldBeTrue();
    }

    [Fact(DisplayName = "Given matching Bearer token, when IsAuthorized, then true")]
    public void BearerMatch()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer secret-key";

        StudioApiKeyMiddleware.IsAuthorized(context.Request, "secret-key").ShouldBeTrue();
    }

    [Fact(DisplayName = "Given wrong key, when IsAuthorized, then false")]
    public void WrongKey()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[StudioApiKeyMiddleware.HeaderName] = "nope";

        StudioApiKeyMiddleware.IsAuthorized(context.Request, "secret-key").ShouldBeFalse();
    }

    [Fact(DisplayName = "Given missing key when required, when IsAuthorized, then false")]
    public void MissingWhenRequired()
    {
        var context = new DefaultHttpContext();

        StudioApiKeyMiddleware.IsAuthorized(context.Request, "secret-key").ShouldBeFalse();
    }
}
