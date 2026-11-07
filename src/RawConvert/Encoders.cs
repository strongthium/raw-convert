using System.Buffers.Binary;
using System.IO.Compression;
using System.Security;
using System.Text;

namespace Strongthium.RawConvert;

internal enum Format { Png, Tiff }

/// <summary>
/// Writes a PNG: 8-bit RGB, marked sRGB, and the pixels deflated losslessly. Each row is filtered in whichever of PNG's
/// five ways leaves the smallest numbers, which is what makes photos compress well. The photo's Exif goes in an eXIf
/// chunk, whole; its date taken and camera go in XMP and a "Creation Time" too, which is where Windows (Explorer,
/// Photos) looks for them in a PNG.
/// </summary>
internal static class PngWriter
{
    private const int ChunkSize = 1 << 20;

    public static void Write(Stream output, RgbImage image, Metadata metadata)
    {
        output.Write([0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A]);

        var header = new byte[13];
        BinaryPrimitives.WriteUInt32BigEndian(header, (uint)image.Width);
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(4), (uint)image.Height);
        header[8] = 8; // bits per sample
        header[9] = 2; // RGB
        Chunk(output, "IHDR", header);
        Chunk(output, "sRGB", [0]); // perceptual
        if (metadata.Image.Count + metadata.Exif.Count + metadata.Gps.Count > 0)
        {
            Chunk(output, "eXIf", TiffWriter.Block(metadata.ImageFor(), metadata.ExifFor(image.Width, image.Height), metadata.Gps));
        }
        if (metadata.TakenAt is { } taken) Chunk(output, "tEXt", Encoding.Latin1.GetBytes($"Creation Time\0{taken:yyyy:MM:dd HH:mm:ss}"));
        if (Xmp(metadata) is { } xmp) Chunk(output, "iTXt", Encoding.UTF8.GetBytes($"XML:com.adobe.xmp\0\0\0\0\0{xmp}"));

        // A row's filter depends only on the picture's own rows, so the rows are filtered all at once, and then deflated
        // in one stream, as PNG has it.
        var stride = image.Stride;
        var line = stride + 1;
        var filtered = GC.AllocateUninitializedArray<byte>(checked(line * image.Height));
        Parallel.For(0, image.Height, () => new byte[stride], (y, _, trial) =>
        {
            var row = image.Pixels.AsSpan(y * stride, stride);
            var above = y > 0 ? image.Pixels.AsSpan((y - 1) * stride, stride) : default;
            Filter(row, above, filtered.AsSpan(y * line, line), trial);
            return trial;
        }, _ => { });

