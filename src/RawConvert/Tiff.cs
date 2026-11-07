using System.Buffers.Binary;
using System.Text;

namespace Strongthium.RawConvert;

/// <summary>One entry of a TIFF directory: its tag, its type, how many values, and the values' bytes, little-endian.</summary>
internal sealed record TiffEntry(ushort Tag, ushort Type, uint Count, byte[] Value)
{
    public const ushort Byte = 1, Ascii = 2, Short = 3, Long = 4, Rational = 5, Undefined = 7;

    public static TiffEntry Shorts(ushort tag, params ushort[] values)
    {
        var bytes = new byte[values.Length * 2];
        for (var i = 0; i < values.Length; i++) BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(i * 2), values[i]);
        return new TiffEntry(tag, Short, (uint)values.Length, bytes);
    }

    public static TiffEntry Longs(ushort tag, params uint[] values)
    {
        var bytes = new byte[values.Length * 4];
        for (var i = 0; i < values.Length; i++) BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(i * 4), values[i]);
        return new TiffEntry(tag, Long, (uint)values.Length, bytes);
    }

    public static TiffEntry Fraction(ushort tag, uint numerator, uint denominator)
    {
        var bytes = new byte[8];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, numerator);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4), denominator);
        return new TiffEntry(tag, Rational, 1, bytes);
    }

    public static TiffEntry Text(ushort tag, string text)
    {
        var bytes = Encoding.ASCII.GetBytes(text + "\0");
        return new TiffEntry(tag, Ascii, (uint)bytes.Length, bytes);
    }

    /// <summary>The first value, as a number, for a BYTE, SHORT or LONG.</summary>
    public uint? Number => (Type, Value.Length) switch
    {
        (Byte, >= 1) => Value[0],
        (Short, >= 2) => BinaryPrimitives.ReadUInt16LittleEndian(Value),
        (Long, >= 4) => BinaryPrimitives.ReadUInt32LittleEndian(Value),
        _ => null,
    };

    /// <summary>An ASCII value, without the NUL at its end.</summary>
    public string? String => Type == Ascii ? Encoding.ASCII.GetString(Value).TrimEnd('\0') : null;

    /// <summary>The size of one value of a type, or 0 for a type TIFF doesn't have.</summary>
    public static int SizeOf(ushort type) => type switch
    {
        1 or 2 or 6 or 7 => 1,
        3 or 8 => 2,
        4 or 9 or 11 or 13 => 4,
        5 or 10 or 12 => 8,
        _ => 0,
    };

    /// <summary>The size of the numbers a value is made of, which is what's swapped between byte orders: a RATIONAL is
    /// two LONGs.</summary>
    public static int PartOf(ushort type) => type switch
    {
        5 or 10 => 4,
        _ => SizeOf(type),
    };
}

/// <summary>Reads the directories of a TIFF file, such as a CR2, which is one. Only the directories are read, not the
/// image data they point to.</summary>
internal sealed class TiffReader(Stream stream)
{
    // A directory's entry count, and a value's size, beyond which the file isn't believed.
    private const int MostEntries = 4096;
    private const int LargestValue = 16 << 20;

    private bool bigEndian;

    /// <summary>The offset of the first directory, after checking the header.</summary>
    public uint Open()
    {
        Span<byte> header = stackalloc byte[8];
        stream.Position = 0;
        stream.ReadExactly(header);
        bigEndian = (header[0], header[1]) switch
        {
            ((byte)'I', (byte)'I') => false,
            ((byte)'M', (byte)'M') => true,
            _ => throw new InvalidDataException("It isn't a TIFF file: it doesn't start with II or MM."),
        };
        if (U16(header[2..]) != 42) throw new InvalidDataException("It isn't a TIFF file: there's no 42 in its header.");
        return U32(header[4..]);
    }

    /// <summary>The entries of the directory at an offset, and the offset of the next one (0 for none).</summary>
    public (List<TiffEntry> Entries, uint Next) ReadDirectory(uint offset)
    {
        if (offset < 8 || offset >= stream.Length) throw new InvalidDataException($"A directory is said to be at {offset}, outside the file.");
        stream.Position = offset;
        Span<byte> two = stackalloc byte[2];
        stream.ReadExactly(two);
        int count = U16(two);
        if (count > MostEntries) throw new InvalidDataException($"A directory is said to have {count} entries.");

        var table = new byte[count * 12 + 4];
        stream.ReadExactly(table);
        var entries = new List<TiffEntry>(count);
        for (var i = 0; i < count; i++)
        {
            var entry = table.AsSpan(i * 12, 12);
            var tag = U16(entry);
            var type = U16(entry[2..]);
            var values = U32(entry[4..]);
            var size = TiffEntry.SizeOf(type);
            if (size == 0) continue;
            var length = (long)size * values;
            if (length > LargestValue) continue;

            var value = new byte[length];
            if (length <= 4)
            {
                entry.Slice(8, (int)length).CopyTo(value);
            }
            else
            {
                var at = U32(entry[8..]);
                if (at + length > stream.Length) continue;
                var back = stream.Position;
                stream.Position = at;
                stream.ReadExactly(value);
                stream.Position = back;
            }
            if (bigEndian) Swap(value, TiffEntry.PartOf(type));
            entries.Add(new TiffEntry(tag, type, values, value));
        }
        return (entries, U32(table.AsSpan(count * 12)));
    }

    private static void Swap(byte[] value, int part)
    {
        if (part < 2) return;
        for (var i = 0; i + part <= value.Length; i += part) value.AsSpan(i, part).Reverse();
    }

