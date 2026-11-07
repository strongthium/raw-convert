using System.Buffers.Binary;

namespace Strongthium.RawConvert.Tests;

/// <summary>Reading a CR2's directories, and what of them goes into the new file.</summary>
public sealed class TiffTests
{
    [Fact]
    public void ReadsWhatACr2Says()
    {
        var metadata = Metadata.Read(new MemoryStream(Samples.Cr2(orientation: 6)));

        Assert.Equal(6, metadata.Orientation);
        Assert.Equal((6, 4), (metadata.Width, metadata.Height));
        Assert.Equal(Samples.Taken, metadata.Taken);
        Assert.Equal(["Canon", "Canon EOS Sample", Samples.Taken, "Alex Sample"], metadata.Image.Select(entry => entry.String));
        Assert.Equal(3, metadata.Gps.Count);
    }

    [Fact]
    public void LeavesBehindTheMakerNoteAndPointersIntoTheCr2()
    {
        var metadata = Metadata.Read(new MemoryStream(Samples.Cr2()));

        Assert.DoesNotContain(metadata.Exif, entry => entry.Tag is Metadata.MakerNote or Metadata.InteropPointer);
        var exif = metadata.ExifFor(40, 30);
        Assert.Equal(40u, exif.Single(entry => entry.Tag == Metadata.PixelXDimension).Number);
        Assert.Equal(30u, exif.Single(entry => entry.Tag == Metadata.PixelYDimension).Number);
        Assert.Equal("EF85mm f/1.8 USM", exif.Single(entry => entry.Tag == 42036).String);
    }

    [Fact]
    public void TheNewFileIsTheRightWayUpAlready()
    {
        var metadata = Metadata.Read(new MemoryStream(Samples.Cr2(orientation: 8)));
        var block = TiffWriter.Block(metadata.ImageFor(), metadata.ExifFor(4, 6), metadata.Gps);

        var again = Metadata.Read(new MemoryStream(block));
        Assert.Equal(1, again.Orientation);
        Assert.Equal(Samples.Taken, again.Taken);
        Assert.Equal(metadata.Gps.Select(entry => entry.Value), again.Gps.Select(entry => entry.Value));
    }

    [Fact]
    public void ReadsBigEndianFiles()
    {
        // MM, one directory at 8: Orientation (SHORT 3), XResolution (RATIONAL 300/1, at 26) and Make ("Nikon", at 34).
        var file = new byte[64];
        "MM"u8.CopyTo(file);
        BinaryPrimitives.WriteUInt16BigEndian(file.AsSpan(2), 42);
        BinaryPrimitives.WriteUInt32BigEndian(file.AsSpan(4), 8);
        BinaryPrimitives.WriteUInt16BigEndian(file.AsSpan(8), 3);
        Entry(10, 274, 3, 1, 0x00030000);
        Entry(22, 282, 5, 1, 50);
        Entry(34, 271, 2, 6, 58);
        BinaryPrimitives.WriteUInt32BigEndian(file.AsSpan(50), 300);
        BinaryPrimitives.WriteUInt32BigEndian(file.AsSpan(54), 1);
        "Nikon\0"u8.CopyTo(file.AsSpan(58));

        var reader = new TiffReader(new MemoryStream(file));
        var (entries, next) = reader.ReadDirectory(reader.Open());

        Assert.Equal(0u, next);
        Assert.Equal(3u, entries.Single(entry => entry.Tag == 274).Number);
        Assert.Equal([44, 1, 0, 0, 1, 0, 0, 0], entries.Single(entry => entry.Tag == 282).Value);
        Assert.Equal("Nikon", entries.Single(entry => entry.Tag == 271).String);

        void Entry(int at, ushort tag, ushort type, uint count, uint value)
        {
            BinaryPrimitives.WriteUInt16BigEndian(file.AsSpan(at), tag);
            BinaryPrimitives.WriteUInt16BigEndian(file.AsSpan(at + 2), type);
            BinaryPrimitives.WriteUInt32BigEndian(file.AsSpan(at + 4), count);
            BinaryPrimitives.WriteUInt32BigEndian(file.AsSpan(at + 8), value);
        }
    }

    [Theory]
    [InlineData("JPEG file, not a TIFF")]
    [InlineData("II*\0\xff\xff\xff\x7f")]
    public void SaysWhenItIsntATiff(string content)
    {
        var bytes = content.Select(c => (byte)c).ToArray();
        Assert.ThrowsAny<Exception>(() => Metadata.Read(new MemoryStream(bytes)));
    }

    [Fact]
    public void ValuesThatDontFitInTheirEntryLandOnEvenOffsets()
    {
        var block = TiffWriter.Block([TiffEntry.Text(271, "Odd"), TiffEntry.Text(272, "Even1")], [], []);
        var reader = new TiffReader(new MemoryStream(block));
        var (entries, _) = reader.ReadDirectory(reader.Open());

        Assert.Equal(["Odd", "Even1"], entries.Select(entry => entry.String));
        Assert.Equal(0, block.Length % 2);
    }
}
