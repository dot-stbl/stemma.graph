using OpenTelemetry.Trace;
using Voluta.Diagnostics;

namespace Voluta.OpenTelemetry;

/// <summary>
///     Registers Voluta <see cref="System.Diagnostics.ActivitySource" /> with OpenTelemetry.
/// </summary>
public static class TracerProviderBuilderExtensions
{
    /// <summary>
    ///     Adds Voluta runtime activities (superstep, node execute, checkpoint put/get/list)
    ///     to the tracer provider.
    /// </summary>
    /// <param name="builder">OpenTelemetry tracer provider builder.</param>
    /// <returns>The same builder for chaining.</returns>
    public static TracerProviderBuilder AddVolutaInstrumentation(this TracerProviderBuilder builder)
    {
        return builder.AddSource(VolutaDiagnostics.SourceName);
    }
}
