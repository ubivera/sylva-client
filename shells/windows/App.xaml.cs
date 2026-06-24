using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using uniffi.sylva_sdk;

namespace Ubivera.Sylva.Client
{
    /// <summary>
    /// Application entry point. Owns the single, app-lifetime <see cref="SylvaClient"/>
    /// handle (the blocking SDK facade) that every page calls into.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// The shared SDK handle. Its secrets live in the OS keychain under the
        /// service name below, scoped to the current OS user.
        /// </summary>
        internal static SylvaClient Client { get; private set; } = null!;

        /// <summary>The window's root navigation frame, so shell pages can exit to
        /// Connect on sign-out / forget. Set by <see cref="MainWindow"/>.</summary>
        internal static Frame? RootNavFrame { get; set; }

        /// <summary>The main window's HWND, needed to parent WinRT pickers (e.g. the
        /// avatar file picker) in a packaged desktop app. Set by <see cref="MainWindow"/>.</summary>
        internal static nint MainWindowHandle { get; set; }

        /// <summary>Raised after the avatar changes so persistent chrome (the nav
        /// footer avatar) can refresh without a reload.</summary>
        internal static event Action? AvatarChanged;

        internal static void NotifyAvatarChanged() => AvatarChanged?.Invoke();

        private Window? _window;

        public App()
        {
            InitializeComponent();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            Client = new SylvaClient("sylva-client");
            _window = new MainWindow();
            _window.Activate();
        }
    }
}
