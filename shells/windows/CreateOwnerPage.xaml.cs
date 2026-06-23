using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;
using uniffi.sylva_sdk;

namespace Ubivera.Sylva.Client
{
    /// <summary>
    /// Create the first owner on a fresh server. All key material is generated
    /// client-side; the returned Secret Key is shown once for write-down (it is
    /// not recoverable) before the user can continue.
    /// </summary>
    public sealed partial class CreateOwnerPage : Page
    {
        public CreateOwnerPage()
        {
            InitializeComponent();
            DeviceLabelBox.Text = Environment.MachineName;
        }

        private async void OnCreateClick(object sender, RoutedEventArgs e)
        {
            var email = EmailBox.Text.Trim();
            var name = DisplayNameBox.Text.Trim();
            var password = PasswordBox.Password;
            var label = DeviceLabelBox.Text.Trim();
            if (email.Length == 0 || name.Length == 0 || password.Length == 0 || label.Length == 0)
            {
                ShowError("Fill in every field.");
                return;
            }

            ErrorBar.IsOpen = false;
            SetBusy(true);
            try
            {
                var enrollment = await Task.Run(() => App.Client.CreateOwner(email, name, password, label));
                SecretKeyText.Text = enrollment.secretKey;
                FormPanel.Visibility = Visibility.Collapsed;
                SecretPanel.Visibility = Visibility.Visible;
            }
            catch (ClientException ex) { ShowError(Describe(ex)); }
            catch (Exception ex) { ShowError(ex.Message); }
            finally { SetBusy(false); }
        }

        private void OnCopyClick(object sender, RoutedEventArgs e)
        {
            var data = new DataPackage();
            data.SetText(SecretKeyText.Text);
            Clipboard.SetContent(data);
        }

        private void OnSavedToggled(object sender, RoutedEventArgs e)
            => ContinueButton.IsEnabled = SavedCheck.IsChecked == true;

        private void OnContinueClick(object sender, RoutedEventArgs e)
            => Frame.Navigate(typeof(ShellPage));

        private void ShowError(string message)
        {
            ErrorBar.Title = "Couldn't create owner";
            ErrorBar.Message = message;
            ErrorBar.IsOpen = true;
        }

        private void SetBusy(bool busy)
        {
            Busy.IsActive = busy;
            SubmitButton.IsEnabled = !busy;
        }

        private static string Describe(ClientException ex) => ex switch
        {
            ClientException.NotConnected => "Not connected to a server — go back and connect first.",
            ClientException.Connect => "Couldn't reach the server.",
            _ => "Couldn't create the owner — the server may already have one (try Sign in instead), or it's unreachable.",
        };
    }
}
