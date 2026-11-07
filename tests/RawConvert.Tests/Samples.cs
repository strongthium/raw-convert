using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Strongthium.RawConvert.Tests;

/// <summary>Made-up pictures and CR2 files, and Windows' own decoders (through WPF) to read back what was written.</summary>
internal static class Samples
{
    /// <summary>A picture of gradients and noise, so that every PNG filter gets used.</summary>
    public static RgbImage Picture(int width, int height, int seed = 1)
    {
        var random = new Random(seed);
        var pixels = new byte[width * height * 3];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var i = (y * width + x) * 3;
                pixels[i] = (byte)(x * 255 / Math.Max(1, width - 1));
                pixels[i + 1] = (byte)(y * 255 / Math.Max(1, height - 1));
                pixels[i + 2] = (byte)random.Next(256);
            }
        }
        return new RgbImage(width, height, pixels);
    }

    public const string Taken = "2024:07:14 16:32:05";

    /// <summary>A CR2-like TIFF holding what a Canon's first directory, Exif and GPS directories hold, maker note included.
    /// Its first directory says it's <paramref name="width"/> by <paramref name="height"/>, as a CR2's does.</summary>
    public static byte[] Cr2(int orientation = 1, int width = 6, int height = 4, bool withGps = true)
    {
        List<TiffEntry> first =
        [
            TiffEntry.Longs(256, (uint)width),
            TiffEntry.Longs(257, (uint)height),
            TiffEntry.Shorts(259, 6),
            TiffEntry.Text(271, "Canon"),
            TiffEntry.Text(272, "Canon EOS Sample"),
            TiffEntry.Shorts(274, (ushort)orientation),
            TiffEntry.Text(306, Taken),
            TiffEntry.Text(315, "Alex Sample"),
        ];
        List<TiffEntry> exif =
        [
            TiffEntry.Fraction(33434, 1, 250), // ExposureTime
            TiffEntry.Fraction(33437, 56, 10), // FNumber
            TiffEntry.Shorts(34855, 400), // ISO
            TiffEntry.Text(36867, Taken), // DateTimeOriginal
            TiffEntry.Fraction(37386, 85, 1), // FocalLength
            new TiffEntry(37500, TiffEntry.Undefined, 64, new byte[64]), // MakerNote
            TiffEntry.Longs(40962, 9999),
            TiffEntry.Longs(40963, 9999),
            TiffEntry.Longs(40965, 12345), // InteropIFD
            TiffEntry.Text(42036, "EF85mm f/1.8 USM"), // LensModel
        ];
        List<TiffEntry> gps = withGps
            ? [new TiffEntry(0, TiffEntry.Byte, 4, [2, 3, 0, 0]), TiffEntry.Text(1, "N"), TiffEntry.Text(3, "W")]
            : [];
        using var memory = new MemoryStream();
        TiffWriter.Write(memory, first, exif, gps);
        return memory.ToArray();
    }

    /// <summary>A file decoded by Windows, as 8-bit RGB, with its metadata.</summary>
    public static (RgbImage Image, BitmapMetadata? Metadata) Decode(string path)
    {
        using var file = File.OpenRead(path);
        var decoder = BitmapDecoder.Create(file, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var frame = decoder.Frames[0];
        var rgb = new FormatConvertedBitmap(frame, PixelFormats.Rgb24, null, 0);
        var pixels = new byte[rgb.PixelWidth * rgb.PixelHeight * 3];
        rgb.CopyPixels(pixels, rgb.PixelWidth * 3, 0);
        return (new RgbImage(rgb.PixelWidth, rgb.PixelHeight, pixels), frame.Metadata as BitmapMetadata);
    }

    /// <summary>A folder of its own under the tests' output, deleted when it's disposed.</summary>
    public sealed class Folder : IDisposable
    {
        public string Path { get; } = Directory.CreateDirectory(System.IO.Path.Combine(AppContext.BaseDirectory, "work", Guid.NewGuid().ToString("N"))).FullName;

        public string File(string relative, byte[] content)
        {
            var path = System.IO.Path.Combine(Path, relative);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
            System.IO.File.WriteAllBytes(path, content);
            return path;
        }

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
