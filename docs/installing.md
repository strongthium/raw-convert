<p align="center">
  <a href="https://strongthium.com" title="Strongthium Labs"><img src="https://raw.githubusercontent.com/strongthium/.github/main/profile/banner-2200x440.png" alt="Strongthium Labs: Take back control of your internet."></a>
</p>

[raw-convert](../README.md) › [Manual](README.md) › Installing

# Installing

`raw-convert` is one file, `raw-convert.exe`, of about 300 KB, for Windows 10 and 11 on x64 PCs. There's no installer:
it's put where you want it, and run from a command prompt or PowerShell.

## What it needs

| | |
|---|---|
| **The .NET 10 Runtime** | the program runs on the [.NET 10 Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) installed on the PC, which Microsoft Update keeps patched. The runtime isn't inside the `.exe`, which is why it's small |
| **The Raw Image Extension** | Windows itself develops the RAW photos, with Microsoft's free [Raw Image Extension](https://apps.microsoft.com/detail/9NCTDW2W1BH8) from the Microsoft Store. Without it no photo converts, and `raw-convert` says where to get it (see [Messages and exit codes](messages.md)) |

Nothing else: `raw-convert` reads the CR2's details and writes the PNG or TIFF itself, and leaves the RAW to Windows.

## Getting it

From [Releases](https://github.com/strongthium/raw-convert/releases/latest):

| | |
|---|---|
| **`raw-convert-<version>-win-x64-needs-dotnet.zip`** | `raw-convert.exe`, with its `README.md` and `LICENSE` |
| **`SHA256SUMS`** | the SHA-256 checksum of the zip, to check the download against |

Or take `raw-convert.exe` straight from the repository's [`public/`](../public) folder, which always holds the latest
build, with its own `SHA256SUMS` and `VERSION`.

To check a download in PowerShell, compare the hash it gives with the line for the zip in `SHA256SUMS`:

```
Get-FileHash raw-convert-1.0.0.0-win-x64-needs-dotnet.zip
```

## Where to put it

Anywhere. Put it in a folder on your `PATH` and `raw-convert` runs from any folder. Otherwise run it from its own
folder: in a command prompt as `raw-convert`, in PowerShell as `.\raw-convert`. Folders given to it that aren't full
paths are taken from the folder you're in.

`raw-convert --help` lists the options (see [Options](options.md)), which is a quick way to see that it runs.

## Updating and removing it

`raw-convert --update` puts the latest release in its place, after asking (see [`--about` and `--update`](about-and-updates.md)).
Downloading the zip again and replacing the file does the same.

It installs nothing and keeps no settings, so removing it is deleting `raw-convert.exe`. After an update, the copy it
replaced is left beside it as `raw-convert.exe.old` until it next starts, which deletes it.

[Contents](README.md) · [`--about` and `--update`](about-and-updates.md) →

<!-- strongthium-links -->

---

<p align="center"><sub>
<b>Strongthium open-source tools:</b> <a href="https://github.com/strongthium/emudhra-security-patch">eMudhra Security Patch</a> · <a href="https://github.com/strongthium/folder-size">Folder Size</a> · <a href="https://github.com/strongthium/health-reminder">Health Reminder</a> · <a href="https://github.com/strongthium/host-dossier">Host Dossier</a> · <a href="https://github.com/strongthium/outbound-watch">Outbound Watch</a> · <b>RAW Convert</b> · <a href="https://github.com/strongthium/video-dispatch">Video Dispatch</a> · <a href="https://github.com/strongthium/webdav-server">WebDAV Server</a><br>
<b>More from Strongthium:</b> <a href="https://strongthium.com/linux-server-firewall">Guard</a> · <a href="https://strongthium.com/self-hosted-mail-server">Mail</a> · <a href="https://strongthium.com/self-hosted-live-chat">Chat</a> · <a href="https://strongthium.com/windows-ssh-client">Remote</a> · <a href="https://strongthium.com/twilio-sms-desktop-app">SMS</a> · <a href="https://strongthium.com/time-tracking-invoicing-software">Work</a> · <a href="https://strongthium.com/windows-registry-editor">Registry</a> · <a href="https://strongthium.com/cfml-server">DotCF</a> · <a href="https://strongthium.com/video-conferencing-dial-up-modem">Convey</a><br>
<a href="https://strongthium.com">Strongthium Labs</a> · Take back control of your internet.
</sub></p>
