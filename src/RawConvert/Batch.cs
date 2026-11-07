using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Strongthium.RawConvert;

internal sealed class Options
{
    public required string Input { get; init; }
    public required string Output { get; init; }
    public Format Format { get; init; } = Format.Png;
    public bool Overwrite { get; init; }
    public bool CopyOthers { get; init; }
    public int Jobs { get; init; } = DefaultJobs;
    public bool Quiet { get; init; }

    /// <summary>Half the processors, up to 4: a 20-megapixel photo takes a few hundred megabytes while it's developed.</summary>
    public static int DefaultJobs => Math.Clamp(Environment.ProcessorCount / 2, 1, 4);

    public string Extension => Format == Format.Tiff ? ".tif" : ".png";
}

/// <summary>One file to do: a CR2 to convert, or (with --copy-others) another file to copy.</summary>
internal sealed record Work(string Source, string Target, string Shown, bool Convert);

/// <summary>
/// Converts every CR2 under a folder into a PNG or TIFF of the same size, at the same place under another folder. A file
/// already in the output folder is left alone unless --overwrite is given, so a batch that was stopped picks up where it
/// left off. Each file is written beside its final name first and renamed once it's whole.
/// </summary>
internal sealed class Batch(Options options, Func<string, RgbImage> decode, TextWriter output, TextWriter errors)
{
    public const string RawExtension = ".cr2";
    private const string Partial = ".partial";

    private readonly Lock speaking = new();
    private int converted, copied, skipped, failed, noDecoder, finished;

    public int Converted => converted;
    public int Failed => failed;

    /// <summary>What there is to do, in the order the folders are listed. The output folder isn't looked in, should it be
    /// inside the input folder.</summary>
    public List<Work> Plan()
    {
        var input = Path.GetFullPath(options.Input);
        var outputRoot = Path.GetFullPath(options.Output);
        var work = new List<Work>();
        var folders = new Stack<string>();
        folders.Push(input);
        var skip = new EnumerationOptions { IgnoreInaccessible = true, AttributesToSkip = FileAttributes.Hidden | FileAttributes.System };
        while (folders.Count > 0)
        {
            var folder = folders.Pop();
            foreach (var file in Directory.EnumerateFiles(folder, "*", skip).Order(StringComparer.OrdinalIgnoreCase))
            {
                var relative = Path.GetRelativePath(input, file);
                var isRaw = Path.GetExtension(file).Equals(RawExtension, StringComparison.OrdinalIgnoreCase);
                if (isRaw) work.Add(new Work(file, Path.Combine(outputRoot, Path.ChangeExtension(relative, options.Extension)), relative, true));
                else if (options.CopyOthers && !file.EndsWith(Partial, StringComparison.OrdinalIgnoreCase)) work.Add(new Work(file, Path.Combine(outputRoot, relative), relative, false));
            }
            foreach (var sub in Directory.EnumerateDirectories(folder, "*", skip).Order(StringComparer.OrdinalIgnoreCase).Reverse())
            {
                if (!Path.GetFullPath(sub).Equals(outputRoot, StringComparison.OrdinalIgnoreCase)) folders.Push(sub);
            }
        }
        return work;
    }

    public int Run(CancellationToken cancel)
    {
        var clock = Stopwatch.StartNew();
        var work = Plan();
        var raws = work.Count(item => item.Convert);
        if (raws == 0 && !options.Quiet) output.WriteLine($"There are no {RawExtension.ToUpperInvariant()[1..]} files in {Path.GetFullPath(options.Input)} or the folders in it.");

        var next = -1;
        var threads = Enumerable.Range(0, Math.Max(1, Math.Min(options.Jobs, work.Count))).Select(n => new Thread(() =>
        {
            int index;
            while (!cancel.IsCancellationRequested && (index = Interlocked.Increment(ref next)) < work.Count) Do(work[index], work.Count);
        }) { Name = $"convert {n + 1}", IsBackground = true }).ToList();
        threads.ForEach(thread => thread.Start());
        threads.ForEach(thread => thread.Join());

        var parts = new List<string> { $"{converted} converted" };
        if (options.CopyOthers) parts.Add($"{copied} copied");
        if (skipped > 0) parts.Add($"{skipped} already there, left as they were");
        if (failed > 0) parts.Add($"{failed} failed");
        var stopped = cancel.IsCancellationRequested ? ", stopped before the end" : "";
        lock (speaking)
        {
            if (!options.Quiet || failed > 0) output.WriteLine($"{string.Join(", ", parts)}{stopped}, in {Took(clock.Elapsed)}.");
            if (noDecoder > 0 && converted == 0) errors.WriteLine(NoDecoderHelp);
        }
        return failed > 0 || cancel.IsCancellationRequested ? 1 : 0;
    }