    private ushort U16(ReadOnlySpan<byte> bytes) => bigEndian ? BinaryPrimitives.ReadUInt16BigEndian(bytes) : BinaryPrimitives.ReadUInt16LittleEndian(bytes);

    private uint U32(ReadOnlySpan<byte> bytes) => bigEndian ? BinaryPrimitives.ReadUInt32BigEndian(bytes) : BinaryPrimitives.ReadUInt32LittleEndian(bytes);
}

/// <summary>
/// Writes a little-endian TIFF: the first directory, then the Exif and GPS directories it points to, then the image's
/// strips if there are any. The same layout without strips is the block of Exif data a PNG holds in its eXIf chunk.
/// </summary>
internal static class TiffWriter
{
    public const ushort ExifPointer = 34665, GpsPointer = 34853, StripOffsets = 273;

    public static void Write(Stream output, IEnumerable<TiffEntry> first, IReadOnlyList<TiffEntry> exif, IReadOnlyList<TiffEntry> gps,
        IReadOnlyList<byte[]>? strips = null)
    {
        // Every pointer is one LONG, which fits in its entry, so each directory's size is known before what it points at
        // is placed.
        var main = first.Where(entry => entry.Tag is not (ExifPointer or GpsPointer or StripOffsets)).ToList();
        if (exif.Count > 0) main.Add(TiffEntry.Longs(ExifPointer, 0));
        if (gps.Count > 0) main.Add(TiffEntry.Longs(GpsPointer, 0));
        if (strips is not null) main.Add(TiffEntry.Longs(StripOffsets, new uint[strips.Count]));
        main = Sorted(main);
        var exifEntries = Sorted(exif);
        var gpsEntries = Sorted(gps);

        long mainAt = 8;
        var exifAt = mainAt + SizeOf(main);
        var gpsAt = exifAt + (exifEntries.Count > 0 ? SizeOf(exifEntries) : 0);
        var stripsAt = gpsAt + (gpsEntries.Count > 0 ? SizeOf(gpsEntries) : 0);
        var end = stripsAt + (strips?.Sum(strip => (long)strip.Length) ?? 0);
        if (end > uint.MaxValue) throw new InvalidOperationException("The TIFF would be larger than 4 GB, which TIFF can't hold.");

        for (var i = 0; i < main.Count; i++)
        {
            main[i] = main[i].Tag switch
            {
                ExifPointer => TiffEntry.Longs(ExifPointer, (uint)exifAt),
                GpsPointer => TiffEntry.Longs(GpsPointer, (uint)gpsAt),
                StripOffsets => TiffEntry.Longs(StripOffsets, Offsets(strips!, stripsAt)),
                _ => main[i],
            };
        }

        using var writer = new BinaryWriter(output, Encoding.ASCII, leaveOpen: true);
        writer.Write("II"u8);
        writer.Write((ushort)42);
        writer.Write((uint)mainAt);
        WriteDirectory(writer, main, mainAt);
        if (exifEntries.Count > 0) WriteDirectory(writer, exifEntries, exifAt);
        if (gpsEntries.Count > 0) WriteDirectory(writer, gpsEntries, gpsAt);
        foreach (var strip in strips ?? []) writer.Write(strip);
    }

    /// <summary>The Exif block of a PNG's eXIf chunk: a TIFF with directories and no image.</summary>
    public static byte[] Block(IEnumerable<TiffEntry> first, IReadOnlyList<TiffEntry> exif, IReadOnlyList<TiffEntry> gps)
    {
        using var memory = new MemoryStream();
        Write(memory, first, exif, gps);
        return memory.ToArray();
    }

    private static List<TiffEntry> Sorted(IEnumerable<TiffEntry> entries) =>
        entries.GroupBy(entry => entry.Tag).Select(same => same.Last()).OrderBy(entry => entry.Tag).ToList();

    private static uint[] Offsets(IReadOnlyList<byte[]> strips, long at)
    {
        var offsets = new uint[strips.Count];
        for (var i = 0; i < strips.Count; i++)
        {
            offsets[i] = (uint)at;
            at += strips[i].Length;
        }
        return offsets;
    }

    /// <summary>A directory's size with its values that don't fit in their entries, each kept to an even offset.</summary>
    private static long SizeOf(List<TiffEntry> entries) =>
        2 + 12L * entries.Count + 4 + entries.Where(entry => entry.Value.Length > 4).Sum(entry => (long)Even(entry.Value.Length));

    private static int Even(int length) => length + (length & 1);

    private static void WriteDirectory(BinaryWriter writer, List<TiffEntry> entries, long at)
    {
        if (writer.BaseStream.Position != at) throw new InvalidOperationException("A TIFF directory isn't where it was placed.");
        var values = at + 2 + 12L * entries.Count + 4;
        writer.Write((ushort)entries.Count);
        foreach (var entry in entries)
        {
            writer.Write(entry.Tag);
            writer.Write(entry.Type);
            writer.Write(entry.Count);
            if (entry.Value.Length <= 4)
            {
                writer.Write(entry.Value);
                for (var pad = entry.Value.Length; pad < 4; pad++) writer.Write((byte)0);
            }
            else
            {
                writer.Write((uint)values);
                values += Even(entry.Value.Length);
            }
        }
        writer.Write(0u);
        foreach (var entry in entries.Where(entry => entry.Value.Length > 4))
        {
            writer.Write(entry.Value);
            if ((entry.Value.Length & 1) == 1) writer.Write((byte)0);
        }
    }
}