        using (var idat = new IdatStream(output))
        {
            ParallelZlib(filtered, idat);
        }
        Chunk(output, "IEND", []);
    }

    /// <summary>The date taken, camera and lens as XMP, or nothing when the photo says none of them.</summary>
    internal static string? Xmp(Metadata metadata)
    {
        var properties = new List<string>();
        if (metadata.TakenAt is { } taken)
        {
            properties.Add($"<exif:DateTimeOriginal>{taken:yyyy-MM-ddTHH:mm:ss}</exif:DateTimeOriginal>");
            properties.Add($"<xmp:CreateDate>{taken:yyyy-MM-ddTHH:mm:ss}</xmp:CreateDate>");
        }
        if (metadata.Text(271) is { Length: > 0 } make) properties.Add($"<tiff:Make>{SecurityElement.Escape(make)}</tiff:Make>");
        if (metadata.Text(272) is { Length: > 0 } model) properties.Add($"<tiff:Model>{SecurityElement.Escape(model)}</tiff:Model>");
        if (metadata.Text(42036) is { Length: > 0 } lens) properties.Add($"<exifEX:LensModel>{SecurityElement.Escape(lens)}</exifEX:LensModel>");
        if (properties.Count == 0) return null;
        return "<?xpacket begin='﻿' id='W5M0MpCehiHzreSzNTczkc9d'?>"
            + "<x:xmpmeta xmlns:x='adobe:ns:meta/'><rdf:RDF xmlns:rdf='http://www.w3.org/1999/02/22-rdf-syntax-ns#'>"
            + "<rdf:Description rdf:about='' xmlns:exif='http://ns.adobe.com/exif/1.0/' xmlns:xmp='http://ns.adobe.com/xap/1.0/'"
            + " xmlns:tiff='http://ns.adobe.com/tiff/1.0/' xmlns:exifEX='http://cipa.jp/exif/1.0/'>"
            + string.Concat(properties)
            + "</rdf:Description></rdf:RDF></x:xmpmeta><?xpacket end='r'?>";
    }

    private const int Piece = 4 << 20;

    /// <summary>
    /// Deflates into one zlib stream, as PNG has it, a piece at a time on every processor, as pigz does: each piece but
    /// the last ends on a sync flush (an empty stored block, which leaves it on a byte boundary), so the pieces follow
    /// one another as one deflate stream. Each piece starts without the one before it to look back on, which costs a
    /// few bytes in megabytes.
    /// </summary>
    internal static void ParallelZlib(byte[] data, Stream output)
    {
        var count = Math.Max(1, (data.Length + Piece - 1) / Piece);
        var pieces = new byte[count][];
        Parallel.For(0, count, i =>
        {
            var start = i * Piece;
            var slice = data.AsSpan(start, Math.Min(Piece, data.Length - start));
            using var memory = new MemoryStream();
            var deflate = new DeflateStream(memory, CompressionLevel.Optimal, leaveOpen: true);
            deflate.Write(slice);
            if (i == count - 1)
            {
                deflate.Dispose();
                pieces[i] = memory.ToArray();
            }
            else
            {
                // Flush ends the piece with a sync flush; Dispose would then add a last, final block, which is left off.
                deflate.Flush();
                var length = (int)memory.Length;
                deflate.Dispose();
                pieces[i] = memory.GetBuffer().AsSpan(0, length).ToArray();
            }
        });

        output.Write([0x78, 0x9C]);
        foreach (var piece in pieces) output.Write(piece);
        Span<byte> check = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(check, Adler32(data));
        output.Write(check);
    }

    internal static uint Adler32(ReadOnlySpan<byte> data)
    {
        const uint Modulus = 65521;
        uint a = 1, b = 0;
        while (!data.IsEmpty)
        {
            // 5552 bytes is the most that can be summed before b could pass 2^32.
            var block = data[..Math.Min(5552, data.Length)];
            foreach (var value in block)
            {
                a += value;
                b += a;
            }
            a %= Modulus;
            b %= Modulus;
            data = data[block.Length..];
        }
        return b << 16 | a;
    }

    /// <summary>Filters a row the way that sums to the least, as signed bytes: the usual guess at what deflates best.
    /// The first byte of <paramref name="output"/> is the filter's number, and the rest the filtered row.</summary>
    internal static void Filter(ReadOnlySpan<byte> row, ReadOnlySpan<byte> above, Span<byte> output, byte[] trial)
    {
        var best = 0;
        var bestSum = long.MaxValue;
        for (var type = 0; type < 5; type++)
        {
            Apply(type, row, above, trial);
            long sum = 0;
            foreach (var value in trial) sum += Math.Abs((int)(sbyte)value);
            if (sum < bestSum)
            {
                bestSum = sum;
                best = type;
            }
        }
        output[0] = (byte)best;
        Apply(best, row, above, output[1..]);
    }

    /// <summary>One of PNG's five filters, on a row of 3 bytes a pixel: none, sub, up, average and Paeth. The first row
    /// has no row above, which counts as zeros.</summary>
    private static void Apply(int type, ReadOnlySpan<byte> row, ReadOnlySpan<byte> above, Span<byte> output)
    {
        var top = !above.IsEmpty;
        switch (type)
        {
            case 0:
                row.CopyTo(output);
                break;
            case 1:
                for (var i = 0; i < row.Length; i++) output[i] = (byte)(row[i] - (i >= 3 ? row[i - 3] : 0));
                break;
            case 2:
                for (var i = 0; i < row.Length; i++) output[i] = (byte)(row[i] - (top ? above[i] : 0));
                break;
            case 3:
                for (var i = 0; i < row.Length; i++) output[i] = (byte)(row[i] - (((i >= 3 ? row[i - 3] : 0) + (top ? above[i] : 0)) >> 1));
                break;
            default:
                for (var i = 0; i < row.Length; i++)
                {
                    int a = i >= 3 ? row[i - 3] : 0;
                    int b = top ? above[i] : 0;
                    int c = i >= 3 && top ? above[i - 3] : 0;
                    output[i] = (byte)(row[i] - Paeth(a, b, c));
                }
                break;
        }
    }

    private static int Paeth(int a, int b, int c)
    {
        var p = a + b - c;
        int pa = Math.Abs(p - a), pb = Math.Abs(p - b), pc = Math.Abs(p - c);
        return pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
    }

    internal static void Chunk(Stream output, string type, ReadOnlySpan<byte> data)
    {
        Span<byte> four = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(four, (uint)data.Length);
        output.Write(four);
        Span<byte> name = [(byte)type[0], (byte)type[1], (byte)type[2], (byte)type[3]];
        output.Write(name);
        output.Write(data);
        BinaryPrimitives.WriteUInt32BigEndian(four, Crc32.Of(Crc32.Of(Crc32.Start, name), data) ^ 0xFFFFFFFF);
        output.Write(four);
    }

    /// <summary>Cuts the deflated pixels into IDAT chunks of a megabyte.</summary>
    private sealed class IdatStream(Stream output) : Stream
    {
        private readonly byte[] buffer = new byte[ChunkSize];
        private int used;

        public override void Write(byte[] data, int offset, int count) => Write(data.AsSpan(offset, count));

        public override void Write(ReadOnlySpan<byte> data)
        {
            while (!data.IsEmpty)
            {
                var take = Math.Min(data.Length, buffer.Length - used);
                data[..take].CopyTo(buffer.AsSpan(used));
                used += take;
                data = data[take..];
                if (used == buffer.Length) Flush();
            }
        }

        public override void Flush()
        {
            if (used == 0) return;
            Chunk(output, "IDAT", buffer.AsSpan(0, used));
            used = 0;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) Flush();
            base.Dispose(disposing);
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] data, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
    }
}

