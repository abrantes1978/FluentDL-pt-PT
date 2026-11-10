using PhotoSauce.MagicScaler;
using PhotoSauce.NativeCodecs.Libwebp;

namespace FluentDL.Core.Helpers;

/// <summary>Creates a normalized square JPEG for embedding as audio cover art.</summary>
internal static class ArtworkProcessor
{
    private const int CoverSize = 600;
    private const int MaximumEncodedBytes = 32 * 1024 * 1024;
    private const ulong MaximumPixels = 64UL * 1024 * 1024;
    private const uint MaximumDimension = 32768;

    static ArtworkProcessor() => CodecManager.Configure(codecs => codecs.UseLibwebp());

    internal static Task<byte[]> CreateSquareJpegAsync(byte[] source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source.Length == 0 || source.Length > MaximumEncodedBytes)
            throw new InvalidDataException("Album artwork is empty or exceeds the supported file size.");

        return Task.Run(() =>
        {
            using var input = new MemoryStream(source, writable: false);
            var frame = ImageFileInfo.Load(input).Frames[0];
            var width = frame.Width;
            var height = frame.Height;

            if (width <= 0 || height <= 0 || width > MaximumDimension || height > MaximumDimension
                || (ulong)width * (ulong)height > MaximumPixels)
                throw new InvalidDataException("Album artwork exceeds the supported image dimensions.");

            var settings = new ProcessImageSettings
            {
                Width = CoverSize,
                Height = CoverSize,
                ResizeMode = CropScaleMode.Crop,
                Anchor = CropAnchor.Center,
                Interpolation = InterpolationSettings.Lanczos,
                Sharpen = false,
                OrientationMode = OrientationMode.Normalize,
                ColorProfileMode = ColorProfileMode.ConvertToSrgb,
                EncoderOptions = new JpegEncoderOptions(88, ChromaSubsampleMode.Default, false),
            };

            if (!settings.TrySetEncoderFormat(ImageMimeTypes.Jpeg))
                throw new NotSupportedException("A JPEG artwork encoder is unavailable.");

            input.Position = 0;
            using var output = new MemoryStream();
            MagicImageProcessor.ProcessImage(input, output, settings);
            return output.ToArray();
        });
    }
}
