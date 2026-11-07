namespace Strongthium.RawConvert;

/// <summary>Turns a picture the right way up, by the Exif orientation tag (1 to 8) its camera wrote. Each pixel is moved
/// as it is, so nothing is lost.</summary>
internal static class Orientation
{
    /// <summary>Whether an orientation swaps width and height (a quarter turn, with or without a mirror).</summary>
    public static bool Swaps(int orientation) => orientation is >= 5 and <= 8;

    /// <summary>
    /// The orientation to turn a decoded picture by. A raw sensor is always wider than it is tall, as the CR2's own
    /// first directory says; if the picture Windows gave back for a quarter turn is already the other way round, the
    /// decoder has turned it, and it isn't turned again.
    /// </summary>
    public static int ToApply(int orientation, RgbImage decoded, Metadata metadata)
    {
        if (!Swaps(orientation) || metadata.Width == 0 || metadata.Height == 0) return orientation;
        var storedWide = metadata.Width >= metadata.Height;
        var decodedWide = decoded.Width >= decoded.Height;
        return storedWide == decodedWide ? orientation : 1;
    }

    public static RgbImage Apply(RgbImage image, int orientation)
    {
        if (orientation is < 2 or > 8) return image;
        int w = image.Width, h = image.Height;
        var swaps = Swaps(orientation);
        var width = swaps ? h : w;
        var height = swaps ? w : h;
        var source = image.Pixels;
        var target = GC.AllocateUninitializedArray<byte>(source.Length);

        Parallel.For(0, height, y =>
        {
            var row = y * width * 3;
            for (var x = 0; x < width; x++)
            {
                // Where the pixel at (x, y) of the turned picture comes from.
                var (sx, sy) = orientation switch
                {
                    2 => (w - 1 - x, y),
                    3 => (w - 1 - x, h - 1 - y),
                    4 => (x, h - 1 - y),
                    5 => (y, x),
                    6 => (y, h - 1 - x),
                    7 => (w - 1 - y, h - 1 - x),
                    _ => (w - 1 - y, x),
                };
                var from = (sy * w + sx) * 3;
                var to = row + x * 3;
                target[to] = source[from];
                target[to + 1] = source[from + 1];
                target[to + 2] = source[from + 2];
            }
        });
        return new RgbImage(width, height, target);
    }
}
