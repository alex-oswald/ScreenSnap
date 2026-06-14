using System.Threading;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace ScreenSnap;

/// <summary>
/// Custom application entry point. We replace the XAML-generated <c>Main</c>
/// (see <c>DISABLE_XAML_GENERATED_MAIN</c>) so we can enforce a single running
/// instance before the Windows App SDK runtime spins up.
/// </summary>
public static class Program
{
    private static Mutex? _instanceMutex;

    [STAThread]
    private static void Main()
    {
        _instanceMutex = new Mutex(initiallyOwned: true, SingleInstance.MutexName, out bool isNewInstance);
        if (!isNewInstance)
        {
            // Another instance is already running; ask it to surface, then exit.
            SingleInstance.SignalFirstInstance();
            return;
        }

        WinRT.ComWrappersSupport.InitializeComWrappers();

        Application.Start(p =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            _ = new App();
        });

        GC.KeepAlive(_instanceMutex);
    }
}
