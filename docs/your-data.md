<p align="center">
  <a href="https://strongthium.com" title="Strongthium Labs"><img src="https://raw.githubusercontent.com/strongthium/.github/main/profile/banner-2200x440.png" alt="Strongthium Labs: Take back control of your internet."></a>
</p>

[raw-convert](../README.md) › [Manual](README.md) › Your data

# Your data

Your photos never leave your PC, and nothing about them is sent anywhere or saved.

| | |
|---|---|
| **What it reads** | the input folder: its CR2 files, and with `--copy-others` its other files. They're only read, never changed |
| **What it writes** | only inside the output folder: the PNG or TIFF files, each first as a `.partial` file beside its final name, the copies made with `--copy-others`, and the folders they go in |
| **What it keeps** | nothing: no settings, no log and no list of what it has done. Running it again looks at what's in the output folder |
| **What it sends** | nothing, unless you run `--about` or `--update`, which ask GitHub for the latest version (below) |

Each photo is developed by Windows, on the PC, with its Raw Image Extension.

## What goes into the files it makes

The camera's details of each photo go with it, the GPS position where it was taken among them (see
[What's kept](whats-kept.md)). They're in the PNG or TIFF as they were in the CR2, so whoever you give the files to,
or the service you upload them to, can read them as well.

## `--about` and `--update`

They're the only times `raw-convert` goes on the Internet. `api.github.com` sees that the question came from your
address, and which program and version asked. `--update` then downloads the release from GitHub into a folder in your
temporary folder, which it deletes afterwards, and replaces `raw-convert.exe` in its own folder (see
[`--about` and `--update`](about-and-updates.md)).

← [Messages and exit codes](messages.md) · [Contents](README.md)

<!-- strongthium-links -->

---

<p align="center"><sub>
<b>Strongthium open-source tools:</b> <a href="https://github.com/strongthium/emudhra-security-patch">eMudhra Security Patch</a> · <a href="https://github.com/strongthium/folder-size">Folder Size</a> · <a href="https://github.com/strongthium/health-reminder">Health Reminder</a> · <a href="https://github.com/strongthium/host-dossier">Host Dossier</a> · <a href="https://github.com/strongthium/outbound-watch">Outbound Watch</a> · <b>RAW Convert</b> · <a href="https://github.com/strongthium/video-dispatch">Video Dispatch</a> · <a href="https://github.com/strongthium/webdav-server">WebDAV Server</a><br>
<b>More from Strongthium:</b> <a href="https://strongthium.com/linux-server-firewall">Guard</a> · <a href="https://strongthium.com/self-hosted-mail-server">Mail</a> · <a href="https://strongthium.com/self-hosted-live-chat">Chat</a> · <a href="https://strongthium.com/windows-ssh-client">Remote</a> · <a href="https://strongthium.com/twilio-sms-desktop-app">SMS</a> · <a href="https://strongthium.com/time-tracking-invoicing-software">Work</a> · <a href="https://strongthium.com/windows-registry-editor">Registry</a> · <a href="https://strongthium.com/cfml-server">DotCF</a> · <a href="https://strongthium.com/video-conferencing-dial-up-modem">Convey</a><br>
<a href="https://strongthium.com">Strongthium Labs</a> · Take back control of your internet.
</sub></p>
