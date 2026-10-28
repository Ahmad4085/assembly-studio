using System.Windows.Threading;

namespace AssemblyStudio.App.Tests;

/// <summary>
/// One shared STA thread with a running dispatcher for WPF tests. The application object (and its
/// theme resources) is created once on it, since WPF allows a single <see cref="System.Windows.Application"/>
/// per process. Exceptions thrown by a test body propagate to the calling test thread.
/// </summary>
internal static class StaTestThread
{
    private static readonly Lazy<Dispatcher> Dispatcher = new(Start);

    public static void Run(Action body) => Dispatcher.Value.Invoke(body);

    private static Dispatcher Start()
    {
        Dispatcher? dispatcher = null;
        using var ready = new ManualResetEventSlim();
        var thread = new Thread(() =>
        {
            dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
            var app = new App { ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown };
            app.InitializeComponent();
            ready.Set();
            System.Windows.Threading.Dispatcher.Run();
        })
        {
            IsBackground = true,
            Name = "WPF test STA thread",
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        ready.Wait();
        return dispatcher!;
    }
}
