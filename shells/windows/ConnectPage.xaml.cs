using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using uniffi.sylva_sdk;

namespace Ubivera.Sylva.Client
{
    /// <summary>
    /// First screen: point the app at a Sylva server. Calls the blocking
    /// <see cref="SylvaClient.Connect"/> on a background thread (discover -> verify ->
    /// TOFU-pin) and shows the verified server identity. Sign-in / create-owner follow.
    /// </summary>
    public sealed partial class ConnectPage : Page
    {
        public ConnectPage()
        {
            InitializeComponent();
        }

        private async void OnConnectClick(object sender, RoutedEventArgs e)
        {
            var host = HostBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(host))
            {
                ShowError("Enter a server host.");
                return;
            }

            var portValue = PortBox.Value;
            if (double.IsNaN(portValue) || portValue < 1 || portValue > 65535)
            {
                ShowError("Enter a valid discovery port (1–65535).");
                return;
            }
            var port = (ushort)portValue;

            ErrorBar.IsOpen = false;
            ResultPanel.Visibility = Visibility.Collapsed;
            SetBusy(true);
            try
            {
                // SylvaClient.Connect is blocking (the Rust side block_on()s the async
                // facade), so run it off the UI thread; the await marshals back.
                var info = await Task.Run(() => App.Client.Connect(host, port));
                ShowResult(info);
            }
            catch (ClientException ex)
            {
                ShowError(Describe(ex));
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void ShowResult(ConnectInfo info)
        {
            TrustText.Text = info.trust == TrustStatus.FirstContact
                ? "First contact — identity pinned to this device. Verify the fingerprint below matches your server."
                : "Known server — identity matches the pinned key.";
            ServerNameText.Text = $"Server: {info.serverName}   (gRPC port {info.grpcPort})";
            FingerprintText.Text = info.identityFingerprint;
            ResultPanel.Visibility = Visibility.Visible;
        }

        private void ShowError(string message)
        {
            ErrorBar.Title = "Couldn't connect";
            ErrorBar.Message = message;
            ErrorBar.IsOpen = true;
        }

        private void SetBusy(bool busy)
        {
            Busy.IsActive = busy;
            ConnectButton.IsEnabled = !busy;
        }

        private static string Describe(ClientException ex) => ex switch
        {
            ClientException.Connect =>
                "Couldn't reach or verify that server. Check the host and port, and that the server is running.",
            ClientException.IdentityMismatch =>
                "This server's identity doesn't match the key pinned earlier — possible reinstall or interception. Refusing to connect.",
            _ => "Couldn't connect to the server.",
        };

        private void OnSignInClick(object sender, RoutedEventArgs e) => Frame.Navigate(typeof(SignInPage));
        private void OnCreateOwnerClick(object sender, RoutedEventArgs e) => Frame.Navigate(typeof(CreateOwnerPage));
    }
}
