using System.Threading.Tasks;
using Microsoft.UI.Xaml;

namespace Ubivera.Sylva.Client
{
    /// <summary>
    /// The app's single window. Hosts a <see cref="Frame"/> the slice-1 flow
    /// navigates through. On launch it tries to resume a cached session
    /// (already-enrolled -> My devices); otherwise it opens on Connect.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Mica through a custom, extended title bar (the AppTitleBar row) — the
            // PC-Manager look. Caption buttons stay system-drawn on the right.
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
            AppWindow.Resize(new Windows.Graphics.SizeInt32(930, 540));

            App.RootNavFrame = RootFrame;
            App.MainWindowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
            _ = StartAsync();
        }

        private async Task StartAsync()
        {
            string? userId = null;
            try { userId = await Task.Run(() => App.Client.Restore()); }
            catch { /* couldn't resume (offline, identity changed, nothing cached) -> Connect */ }
            Splash.Visibility = Visibility.Collapsed;
            RootFrame.Navigate(userId != null ? typeof(ShellPage) : typeof(ConnectPage));
        }
    }
}
