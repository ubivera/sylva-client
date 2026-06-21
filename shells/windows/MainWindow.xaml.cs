using Microsoft.UI.Xaml;

namespace Ubivera.Sylva.Client
{
    /// <summary>
    /// The app's single window. Hosts a <see cref="Frame"/> that the slice-1 flow
    /// navigates through. For now it always opens on <see cref="ConnectPage"/>;
    /// launch-restore routing (already-enrolled -> my-devices) lands in a later checkpoint.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            RootFrame.Navigate(typeof(ConnectPage));
        }
    }
}
