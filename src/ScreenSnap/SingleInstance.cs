using System.Threading;

namespace ScreenSnap;

/// <summary>
/// Cross-process single-instance coordination. The first instance owns a named mutex
/// and listens on a named event; subsequent launches set the event (asking the running
/// instance to surface) and exit.
/// </summary>
internal static class SingleInstance
{
    private const string InstanceId = "7C9E6679-7425-40DE-944B-E07FC1F90AE7";

    public const string MutexName = $@"Local\ScreenSnap.SingleInstance.{InstanceId}";
    private const string EventName = $@"Local\ScreenSnap.Activate.{InstanceId}";

    /// <summary>Raised (on a background thread) when another launch asks this instance to surface.</summary>
    public static event Action? Activated;

    public static void StartListener()
    {
        var thread = new Thread(ListenLoop)
        {
            IsBackground = true,
            Name = "ScreenSnap.SingleInstanceListener",
        };
        thread.Start();
    }

    private static void ListenLoop()
    {
        using var handle = new EventWaitHandle(false, EventResetMode.AutoReset, EventName);
        while (true)
        {
            if (handle.WaitOne())
            {
                Activated?.Invoke();
            }
        }
    }

    public static void SignalFirstInstance()
    {
        try
        {
            using var handle = EventWaitHandle.OpenExisting(EventName);
            handle.Set();
        }
        catch (WaitHandleCannotBeOpenedException)
        {
            // The owning instance is shutting down; nothing to surface.
        }
    }
}
