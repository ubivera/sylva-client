using Microsoft.UI.Xaml;
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
