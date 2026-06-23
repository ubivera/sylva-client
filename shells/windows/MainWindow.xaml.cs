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
            App.RootNavFrame = RootFrame;
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
