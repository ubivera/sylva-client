using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using uniffi.sylva_sdk;

namespace Ubivera.Sylva.Client
{
    /// <summary>
    /// The signed-in landing page: welcomes the user with their avatar (initials
    /// for now), display name, and email — read from the SDK's <c>GetProfile</c>.
    /// </summary>
    public sealed partial class HomePage : Page
    {
        public HomePage()
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
                var profile = await Task.Run(() => App.Client.GetProfile());
                Avatar.DisplayName = profile.displayName;
                WelcomeText.Text = $"Welcome, {profile.displayName}";
                EmailText.Text = profile.email;
            }
            catch (ClientException ex) { ShowError(Describe(ex)); }
            catch (Exception ex) { ShowError(ex.Message); }
            finally { Busy.IsActive = false; }
        }

        private void ShowError(string message)
        {
            ErrorBar.Title = "Couldn't load your profile";
            ErrorBar.Message = message;
            ErrorBar.IsOpen = true;
        }

        private static string Describe(ClientException ex) => ex switch
        {
            ClientException.NotSignedIn => "You're not signed in — sign in again.",
            _ => "Couldn't reach the server. Try again.",
        };
    }
}