/// <summary>The CRC-32 of PNG's chunks (and of zip and gzip).</summary>
internal static class Crc32
{
    public const uint Start = 0xFFFFFFFF;

    private static readonly uint[] Table = Enumerable.Range(0, 256).Select(n =>
    {
        var c = (uint)n;
        for (var k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
        return c;
    }).ToArray();

    /// <summary>Carries a running CRC on over more bytes. The finished CRC is the running one XOR 0xFFFFFFFF.</summary>
    public static uint Of(uint crc, ReadOnlySpan<byte> data)
    {
        foreach (var b in data) crc = Table[(crc ^ b) & 0xFF] ^ (crc >> 8);
        return crc;
    }
}

/// <summary>
/// Writes a TIFF: 8-bit RGB in strips, each deflated (compression 8, "Adobe Deflate") after horizontal differencing
/// (predictor 2), which is lossless and what Photoshop, Lightroom and Windows read. The photo's Exif and GPS are its
/// own directories, as in a camera's file.
/// </summary>
internal static class TiffImageWriter
{
    private const int StripBytes = 256 << 10;

    public static void Write(Stream output, RgbImage image, Metadata metadata)
    {
        var stride = image.Stride;
        var rowsPerStrip = Math.Max(1, StripBytes / stride);
        var stripCount = (image.Height + rowsPerStrip - 1) / rowsPerStrip;
        var strips = new byte[stripCount][];
        Parallel.For(0, stripCount, strip =>
        {
            var first = strip * rowsPerStrip;
            var rows = Math.Min(rowsPerStrip, image.Height - first);
            var data = image.Pixels.AsSpan(first * stride, rows * stride).ToArray();
            for (var y = 0; y < rows; y++)
            {
                var row = data.AsSpan(y * stride, stride);
                for (var i = row.Length - 1; i >= 3; i--) row[i] -= row[i - 3];
            }
            using var memory = new MemoryStream();
            using (var zlib = new ZLibStream(memory, CompressionLevel.Optimal, leaveOpen: true)) zlib.Write(data);
            strips[strip] = memory.ToArray();
        });

        List<TiffEntry> first =
        [
            TiffEntry.Longs(256, (uint)image.Width),
            TiffEntry.Longs(257, (uint)image.Height),
            TiffEntry.Shorts(258, 8, 8, 8),
            TiffEntry.Shorts(259, 8), // Adobe Deflate
            TiffEntry.Shorts(262, 2), // RGB
            TiffEntry.Shorts(277, 3),
            TiffEntry.Longs(278, (uint)rowsPerStrip),
            TiffEntry.Longs(279, strips.Select(strip => (uint)strip.Length).ToArray()),
            TiffEntry.Fraction(282, 72, 1),
            TiffEntry.Fraction(283, 72, 1),
            TiffEntry.Shorts(284, 1), // chunky
            TiffEntry.Shorts(296, 2), // inches
            TiffEntry.Shorts(317, 2), // horizontal differencing
            .. metadata.ImageFor(),
        ];
        TiffWriter.Write(output, first, metadata.ExifFor(image.Width, image.Height), metadata.Gps, strips);
    }
}
