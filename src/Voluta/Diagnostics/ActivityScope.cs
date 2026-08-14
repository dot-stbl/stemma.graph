using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Voluta.Diagnostics;

/// <summary>
///     Starts an <see cref="Activity" />, records elapsed time into a histogram on dispose.
///     Dispose before any <c>yield return</c> in async iterators.
/// </summary>
internal sealed class ActivityScope : IDisposable
{
    private readonly Activity? activity;
    private readonly Histogram<double>? histogram;
    private readonly long startTimestamp;
    private TagList tags;
    private bool disposed;

    private ActivityScope(
        Activity? activity,
        Histogram<double>? histogram,
        TagList tags)
    {
        this.activity = activity;
        this.histogram = histogram;
        this.tags = tags;
        startTimestamp = Stopwatch.GetTimestamp();
    }

    public Activity? Activity => activity;

    public static ActivityScope Start(
        string activityName,
        Histogram<double>? histogram,
        in TagList tags = default)
    {
        var activity = VolutaDiagnostics.ActivitySource.StartActivity(activityName);
        if (activity is not null)
        {
            foreach (var tag in tags)
            {
                activity.SetTag(tag.Key, tag.Value);
            }
        }

        return new ActivityScope(activity, histogram, tags);
    }

    public void SetTag(string key, object? value)
    {
        tags.Add(key, value);
        activity?.SetTag(key, value);
    }

    public void SetError(Exception exception)
    {
        activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
        SetTag(VolutaDiagnostics.TagErrorType, exception.GetType().FullName);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        var elapsedMs = Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
        histogram?.Record(elapsedMs, tags);
        activity?.Dispose();
    }
}
