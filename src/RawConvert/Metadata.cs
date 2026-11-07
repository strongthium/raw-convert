namespace Strongthium.RawConvert;

/// <summary>
/// What a photo says about itself, taken from a CR2's own TIFF directories: the camera, the date taken, the exposure,
/// the lens and where it was taken (Exif and GPS), and which way up it was held. It's written again into the PNG or
/// TIFF. The maker note, Canon's own data, is left behind: its offsets point into the CR2.
/// </summary>
internal sealed record Metadata(IReadOnlyList<TiffEntry> Image, IReadOnlyList<TiffEntry> Exif, IReadOnlyList<TiffEntry> Gps,
    int Orientation, int Width, int Height)
{
    public const ushort ImageWidth = 256, ImageHeight = 257, OrientationTag = 274;
    public const ushort MakerNote = 37500, InteropPointer = 40965, PixelXDimension = 40962, PixelYDimension = 40963;

    /// <summary>The first directory's tags that are about the photo rather than about how its image is stored.</summary>
    private static readonly HashSet<ushort> ImageTags =
    [
        270, // ImageDescription
        271, // Make
        272, // Model
        305, // Software
        306, // DateTime
        315, // Artist
        33432, // Copyright
    ];

    public static readonly Metadata None = new([], [], [], 1, 0, 0);

    public static Metadata Read(string path)
    {
        using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.RandomAccess);
        return Read(file);
    }

    public static Metadata Read(Stream stream)
    {
        var reader = new TiffReader(stream);
        var (first, _) = reader.ReadDirectory(reader.Open());
        var exif = Find(first, TiffWriter.ExifPointer) is { } exifAt ? reader.ReadDirectory(exifAt).Entries : [];
        var gps = Find(first, TiffWriter.GpsPointer) is { } gpsAt ? reader.ReadDirectory(gpsAt).Entries : [];
        var orientation = (int)(Find(first, OrientationTag) ?? 1);
        return new Metadata(
            first.Where(entry => ImageTags.Contains(entry.Tag)).ToList(),
            exif.Where(entry => entry.Tag is not (MakerNote or InteropPointer) && entry.Type != 13).ToList(),
            gps.Where(entry => entry.Type != 13).ToList(),
            orientation is >= 1 and <= 8 ? orientation : 1,
            (int)(Find(first, ImageWidth) ?? 0),
            (int)(Find(first, ImageHeight) ?? 0));
    }

    /// <summary>The first directory's photo tags for the new file, which is the right way up already.</summary>
    public List<TiffEntry> ImageFor() => [.. Image, TiffEntry.Shorts(OrientationTag, 1)];

    /// <summary>The Exif directory for the new file, with its size in pixels.</summary>
    public List<TiffEntry> ExifFor(int width, int height) => Exif.Count == 0 ? [] :
    [
        .. Exif.Where(entry => entry.Tag is not (PixelXDimension or PixelYDimension)),
        TiffEntry.Longs(PixelXDimension, (uint)width),
        TiffEntry.Longs(PixelYDimension, (uint)height),
    ];

    /// <summary>The date and time taken, as the camera wrote it (DateTimeOriginal, else DateTime).</summary>
    public string? Taken => Text(36867) ?? Text(306);

    /// <summary>The date and time taken, on the camera's clock, if the camera wrote one that's a date.</summary>
    public DateTime? TakenAt => DateTime.TryParseExact(Taken, "yyyy:MM:dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture,
        System.Globalization.DateTimeStyles.None, out var taken) ? taken : null;

    /// <summary>A text tag of the first directory or the Exif directory.</summary>
    public string? Text(ushort tag) => (Exif.FirstOrDefault(entry => entry.Tag == tag) ?? Image.FirstOrDefault(entry => entry.Tag == tag))?.String?.Trim();

    private static uint? Find(List<TiffEntry> entries, ushort tag) => entries.FirstOrDefault(entry => entry.Tag == tag)?.Number;
}
