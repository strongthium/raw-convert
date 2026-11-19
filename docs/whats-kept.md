<p align="center">
  <a href="https://strongthium.com" title="Strongthium Labs"><img src="https://raw.githubusercontent.com/strongthium/.github/main/profile/banner-2200x440.png" alt="Strongthium Labs: Take back control of your internet."></a>
</p>

[raw-convert](../README.md) › [Manual](README.md) › What's kept

# What's kept

| | |
|---|---|
| **Size** | the whole picture Windows develops from the RAW, never resized: 5496 × 3670 from a Canon EOS 6D. sRAW and mRAW photos stay at their smaller size |
| **Colour** | 8 bits a colour, red, green and blue, in sRGB, as Windows develops it. A CR2 holds more than 8 bits a colour, which no PNG or JPEG viewer shows; editing the RAW itself is still the way to use all of them |
| **Which way up** | turned the right way up, pixel by pixel, and marked as needing no turn (below) |
| **Details** | the camera's own details of the photo (below) |
| **File dates** | each file gets the created and modified dates of the CR2 it came from, so a folder sorted by date sorts the same |

The CR2 itself is only read, never changed.

## Which way up

A camera held upright, or upside down, writes which way up the photo goes (the Exif orientation, 1 to 8) and leaves
the picture as the sensor took it. `raw-convert` turns (and, for the orientations that need it, mirrors) the picture
by that, moving each pixel as it is, so nothing is lost, and writes the orientation as 1: the right way up already.
So it shows the right way up everywhere, in viewers that read the orientation and in those that don't.

If Windows has already turned a photo itself (its picture is the other way round from the sensor's), it isn't turned
again.

## Details

What the camera wrote about the photo goes into the PNG or TIFF (see [PNG or TIFF](png-or-tiff.md) for where):

| | |
|---|---|
| **Kept** | the make and model of the camera, the date and time taken, the exposure, ISO, focal length, flash, the lens, the GPS position, and the artist, copyright, description and software the camera wrote: the whole Exif and GPS of the CR2 |
| **Changed** | the orientation, written as 1 (above), and the size in pixels, written as the new file's |
| **Left out** | Canon's own maker note, which only means something inside the CR2, and the CR2's thumbnails and previews |

If a CR2's details can't be read, but its picture can, the photo is converted without them.

← [PNG or TIFF](png-or-tiff.md) · [Contents](README.md) · [Options](options.md) →

<!-- strongthium-links -->

---

<p align="center"><sub>
<b>Strongthium open-source tools:</b> <a href="https://github.com/strongthium/emudhra-security-patch">eMudhra Security Patch</a> · <a href="https://github.com/strongthium/folder-size">Folder Size</a> · <a href="https://github.com/strongthium/health-reminder">Health Reminder</a> · <a href="https://github.com/strongthium/host-dossier">Host Dossier</a> · <a href="https://github.com/strongthium/outbound-watch">Outbound Watch</a> · <b>RAW Convert</b> · <a href="https://github.com/strongthium/video-dispatch">Video Dispatch</a> · <a href="https://github.com/strongthium/webdav-server">WebDAV Server</a><br>
<b>More from Strongthium:</b> <a href="https://strongthium.com/linux-server-firewall">Guard</a> · <a href="https://strongthium.com/self-hosted-mail-server">Mail</a> · <a href="https://strongthium.com/self-hosted-live-chat">Chat</a> · <a href="https://strongthium.com/windows-ssh-client">Remote</a> · <a href="https://strongthium.com/twilio-sms-desktop-app">SMS</a> · <a href="https://strongthium.com/time-tracking-invoicing-software">Work</a> · <a href="https://strongthium.com/windows-registry-editor">Registry</a> · <a href="https://strongthium.com/cfml-server">DotCF</a> · <a href="https://strongthium.com/video-conferencing-dial-up-modem">Convey</a><br>
<a href="https://strongthium.com">Strongthium Labs</a> · Take back control of your internet.
</sub></p>
