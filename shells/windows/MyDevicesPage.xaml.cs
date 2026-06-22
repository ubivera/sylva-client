using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using uniffi.sylva_sdk;

namespace Ubivera.Sylva.Client
{
    /// <summary>A row projected from the SDK's (internal) DeviceInfo for x:Bind.</summary>
    public record DeviceRow(string Label, string Detail, string DeviceId);

    /// <summary>
    /// This account's devices — list, revoke, sign out. The end of slice 1.
    /// Sign out keeps the device enrolled (return with just a password); "Forget
    /// this server" is the full wipe / reset.
    /// </summary>
    public sealed partial class MyDevicesPage : Page
    {
        public MyDevicesPage()
        {
            InitializeComponent();
            Loaded += (_, _) => _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            ErrorBar.IsOpen = false;
            Busy.IsActive = true;
            try
            {
                var devices = await Task.Run(() => App.Client.ListDevices());
                var rows = devices.Select(d => new DeviceRow(
                    d.label,
                    $"{d.platform} · added {d.createdAt}" + (d.revoked ? " · revoked" : ""),
                    d.deviceId)).ToList();
                DeviceList.ItemsSource = rows;
                EmptyText.Visibility = rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (ClientException ex) { ShowError(Describe(ex)); }
            catch (Exception ex) { ShowError(ex.Message); }
            finally { Busy.IsActive = false; }
        }

        private async void OnRevokeClick(object sender, RoutedEventArgs e)
        {
            var id = (string)((Button)sender).Tag;
            try { await Task.Run(() => App.Client.RevokeDevice(id)); }
            catch (ClientException ex) { ShowError(Describe(ex)); return; }
            catch (Exception ex) { ShowError(ex.Message); return; }
            await LoadAsync();
        }

        private async void OnSignOutClick(object sender, RoutedEventArgs e)
        {
            try { await Task.Run(() => App.Client.SignOut()); }
            catch { /* best effort — we're clearing local state and leaving anyway */ }
            Frame.Navigate(typeof(ConnectPage));
        }

        private async void OnForgetClick(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                XamlRoot = XamlRoot,
                Title = "Forget this server?",
                Content = "This removes the account and server from this device. "
                    + "You'll need your Secret Key to sign in again.",
                PrimaryButtonText = "Forget",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
            };
            if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;

            try { await Task.Run(() => App.Client.ForgetServer()); }
            catch { /* best effort — we're wiping local state and leaving anyway */ }
            Frame.Navigate(typeof(ConnectPage));
        }

        private void ShowError(string message)
        {
            ErrorBar.Title = "Something went wrong";
            ErrorBar.Message = message;
            ErrorBar.IsOpen = true;
        }

        private static string Describe(ClientException ex) => ex switch
        {
            ClientException.NotSignedIn => "You're not signed in — sign in again.",
            ClientException.Server => "The server couldn't complete that request.",
            _ => "Something went wrong. Try again.",
        };
    }
}
