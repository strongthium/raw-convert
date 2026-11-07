using Strongthium.RawConvert;

const string Usage = """
    raw-convert <input folder> <output folder> [options]

    Converts every Canon CR2 photo in the input folder, and in the folders inside it, into a lossless PNG of the same
    size, at the same place under the output folder. The date taken, camera, lens, exposure and GPS go with each one.

      --format png|tiff       PNG (the default) or TIFF; both are lossless and Amazon Photos takes both
      --overwrite             convert again what's already in the output folder (otherwise it's left as it is)
      --copy-others           copy the files that aren't CR2 (JPEGs, videos) into the output folder too
      --jobs <n>              how many photos to convert at once (default {jobs}, from this PC's processors)
      --quiet                 say nothing but what goes wrong
      --about                 who makes this, and whether there's a newer version (GitHub is asked)
      --update                install the newer version, if there is one: it asks first, unless --yes is given

    Windows develops the raw data, with Microsoft's free Raw Image Extension from the Microsoft Store.
    """;
var usage = Usage.Replace("{jobs}", Options.DefaultJobs.ToString());

Updater.Tidy();

var folders = new List<string>();
var about = false;
var update = false;
var yes = false;
var format = Format.Png;
var overwrite = false;
var copyOthers = false;
var jobs = Options.DefaultJobs;
var quiet = false;

for (var i = 0; i < args.Length; i++)
{
    string Next() => ++i < args.Length ? args[i] : throw new ArgumentException($"{args[i - 1]} needs a value.");
    try
    {
        switch (args[i])
        {
            case "--help" or "-h" or "/?":
                Console.WriteLine(usage);
                return 0;
            case "--about": about = true; break;
            case "--update": update = true; break;
            case "--yes" or "-y": yes = true; break;
            case "--overwrite": overwrite = true; break;
            case "--copy-others": copyOthers = true; break;
            case "--quiet": quiet = true; break;
            case "--format":
                format = Next().ToLowerInvariant() switch
                {
                    "png" => Format.Png,
                    "tiff" or "tif" => Format.Tiff,
                    var other => throw new ArgumentException($"--format is png or tiff, not {other}."),
                };
                break;
            case "--jobs":
                if (!int.TryParse(Next(), out jobs) || jobs < 1 || jobs > 64) throw new ArgumentException("--jobs is a number from 1 to 64.");
                break;
            default:
                if (args[i].StartsWith("--", StringComparison.Ordinal)) throw new ArgumentException($"Unknown option: {args[i]}");
                folders.Add(args[i]);
                break;
        }
    }
    catch (ArgumentException exception)
    {
        Console.Error.WriteLine(exception.Message);
        return 2;
    }
}

if (update) return await AboutCommand.UpdateAsync(yes);
if (about) return await AboutCommand.AboutAsync(yes);

if (folders.Count != 2)
{
    if (folders.Count > 2) Console.Error.WriteLine($"Only two folders, the input and the output: {folders.Count} were given.\n");
    Console.Error.WriteLine(usage);
    return 2;
}
var input = Path.GetFullPath(folders[0]);
var output = Path.GetFullPath(folders[1]);
if (!Directory.Exists(input))
{
    Console.Error.WriteLine($"There's no folder {input}.");
    return 2;
}
if (Path.TrimEndingDirectorySeparator(input).Equals(Path.TrimEndingDirectorySeparator(output), StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine("The output folder is the input folder: give it a folder of its own (it can be inside the input folder).");
    return 2;
}

using var cancel = new CancellationTokenSource();
Console.CancelKeyPress += (_, stop) =>
{
    // The first Ctrl+C lets the photos being converted finish, and starts no more; a second one stops at once.
    if (cancel.IsCancellationRequested) return;
    stop.Cancel = true;
    cancel.Cancel();
    Console.Error.WriteLine("Stopping once the photos being converted are done (Ctrl+C again stops at once)...");
};

var options = new Options
{
    Input = input,
    Output = output,
    Format = format,
    Overwrite = overwrite,
    CopyOthers = copyOthers,
    Jobs = jobs,
    Quiet = quiet,
};
try
{
    return new Batch(options, Wic.Decode, Console.Out, Console.Error).Run(cancel.Token);
}
catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
{
    Console.Error.WriteLine(exception.Message);
    return 1;
}
