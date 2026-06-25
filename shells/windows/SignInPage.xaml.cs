using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using uniffi.sylva_sdk;

namespace Ubivera.Sylva.Client
{
    /// <summary>
    /// Sign in (+ unlock) to an existing account. Secret Key is required the first
    /// time on a new device; blank reuses this device's keychain-cached key.
    /// </summary>
    public sealed partial class SignInPage : Page
    {
        public SignInPage()
        {
            InitializeComponent();
        }

        private async void OnSignInClick(object sender, RoutedEventArgs e)
        {
            var email = EmailBox.Text.Trim();
            var password = PasswordBox.Password;
            if (email.Length == 0 || password.Length == 0)
            {
                ShowError("Enter your email and password.");
                return;
            }
            var secret = SecretKeyBox.Text.Trim();
            var secretKey = secret.Length == 0 ? null : secret;

            ErrorBar.IsOpen = false;
            SetBusy(true);
            try
            {
                var outcome = await Task.Run(() => App.Client.SignIn(email, password, secretKey));
                if (outcome is SignInOutcome.Success)
                {
                    Frame.Navigate(typeof(ShellPage));
                }
                else // MfaRequired — swap to the second-factor step
                {
                    FormPanel.Visibility = Visibility.Collapsed;
                    MfaPanel.Visibility = Visibility.Visible;
                    MfaCodeBox.Focus(FocusState.Programmatic);
                }
            }
            catch (ClientException ex) { ShowError(Describe(ex)); }
            catch (Exception ex) { ShowError(ex.Message); }
            finally { SetBusy(false); }
        }

        private async void OnVerifyMfaClick(object sender, RoutedEventArgs e)
        {
            var code = MfaCodeBox.Text.Trim();
            if (code.Length == 0) { ShowMfaError("Enter the code from your authenticator."); return; }

            MfaErrorBar.IsOpen = false;
            SetMfaBusy(true);
            try
            {
                var outcome = await Task.Run(() => App.Client.SubmitMfa(code));
                if (outcome is SignInOutcome.Success)
                {
                    Frame.Navigate(typeof(ShellPage));
                }
            }
            catch (ClientException ex) { ShowMfaError(DescribeMfa(ex)); MfaCodeBox.SelectAll(); }
            catch (Exception ex) { ShowMfaError(ex.Message); }
            finally { SetMfaBusy(false); }
        }

        private void ShowError(string message)
        {
            ErrorBar.Title = "Couldn't sign in";
            ErrorBar.Message = message;
            ErrorBar.IsOpen = true;
        }

        private void ShowMfaError(string message)
        {
            MfaErrorBar.Title = "Couldn't verify";
            MfaErrorBar.Message = message;
            MfaErrorBar.IsOpen = true;
        }

        private void SetMfaBusy(bool busy)
        {
            MfaBusy.IsActive = busy;
            VerifyButton.IsEnabled = !busy;
        }

        private static string DescribeMfa(ClientException ex) => ex switch
        {
            ClientException.InvalidCode => "That code didn't match. Use the current code from your app.",
            _ => "Couldn't verify the code. Try again.",
        };

        private void SetBusy(bool busy)
        {
            Busy.IsActive = busy;
            SubmitButton.IsEnabled = !busy;
        }

        private static string Describe(ClientException ex) => ex switch
        {
            ClientException.Crypto => "Wrong password or Secret Key.",
            ClientException.SecretKeyRequired =>
                "This device isn't enrolled yet — enter the Secret Key from a device you're already signed in on.",
            ClientException.NotConnected => "Not connected to a server — go back and connect first.",
            _ => "Couldn't sign in. Check the server and try again.",
        };
    }
}
