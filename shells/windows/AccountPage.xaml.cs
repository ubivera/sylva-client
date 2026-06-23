using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using uniffi.sylva_sdk;

namespace Ubivera.Sylva.Client
{
    /// <summary>
    /// The app's landing page: a profile header (avatar + name + email) plus
    /// collapsible ribbons for what the user can change — avatar (soon), display
    /// name, email, password. Session actions (sign out / forget) live in the
    /// shell's nav footer. Mirrors the Windows Settings / PC Manager layout.
    /// </summary>
    public sealed partial class AccountPage : Page
    {
        public AccountPage()
        {
            InitializeComponent();
            Loaded += (_, _) => _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            try
            {
                var profile = await Task.Run(() => App.Client.GetProfile());
                Avatar.DisplayName = profile.displayName;
                NameText.Text = profile.displayName;
                EmailText.Text = profile.email;
                DisplayNameBox.Text = profile.displayName;
                CurrentEmailText.Text = $"Current: {profile.email}";
            }
            catch (Exception ex) { ShowError(ex is ClientException c ? Describe(c) : ex.Message); }
        }

        private async void OnSaveNameClick(object sender, RoutedEventArgs e)
        {
            var name = DisplayNameBox.Text.Trim();
            if (name.Length == 0) { ShowError("Enter a display name."); return; }
            var updated = await Run(SaveNameButton, () => App.Client.UpdateDisplayName(name), "Display name updated.");
            if (updated is not null)
            {
                NameText.Text = updated.displayName;
                Avatar.DisplayName = updated.displayName;
            }
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
            var updated = await Run(ChangeEmailButton, () => App.Client.UpdateEmail(email, password), "Email updated.");
            if (updated is not null)
            {
                EmailText.Text = updated.email;
                CurrentEmailText.Text = $"Current: {updated.email}";
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
            if (await RunVoid(ChangePwButton, () => App.Client.ChangePassword(current, next), "Password changed."))
            {
                CurrentPwBox.Password = "";
                NewPwBox.Password = "";
                ConfirmPwBox.Password = "";
            }
        }

        /// <summary>Run a profile-returning SDK call off the UI thread; report result.
        /// Returns the updated profile on success, else null.</summary>
        private async Task<Profile?> Run(Button button, Func<Profile> call, string success)
        {
            StatusBar.IsOpen = false;
            button.IsEnabled = false;
            try
            {
                var profile = await Task.Run(call);
                ShowSuccess(success);
                return profile;
            }
            catch (ClientException ex) { ShowError(Describe(ex)); return null; }
            catch (Exception ex) { ShowError(ex.Message); return null; }
            finally { button.IsEnabled = true; }
        }

        private async Task<bool> RunVoid(Button button, Action call, string success)
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
