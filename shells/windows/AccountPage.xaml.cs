using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using uniffi.sylva_sdk;
using Windows.Storage;
using Windows.Storage.Pickers;

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
        private readonly ObservableCollection<AuthenticatorVm> _authenticators = new();

        public AccountPage()
        {
            InitializeComponent();
            TotpList.ItemsSource = _authenticators;
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
            await LoadAvatarAsync();
            await RefreshTotpAsync();
        }

        /// <summary>Decrypt and show the stored avatar; falls back to initials if
        /// there's none yet (best-effort — never blocks the page).</summary>
        private async Task LoadAvatarAsync()
        {
            try
            {
                var bytes = await Task.Run(() => App.Client.GetAvatar());
                if (bytes is { Length: > 0 })
                {
                    Avatar.ProfilePicture = await AvatarImaging.FromBytesAsync(bytes);
                }
            }
            catch { /* no avatar / not decryptable here — initials stay */ }
        }

        private async void OnChoosePhotoClick(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker { SuggestedStartLocation = PickerLocationId.PicturesLibrary };
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".webp");
            WinRT.Interop.InitializeWithWindow.Initialize(picker, App.MainWindowHandle);

            var file = await picker.PickSingleFileAsync();
            if (file is null) { return; }

            StatusBar.IsOpen = false;
            ChoosePhotoButton.IsEnabled = false;
            try
            {
                byte[] png;
                using (var stream = await file.OpenAsync(FileAccessMode.Read))
                {
                    png = await AvatarImaging.NormalizeToPngAsync(stream, 512);
                }
                await Task.Run(() => App.Client.SetAvatar(png));
                Avatar.ProfilePicture = await AvatarImaging.FromBytesAsync(png);
                App.NotifyAvatarChanged();
                ShowSuccess("Avatar updated.");
            }
            catch (ClientException ex) { ShowError(Describe(ex)); }
            catch (Exception ex) { ShowError(ex.Message); }
            finally { ChoosePhotoButton.IsEnabled = true; }
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

        // ── Two-step verification (TOTP) ────────────────────────────────────────

        private async Task RefreshTotpAsync()
        {
            try
            {
                var factors = await Task.Run(() => App.Client.ListTotp());
                _authenticators.Clear();
                foreach (var f in factors)
                {
                    var label = string.IsNullOrWhiteSpace(f.label) ? "Authenticator" : f.label;
                    _authenticators.Add(new AuthenticatorVm(f.totpId, label, FormatAdded(f.createdAt)));
                }
                NoTotpText.Visibility = _authenticators.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch { /* leave the list as-is — the rest of the page still works */ }
        }

        private async void OnAddTotpClick(object sender, RoutedEventArgs e)
        {
            StatusBar.IsOpen = false;
            TotpEnrollment enrollment;
            try { enrollment = await Task.Run(() => App.Client.EnrollTotp()); }
            catch (ClientException ex) { ShowError(Describe(ex)); return; }
            catch (Exception ex) { ShowError(ex.Message); return; }

            var window = new AddAuthenticatorWindow(enrollment);
            window.Activate();
            if (await window.Completed)
            {
                ShowSuccess("Authenticator added.");
                await RefreshTotpAsync();
            }
        }

        private async void OnRemoveTotpClick(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not string totpId) { return; }
            var confirm = new ContentDialog
            {
                XamlRoot = XamlRoot,
                Title = "Remove authenticator?",
                Content = "You won't be able to use this app for sign-in codes anymore.",
                PrimaryButtonText = "Remove",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
            };
            if (await confirm.ShowAsync() != ContentDialogResult.Primary) { return; }
            try
            {
                await Task.Run(() => App.Client.RemoveTotp(totpId));
                ShowSuccess("Authenticator removed.");
                await RefreshTotpAsync();
            }
            catch (ClientException ex) { ShowError(Describe(ex)); }
            catch (Exception ex) { ShowError(ex.Message); }
        }

        private static string FormatAdded(string rfc3339) =>
            DateTimeOffset.TryParse(rfc3339, out var dt) ? $"Added {dt.LocalDateTime:d}" : "Added";

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

    /// <summary>Row VM for the authenticator list (clean PascalCase for x:Bind).</summary>
    internal sealed record AuthenticatorVm(string Id, string Label, string Added);
}
