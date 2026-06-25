using System;
using System.Threading.Tasks;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using QRCoder;
using uniffi.sylva_sdk;

namespace Ubivera.Sylva.Client
{
    /// <summary>
    /// Standalone window for enrolling a TOTP authenticator: shows the QR + manual
    /// key, takes a name + the confirming code. A separate, fixed-size window
    /// (Mica + extended title bar, matching the main app) so the QR and fields
    /// aren't clipped inside the small Hub window. <see cref="Completed"/> resolves
    /// <c>true</c> once an authenticator is added, <c>false</c> if cancelled.
    /// </summary>
    public sealed partial class AddAuthenticatorWindow : Window
    {
        private const double WidthDip = 420;
        private const double TitleBarDip = 40;

        private readonly TotpEnrollment _enrollment;
        private readonly TaskCompletionSource<bool> _tcs = new();

        public Task<bool> Completed => _tcs.Task;

        internal AddAuthenticatorWindow(TotpEnrollment enrollment)
        {
            InitializeComponent();
            _enrollment = enrollment;
            SecretBox.Text = enrollment.secretBase32;

            // Match the main window's chrome: Mica through an extended custom title
            // bar with the app logo; a fixed-size utility window (no resize/maximize).
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
            if (AppWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.IsResizable = false;
                presenter.IsMaximizable = false;
            }

            Body.Loaded += (_, _) => SizeToContentAndCenter();
            // Closing without a successful confirm resolves false (no-op if already true).
            Closed += (_, _) => _tcs.TrySetResult(false);
            _ = LoadQrAsync();
        }

        /// <summary>Fit the window to its content (no dead space) and center it on
        /// the current display. AppWindow sizes are physical pixels, so scale the
        /// measured DIPs by the display's rasterization scale.</summary>
        private void SizeToContentAndCenter()
        {
            var scale = Body.XamlRoot?.RasterizationScale ?? 1.0;
            Body.Measure(new Windows.Foundation.Size(WidthDip, double.PositiveInfinity));
            // +4 DIP covers the window's thin frame so the last control isn't clipped.
            var width = (int)Math.Ceiling(WidthDip * scale);
            var height = (int)Math.Ceiling((TitleBarDip + Body.DesiredSize.Height + 4) * scale);
            AppWindow.Resize(new Windows.Graphics.SizeInt32(width, height));

            var area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest);
            var x = area.WorkArea.X + Math.Max(0, (area.WorkArea.Width - width) / 2);
            var y = area.WorkArea.Y + Math.Max(0, (area.WorkArea.Height - height) / 2);
            AppWindow.Move(new Windows.Graphics.PointInt32(x, y));
        }

        private async Task LoadQrAsync()
        {
            try { QrImage.Source = await AvatarImaging.FromBytesAsync(QrPng(_enrollment.otpauthUri)); }
            catch { /* the QR is a convenience; the manual key still works */ }
        }

        private static byte[] QrPng(string otpauthUri)
        {
            var data = new QRCodeGenerator().CreateQrCode(otpauthUri, QRCodeGenerator.ECCLevel.M);
            return new PngByteQRCode(data).GetGraphic(8);
        }

        private void OnCancelClick(object sender, RoutedEventArgs e) => Close();

        private async void OnVerifyClick(object sender, RoutedEventArgs e)
        {
            // Read the controls on the UI thread BEFORE going to a background thread.
            var code = CodeBox.Text.Trim();
            var label = LabelBox.Text.Trim();
            if (code.Length == 0) { ShowError("Enter the code from your app."); return; }

            ErrorBar.IsOpen = false;
            VerifyButton.IsEnabled = false;
            try
            {
                await Task.Run(() => App.Client.ConfirmTotp(_enrollment.totpId, code, label));
                _tcs.TrySetResult(true);
                Close();
            }
            catch (ClientException ex)
            {
                ShowError(ex is ClientException.InvalidCode
                    ? "That code didn't match. Use the current code from your app."
                    : "Couldn't add the authenticator. Try again.");
                CodeBox.SelectAll();
            }
            catch (Exception ex) { ShowError(ex.Message); }
            finally { VerifyButton.IsEnabled = true; }
        }

        private void ShowError(string message)
        {
            ErrorBar.Title = "Couldn't add";
            ErrorBar.Message = message;
            ErrorBar.IsOpen = true;
        }
    }
}
