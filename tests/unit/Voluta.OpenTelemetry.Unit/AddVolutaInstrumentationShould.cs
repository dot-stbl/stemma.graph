using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Shouldly;
using Voluta.Diagnostics;
using Xunit;

namespace Voluta.OpenTelemetry.Unit;

public sealed class AddVolutaInstrumentationShould
{
    [Fact(DisplayName = "Given TracerProviderBuilder, when AddVolutaInstrumentation, then source is registered")]
    public void RegisterActivitySource()
    {
        using var provider = Sdk.CreateTracerProviderBuilder()
            .AddVolutaInstrumentation()
            .Build();

        provider.ShouldNotBeNull();
        VolutaDiagnostics.SourceName.ShouldBe("Voluta");
    }

    [Fact(DisplayName = "Given MeterProviderBuilder, when AddVolutaInstrumentation, then meter is registered")]
    public void RegisterMeter()
    {
        using var provider = Sdk.CreateMeterProviderBuilder()
            .AddVolutaInstrumentation()
            .Build();

        provider.ShouldNotBeNull();
        VolutaDiagnostics.Meter.Name.ShouldBe(VolutaDiagnostics.SourceName);
    }
}
