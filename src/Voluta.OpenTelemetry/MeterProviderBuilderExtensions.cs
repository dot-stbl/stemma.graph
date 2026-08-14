using OpenTelemetry.Metrics;
using Voluta.Diagnostics;

namespace Voluta.OpenTelemetry;

/// <summary>
///     Registers Voluta <see cref="System.Diagnostics.Metrics.Meter" /> with OpenTelemetry.
/// </summary>
public static class MeterProviderBuilderExtensions
{
    /// <summary>
    ///     Adds Voluta runtime metrics (superstep/node duration, interrupt and checkpoint counts)
    ///     to the meter provider.
    /// </summary>
    /// <param name="builder">OpenTelemetry meter provider builder.</param>
    /// <returns>The same builder for chaining.</returns>
    public static MeterProviderBuilder AddVolutaInstrumentation(this MeterProviderBuilder builder)
    {
        return builder.AddMeter(VolutaDiagnostics.SourceName);
    }
}
