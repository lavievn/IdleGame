using System;

// A combat clock only. The caller accepts impact synchronously against its current
// encounter/lives. The video and renderer have no access to this clock.
internal sealed class HeavenlyLightningSchedule
{
    internal enum Event { None, Warning, Impact }
    private readonly double interval, warning;
    private double elapsed;
    private bool warningPending;
    internal long StrikeId { get; private set; }
    internal bool IsWarning => warningPending;

    internal HeavenlyLightningSchedule(double interval, double warning)
    {
        this.interval = interval;
        this.warning = double.IsNaN(warning) || double.IsInfinity(warning) || warning <= 0
            ? interval / 4 : Math.Min(warning, interval / 2);
    }

    internal Event Advance(double seconds)
    {
        if (seconds <= 0 || double.IsNaN(seconds) || double.IsInfinity(seconds)) return Event.None;
        elapsed += seconds;
        if (!warningPending)
        {
            if (elapsed < interval - warning) return Event.None;
            // A hitch crossing the entire warning window cannot hit invisibly.
            if (elapsed >= interval) elapsed = interval - warning;
            warningPending = true;
            StrikeId++;
            return Event.Warning;
        }

        if (elapsed < interval) return Event.None;
        // Keep normal frame rounding, without queued strikes after a hitch.
        elapsed = Math.Min(elapsed - interval, (interval - warning) / 2);
        warningPending = false;
        return Event.Impact;
    }
}
