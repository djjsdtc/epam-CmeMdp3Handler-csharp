using System;
using System.Threading;

namespace Epam.CmeMdp3Handler.Service
{
    /// <summary>
    /// Default scheduler to use in MDP Handler for scheduled tasks in case if user application
    /// does not provide with own service.
    /// Replaces Java's Executors.newScheduledThreadPool(1) with a Timer-based scheduler.
    /// Provides a shared timer for channel idle-check callbacks.
    /// </summary>
    public static class DefaultScheduledServiceHolder
    {
        private static readonly Lazy<SchedulerHolder> _instance = new(() => new SchedulerHolder());

        public static SchedulerHolder GetScheduler() => _instance.Value;

        public class SchedulerHolder : IDisposable
        {
            private bool _disposed;

            // Schedule a recurring action with an initial delay and period (in milliseconds).
            public IDisposable ScheduleWithFixedDelay(Action action, int initialDelayMs, int periodMs)
            {
                var timer = new Timer(_ => action(), null, initialDelayMs, periodMs);
                return timer;
            }

            public void Dispose()
            {
                _disposed = true;
            }
        }
    }
}
