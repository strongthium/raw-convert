<p align="center">
  <a href="https://strongthium.com" title="Strongthium Labs"><img src="https://raw.githubusercontent.com/strongthium/.github/main/profile/banner-2200x440.png" alt="Strongthium Labs: Take back control of your internet."></a>
</p>

[raw-convert](../README.md) › [Manual](README.md) › Messages and exit codes

# Messages and exit codes

Every line `raw-convert` prints when it converts, what it means, and the exit codes. What `--about` and `--update` say
is in [`--about` and `--update`](about-and-updates.md), and the command lines it refuses in [Options](options.md).

## A line for each file

Each is printed when the file is done, numbered in the order they finish. **5/6** is the fifth done of 6.

| | |
|---|---|
| **5/6  2024-07 Lake trip\IMG_0412.CR2 -> IMG_0412.png  (20.5 MB, 2.3 s)** | converted: the photo, the file it became, its size in megabytes of a million bytes, and how long it took |
| **1/6  2024-07 Lake trip\Day 2\MVI_0451.MP4 copied  (0.3 MB, 0.0 s)** | copied as it is, with `--copy-others` |
| **1/7  2024-07 Lake trip\Day 2\IMG_0450.CR2: already there** | left as it was: its file is in the output folder already (see [Running it again](running-again.md)) |
| **6/7  2024-08 Birthday\IMG_0504.CR2: not converted: ....** | failed, and why (below). A file that wasn't copied says **not copied** |

`--quiet` leaves out all of these but the failures.

## Why a file isn't converted

| | |
|---|---|
| **Windows couldn't read it as a photo (it may be damaged, or not a CR2)** | Windows' decoders couldn't make a picture of it: the file is damaged, it isn't really a CR2, or the Raw Image Extension isn't installed (below) |
| **The TIFF would be larger than 4 GB, which TIFF can't hold** | with `--format tiff`, a picture too big for a TIFF |
| anything else | Windows' or .NET's own message: the output folder can't be written to, the disk is full, the file is in use, or there isn't the memory for it (fewer `--jobs` helps) |

The other files go on being converted, and the exit code is 1. No file is left in the output folder for one that
failed, so the next run tries it again.

## When the Raw Image Extension isn't installed

When no photo was converted and at least one couldn't be read as a photo, it ends with this, even with `--quiet`:

```
Windows has no decoder for these CR2 files on this PC. raw-convert uses Microsoft's free Raw Image Extension:
  https://apps.microsoft.com/detail/9NCTDW2W1BH8
Install it from the Microsoft Store, then run raw-convert again: what's done already is left as it is.
```

## The last line

```
1 converted, 5 already there, left as they were, 1 failed, in 0.9 s.
```

| | |
|---|---|
| **1 converted** | always there |
| **1 copied** | with `--copy-others` |
| **5 already there, left as they were** | when there were any |
| **1 failed** | when there were any |
| **stopped before the end** | when it was stopped with **Ctrl+C** |
| **in 0.9 s** | how long it all took: in seconds, then `3 min 20 s`, then `1 h 5 min` |

With `--quiet`, the last line is printed only when a file failed.

## The other messages

| | |
|---|---|
| **There are no CR2 files in ... or the folders in it.** | the input folder, and the folders inside it, have no `.CR2` files. Not a failure: the exit code is 0 |
| **Stopping once the photos being converted are done (Ctrl+C again stops at once)...** | **Ctrl+C** was pressed (see [Running it again](running-again.md)) |
| anything else, alone | it couldn't start at all, such as when the input folder can't be read: Windows' or .NET's own message, and the exit code is 1 |

## Where the lines go

The lines for files done and the last line go to the standard output; the failures, the Raw Image Extension message
and the Ctrl+C message to the error output. So `raw-convert Photos Converted 2> failed.txt` keeps the failures in a file
of their own.

## Exit codes

| | |
|---|---|
| **0** | everything was converted, or was there already (or there was nothing to convert) |
| **1** | a file failed, it was stopped with **Ctrl+C**, or it couldn't start |
| **2** | the command line was wrong: see [Options](options.md) |

`--about` and `--update` give 0 when they could ask GitHub (and, with an update, install it), and 1 when they
couldn't, or when `--update` had nobody to answer its question: see [`--about` and `--update`](about-and-updates.md).

← [Options](options.md) · [Contents](README.md) · [Your data](your-data.md) →

<!-- strongthium-links -->

---

<p align="center"><sub>
<b>Strongthium open-source tools:</b> <a href="https://github.com/strongthium/emudhra-security-patch">eMudhra Security Patch</a> · <a href="https://github.com/strongthium/folder-size">Folder Size</a> · <a href="https://github.com/strongthium/health-reminder">Health Reminder</a> · <a href="https://github.com/strongthium/host-dossier">Host Dossier</a> · <a href="https://github.com/strongthium/outbound-watch">Outbound Watch</a> · <b>RAW Convert</b> · <a href="https://github.com/strongthium/video-dispatch">Video Dispatch</a> · <a href="https://github.com/strongthium/webdav-server">WebDAV Server</a><br>
<b>More from Strongthium:</b> <a href="https://strongthium.com/linux-server-firewall">Guard</a> · <a href="https://strongthium.com/self-hosted-mail-server">Mail</a> · <a href="https://strongthium.com/self-hosted-live-chat">Chat</a> · <a href="https://strongthium.com/windows-ssh-client">Remote</a> · <a href="https://strongthium.com/twilio-sms-desktop-app">SMS</a> · <a href="https://strongthium.com/time-tracking-invoicing-software">Work</a> · <a href="https://strongthium.com/windows-registry-editor">Registry</a> · <a href="https://strongthium.com/cfml-server">DotCF</a> · <a href="https://strongthium.com/video-conferencing-dial-up-modem">Convey</a><br>
<a href="https://strongthium.com">Strongthium Labs</a> · Take back control of your internet.
</sub></p>
