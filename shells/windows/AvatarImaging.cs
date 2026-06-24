using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Ubivera.Sylva.Client
{
    /// <summary>
    /// Avatar image processing — all client-side, because the server is
    /// zero-knowledge and never sees the pixels (it stores only the sealed blob).
    /// <see cref="NormalizeToPngAsync"/> decodes any supported input, center-crops
    /// to a square, scales to a fixed edge, and re-encodes PNG; the re-encode is
    /// also what strips EXIF/metadata (e.g. GPS, camera model, timestamps).
    /// </summary>
    internal static class AvatarImaging
    {
        /// <summary>Decode → center-crop square → scale to <paramref name="edge"/>px
        /// → PNG bytes. Honors EXIF orientation, then discards it on re-encode.</summary>
        public static async Task<byte[]> NormalizeToPngAsync(IRandomAccessStream input, uint edge)
        {
            var decoder = await BitmapDecoder.CreateAsync(input);
            uint w = decoder.PixelWidth, h = decoder.PixelHeight;
            uint shorter = Math.Min(w, h);

            // BitmapTransform applies scale BEFORE crop, so work in scaled coords:
            // scale the shorter side to `edge`, then crop the centered edge×edge square.
            double scale = (double)edge / shorter;
            uint scaledW = Math.Max(edge, (uint)Math.Round(w * scale));
            uint scaledH = Math.Max(edge, (uint)Math.Round(h * scale));
            var transform = new BitmapTransform
            {
                ScaledWidth = scaledW,
                ScaledHeight = scaledH,
                InterpolationMode = BitmapInterpolationMode.Fant,
                Bounds = new BitmapBounds
                {
                    X = (scaledW - edge) / 2,
                    Y = (scaledH - edge) / 2,
                    Width = edge,
                    Height = edge,
                },
            };

            var pixels = await decoder.GetPixelDataAsync(
                BitmapPixelFormat.Rgba8,
                BitmapAlphaMode.Straight,
                transform,
                ExifOrientationMode.RespectExifOrientation,
                ColorManagementMode.ColorManageToSRgb);

            using var outStream = new InMemoryRandomAccessStream();
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, outStream);
            encoder.SetPixelData(BitmapPixelFormat.Rgba8, BitmapAlphaMode.Straight,
                edge, edge, 96, 96, pixels.DetachPixelData());
            await encoder.FlushAsync();

            var bytes = new byte[outStream.Size];
            outStream.Seek(0);
            await outStream.ReadAsync(bytes.AsBuffer(), (uint)outStream.Size, InputStreamOptions.None);
            return bytes;
        }

        /// <summary>A WinUI image source from decoded image bytes (for display).</summary>
        public static async Task<ImageSource> FromBytesAsync(byte[] bytes)
        {
            using var stream = new InMemoryRandomAccessStream();
            await stream.WriteAsync(bytes.AsBuffer());
            stream.Seek(0);
            var image = new BitmapImage();
            await image.SetSourceAsync(stream);
            return image;
        }
    }
}
