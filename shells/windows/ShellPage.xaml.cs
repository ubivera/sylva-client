using Microsoft.UI.Xaml.Controls;

namespace Ubivera.Sylva.Client
{
    /// <summary>
    /// The signed-in shell: a <see cref="NavigationView"/> over a content frame.
    /// The slice-1 flow navigates here on sign-in / create-owner success. The
    /// nav items (Home / Devices / Account) drive the inner frame; sign-out and
    /// forget-server live on the Account page and re-navigate the root frame.
    /// </summary>
    public sealed partial class ShellPage : Page
    {
        public ShellPage()
        {
            InitializeComponent();
            // Select Home on load — this fires SelectionChanged, which navigates.
            Loaded += (_, _) => Nav.SelectedItem = Nav.MenuItems[0];
        }

        private void OnNavSelectionChanged(
            NavigationView sender,
            NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is not NavigationViewItem item)
            {
                return;
            }
            var page = item.Tag switch
            {
                "devices" => typeof(MyDevicesPage),
                "account" => typeof(AccountSettingsPage),
                _ => typeof(HomePage),
            };
            ContentFrame.Navigate(page);
        }
    }
}
