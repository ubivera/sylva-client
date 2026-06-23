using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Ubivera.Sylva.Client
{
    /// <summary>
    /// The signed-in shell: a <see cref="NavigationView"/> over a content frame.
    /// The slice-1 flow navigates here on sign-in / create-owner success. The
    /// nav items (Account / Devices) drive the inner frame; the pane footer
    /// shows the current identity and hosts the session actions (sign out /
    /// forget), which re-navigate the root frame back to Connect.
    /// </summary>
    public sealed partial class ShellPage : Page
    {
        public ShellPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Select Account on load — this fires SelectionChanged, which navigates.
            Nav.SelectedItem = Nav.MenuItems[0];
            _ = LoadFooterIdentityAsync();

            // Show the footer name only when the pane is expanded; collapsed shows
            // just the avatar, centered like the nav icons above it.
            UpdateFooterPane();
            Nav.RegisterPropertyChangedCallback(NavigationView.IsPaneOpenProperty, (_, _) => UpdateFooterPane());
        }

        private void UpdateFooterPane()
        {
            if (Nav.IsPaneOpen)
            {
                // Expanded pane: avatar + name, left-aligned like the nav items.
                FooterName.Visibility = Visibility.Visible;
                AccountButton.Width = double.NaN;
                AccountButton.HorizontalAlignment = HorizontalAlignment.Stretch;
                AccountButton.HorizontalContentAlignment = HorizontalAlignment.Left;
                AccountButton.Padding = new Thickness(16, 8, 8, 8);
            }
            else
            {
                // Collapsed rail: a rail-width button so the centered avatar lands
                // dead-center under the nav icons; no name.
                FooterName.Visibility = Visibility.Collapsed;
                AccountButton.Width = Nav.CompactPaneLength;
                AccountButton.HorizontalAlignment = HorizontalAlignment.Left;
                AccountButton.HorizontalContentAlignment = HorizontalAlignment.Center;
                AccountButton.Padding = new Thickness(0, 8, 0, 8);
            }
        }

        private async Task LoadFooterIdentityAsync()
        {
            try
            {
                var profile = await Task.Run(() => App.Client.GetProfile());
                FooterAvatar.DisplayName = profile.displayName;
                FooterName.Text = profile.displayName;
            }
            catch
            {
                // Footer identity is best-effort chrome; the Account page surfaces
                // real profile/load errors.
            }
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
                _ => typeof(AccountPage),
            };
            ContentFrame.Navigate(page);
            // Collapse the temporarily-expanded overlay pane back to the icon rail
            // once a page is chosen.
            Nav.IsPaneOpen = false;
        }

        private async void OnSignOutClick(object sender, RoutedEventArgs e)
        {
            try { await Task.Run(() => App.Client.SignOut()); }
            catch { /* best effort — leaving the signed-in shell regardless */ }
            App.RootNavFrame?.Navigate(typeof(ConnectPage));
        }

        private async void OnForgetClick(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                XamlRoot = XamlRoot,
                Title = "Forget this server?",
                Content = "This removes the account and server pin from this device. "
                    + "You'll need your Secret Key to sign in again.",
                PrimaryButtonText = "Forget",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
            };
            if (await dialog.ShowAsync() != ContentDialogResult.Primary)
            {
                return;
            }
            try { await Task.Run(() => App.Client.ForgetServer()); }
            catch { /* best effort */ }
            App.RootNavFrame?.Navigate(typeof(ConnectPage));
        }
    }
}
