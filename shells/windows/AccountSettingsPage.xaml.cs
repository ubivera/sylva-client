using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using uniffi.sylva_sdk;

namespace Ubivera.Sylva.Client
{
    /// <summary>
    /// Account self-service: change display name, email (current-password re-auth),
    /// and password (the SDK does the 2SKD re-wrap). Also hosts the session
    /// actions — sign out (keeps the device enrolled) and forget-this-server
    /// (full wipe) — which exit the shell via the root frame.
    /// </summary>
    public sealed partial class AccountSettingsPage : Page
    {
        public AccountSettingsPage()
        {
            InitializeComponent();
            Loaded += (_, _) => _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            try
            {
                var profile = await Task.Run(() => App.Client.GetProfile());
                DisplayNameBox.Text = profile.displayName;
                CurrentEmailText.Text = $"Current: {profile.email}";
            }
            catch (Exception ex) { ShowError(ex is ClientException c ? Describe(c) : ex.Message); }
        }

        private async void OnSaveNameClick(object sender, RoutedEventArgs e)
        {
            var name = DisplayNameBox.Text.Trim();
            if (name.Length == 0) { ShowError("Enter a display name."); return; }
            await Run(SaveNameButton, () => App.Client.UpdateDisplayName(name), "Display name updated.");
        }

        private async void OnChangeEmailClick(object sender, RoutedEventArgs e)
        {
            var email = NewEmailBox.Text.Trim();
            var password = EmailPasswordBox.Password;
            if (email.Length == 0 || password.Length == 0)
            {
                ShowError("Enter the new email and your current password.");
                return;
            }
            if (await Run(ChangeEmailButton, () => App.Client.UpdateEmail(email, password), "Email updated."))
            {
                CurrentEmailText.Text = $"Current: {email}";
                NewEmailBox.Text = "";
                EmailPasswordBox.Password = "";
            }
        }

        private async void OnChangePasswordClick(object sender, RoutedEventArgs e)
        {
            var current = CurrentPwBox.Password;
            var next = NewPwBox.Password;
            var confirm = ConfirmPwBox.Password;
            if (current.Length == 0 || next.Length == 0)
            {
                ShowError("Enter your current and new password.");
                return;
            }
            if (next != confirm) { ShowError("The new passwords don't match."); return; }
            if (await Run(ChangePwButton, () => App.Client.ChangePassword(current, next), "Password changed."))
            {
                CurrentPwBox.Password = "";
                NewPwBox.Password = "";
                ConfirmPwBox.Password = "";
            }
        }

        private async void OnSignOutClick(object sender, RoutedEventArgs e)
        {
            try { await Task.Run(() => App.Client.SignOut()); }
            catch { /* best effort — leaving anyway */ }
            App.RootNavFrame?.Navigate(typeof(ConnectPage));
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
            catch { /* best effort */ }
            App.RootNavFrame?.Navigate(typeof(ConnectPage));
        }

        /// <summary>Run a blocking SDK call off the UI thread; report success/error.
        /// Returns whether it succeeded (so callers can clear fields).</summary>
        private async Task<bool> Run(Button button, Action call, string success)
        {
            StatusBar.IsOpen = false;
            button.IsEnabled = false;
            try
            {
                await Task.Run(call);
                ShowSuccess(success);
                return true;
            }
            catch (ClientException ex) { ShowError(Describe(ex)); return false; }
            catch (Exception ex) { ShowError(ex.Message); return false; }
            finally { button.IsEnabled = true; }
        }

        private void ShowSuccess(string message)
        {
            StatusBar.Severity = InfoBarSeverity.Success;
            StatusBar.Title = "Done";
            StatusBar.Message = message;
            StatusBar.IsOpen = true;
        }

        private void ShowError(string message)
        {
            StatusBar.Severity = InfoBarSeverity.Error;
            StatusBar.Title = "Couldn't save";
            StatusBar.Message = message;
            StatusBar.IsOpen = true;
        }

        private static string Describe(ClientException ex) => ex switch
        {
            ClientException.Crypto => "Wrong password.",
            ClientException.NotSignedIn => "You're not signed in — sign in again.",
            _ => "The server couldn't complete that (the email may already be in use).",
        };
    }
}