    public const string NoDecoderHelp = """
        Windows has no decoder for these CR2 files on this PC. raw-convert uses Microsoft's free Raw Image Extension:
          https://apps.microsoft.com/detail/9NCTDW2W1BH8
        Install it from the Microsoft Store, then run raw-convert again: what's done already is left as it is.
        """;

    private void Do(Work item, int total)
    {
        if (File.Exists(item.Target) && !options.Overwrite)
        {
            Interlocked.Increment(ref skipped);
            Say(total, $"{item.Shown}: already there");
            return;
        }

        var clock = Stopwatch.StartNew();
        var partial = item.Target + Partial;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(item.Target)!);
            if (item.Convert) Convert(item.Source, partial);
            else File.Copy(item.Source, partial, overwrite: true);
            var source = new FileInfo(item.Source);
            File.SetCreationTimeUtc(partial, source.CreationTimeUtc);
            File.SetLastWriteTimeUtc(partial, source.LastWriteTimeUtc);
            File.Move(partial, item.Target, overwrite: true);

            if (item.Convert) Interlocked.Increment(ref converted);
            else Interlocked.Increment(ref copied);
            var size = new FileInfo(item.Target).Length;
            var shown = item.Convert ? $"{item.Shown} -> {Path.GetFileName(item.Target)}" : $"{item.Shown} copied";
            Say(total, $"{shown}  ({size / 1e6:0.0} MB, {Took(clock.Elapsed)})");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException
            or COMException or NotSupportedException or InvalidOperationException or OverflowException or OutOfMemoryException)
        {
            Interlocked.Increment(ref failed);
            try
            {
                File.Delete(partial);
            }
            catch (Exception exception2) when (exception2 is IOException or UnauthorizedAccessException)
            {
                // It's left with .partial on its name, which is never taken for a finished file.
            }
            var why = exception is COMException com && com.HResult is Wic.NoDecoder or Wic.UnknownFormat
                ? "Windows couldn't read it as a photo (it may be damaged, or not a CR2)"
                : exception.Message.TrimEnd('.');
            if (exception is COMException { HResult: Wic.NoDecoder or Wic.UnknownFormat } && item.Convert) Interlocked.Increment(ref noDecoder);
            Say(total, $"{item.Shown}: not {(item.Convert ? "converted" : "copied")}: {why}.", error: true);
        }
    }

    /// <summary>Develops one CR2 and writes it as the chosen format, the right way up, with its Exif.</summary>
    private void Convert(string source, string target)
    {
        Metadata metadata;
        try
        {
            metadata = Metadata.Read(source);
        }
        catch (Exception exception) when (exception is InvalidDataException or EndOfStreamException)
        {
            // The picture may still be readable: it's converted without what the file says about it.
            metadata = Metadata.None;
        }

        var image = decode(source);
        image = Orientation.Apply(image, Orientation.ToApply(metadata.Orientation, image, metadata));
        using var file = new FileStream(target, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 16);
        if (options.Format == Format.Tiff)
        {
            TiffImageWriter.Write(file, image, metadata);
        }
        else
        {
            PngWriter.Write(file, image, metadata);
        }
    }

    /// <summary>Says a file is done, numbered by the order they finish in, which isn't the order they started in.</summary>
    private void Say(int total, string text, bool error = false)
    {
        lock (speaking)
        {
            var counter = $"{(++finished).ToString().PadLeft(total.ToString().Length)}/{total}";
            if (error) errors.WriteLine($"{counter}  {text}");
            else if (!options.Quiet) output.WriteLine($"{counter}  {text}");
        }
    }

    internal static string Took(TimeSpan time) => time.TotalSeconds < 60
        ? $"{time.TotalSeconds:0.0} s"
        : time.TotalHours < 1 ? $"{(int)time.TotalMinutes} min {time.Seconds} s" : $"{(int)time.TotalHours} h {time.Minutes} min";
}
