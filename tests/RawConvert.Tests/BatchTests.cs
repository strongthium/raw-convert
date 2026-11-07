using System.Runtime.InteropServices;

namespace Strongthium.RawConvert.Tests;

/// <summary>Whole batches, on folders of made-up CR2 files, with a decoder that gives back a made-up picture.</summary>
public sealed class BatchTests : IDisposable
{
    private readonly Samples.Folder folder = new();
    private readonly StringWriter output = new();
    private readonly StringWriter errors = new();
    private readonly List<string> decoded = [];

    public void Dispose() => folder.Dispose();

    private string Input => Path.Combine(folder.Path, "in");
    private string Output => Path.Combine(folder.Path, "out");

    private RgbImage Decode(string path)
    {
        lock (decoded) decoded.Add(Path.GetFileName(path));
        if (path.Contains("broken", StringComparison.OrdinalIgnoreCase)) throw new COMException("Windows couldn't read it.", unchecked((int)0x88982F61));
        if (path.Contains("nodecoder", StringComparison.OrdinalIgnoreCase)) throw new COMException("No decoder.", Wic.NoDecoder);
        return Samples.Picture(6, 4);
    }

    private int Run(Format format = Format.Png, bool overwrite = false, bool copyOthers = false, string? output = null) =>
        new Batch(new Options { Input = Input, Output = output ?? Output, Format = format, Overwrite = overwrite, CopyOthers = copyOthers, Jobs = 3 },
            Decode, this.output, errors).Run(CancellationToken.None);

    [Fact]
    public void ConvertsEveryCr2IntoTheSameFolders()
    {
        folder.File("in/IMG_0001.CR2", Samples.Cr2());
        folder.File("in/Trip/Day 2/IMG_0002.cr2", Samples.Cr2());
        folder.File("in/Trip/notes.txt", "not a photo"u8.ToArray());

        Assert.Equal(0, Run());

        Assert.True(File.Exists(Path.Combine(Output, "IMG_0001.png")));
        Assert.True(File.Exists(Path.Combine(Output, "Trip", "Day 2", "IMG_0002.png")));
        Assert.False(File.Exists(Path.Combine(Output, "Trip", "notes.txt")));
        Assert.Empty(Directory.EnumerateFiles(Output, "*.partial", SearchOption.AllDirectories));
        Assert.Contains("2 converted, in", output.ToString());
        Assert.Empty(errors.ToString());
    }

    [Fact]
    public void TurnsPortraitsAndKeepsTheFileTimes()
    {
        var source = folder.File("in/portrait.CR2", Samples.Cr2(orientation: 6));
        var time = new DateTime(2024, 7, 14, 16, 32, 5, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(source, time);

        Run(Format.Tiff);

        var target = Path.Combine(Output, "portrait.tif");
        var (image, metadata) = Samples.Decode(target);
        Assert.Equal((4, 6), (image.Width, image.Height));
        Assert.Equal(Orientation.Apply(Samples.Picture(6, 4), 6).Pixels, image.Pixels);
        Assert.Equal((ushort)1, metadata!.GetQuery("System.Photo.Orientation"));
        Assert.Equal(time, File.GetLastWriteTimeUtc(target));
    }

    [Fact]
    public void LeavesWhatsDoneAloneUnlessToldToOverwrite()
    {
        folder.File("in/a.CR2", Samples.Cr2());
        folder.File("in/b.CR2", Samples.Cr2());
        Run();
        decoded.Clear();

        Run();
        Assert.Empty(decoded);
        Assert.Contains("2 already there", output.ToString());

        Run(overwrite: true);
        Assert.Equal(2, decoded.Count);
    }

    [Fact]
    public void CopiesTheOtherFilesWhenAsked()
    {
        folder.File("in/a.CR2", Samples.Cr2());
        folder.File("in/sub/a.JPG", [1, 2, 3]);

        Run(copyOthers: true);

        Assert.Equal([1, 2, 3], File.ReadAllBytes(Path.Combine(Output, "sub", "a.JPG")));
        Assert.Contains("1 converted, 1 copied", output.ToString());
    }

    [Fact]
    public void AnOutputFolderInsideTheInputIsntLookedIn()
    {
        folder.File("in/a.CR2", Samples.Cr2());
        var inside = Path.Combine(Input, "converted");
        Run(output: inside);
        folder.File("in/converted/stray.CR2", Samples.Cr2());
        decoded.Clear();

        Run(output: inside, overwrite: true, copyOthers: true);

        Assert.Equal(["a.CR2"], decoded);
    }

    [Fact]
    public void OneFileThatFailsDoesntStopTheRest()
    {
        folder.File("in/broken.CR2", Samples.Cr2());
        folder.File("in/good.CR2", Samples.Cr2());

        Assert.Equal(1, Run());

        Assert.True(File.Exists(Path.Combine(Output, "good.png")));
        Assert.False(File.Exists(Path.Combine(Output, "broken.png")));
        Assert.Empty(Directory.EnumerateFiles(Output, "*.partial"));
        Assert.Contains("broken.CR2: not converted: Windows couldn't read it.", errors.ToString());
        Assert.Contains("1 converted, 1 failed", output.ToString());
    }

    [Fact]
    public void SaysWhereToGetTheDecoder()
    {
        folder.File("in/nodecoder.CR2", Samples.Cr2());

        Run();

        Assert.Contains("Windows couldn't read it as a photo", errors.ToString());
        Assert.Contains("Raw Image Extension", errors.ToString());
    }

    [Fact]
    public void AFileThatIsntReallyATiffIsStillConverted()
    {
        folder.File("in/odd.CR2", "not a TIFF at all"u8.ToArray());

        Assert.Equal(0, Run());
        Assert.True(File.Exists(Path.Combine(Output, "odd.png")));
    }
}
