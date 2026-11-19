<p align="center">
  <a href="https://strongthium.com" title="Strongthium Labs"><img src="https://raw.githubusercontent.com/strongthium/.github/main/profile/banner-2200x440.png" alt="Strongthium Labs: Take back control of your internet."></a>
</p>

[raw-convert](../README.md) › [Manual](README.md) › PNG or TIFF

# PNG or TIFF

```
raw-convert Photos "Photos for Amazon"                  # PNG, the default
raw-convert Photos "Photos for Amazon" --format tiff    # TIFF
```

`--format` is `png` or `tiff` (`tif` does too, in capitals or not). A PNG is named `.png`, and a TIFF `.tif`. Amazon
Photos takes both, and both are lossless: the file holds exactly the picture Windows developed from the RAW, pixel for
pixel, turned the right way up. Neither is resized or compressed the way a JPEG is.

| | PNG (the default) | TIFF (`--format tiff`) |
|---|---|---|
| **Pixels** | 8-bit red, green and blue, marked as sRGB | 8-bit red, green and blue |
| **Compression** | deflate, with each row filtered in whichever of PNG's five ways leaves the smallest numbers | deflate ("Adobe Deflate", compression 8), in strips of about 256 KB, after horizontal differencing (predictor 2) |
| **Size** of a 20-megapixel Canon EOS 6D photo | 20.5 MB | 22.4 MB |
| **The photo's details** | the whole Exif and GPS in an `eXIf` chunk; the date taken in a **Creation Time** text; and the date taken, camera and lens again in XMP, which is where Windows (Explorer, Photos) looks in a PNG | the whole Exif and GPS, in directories of their own, as in the camera's own file |

Which details are kept, and which aren't, is in [What's kept](whats-kept.md).

## Which to choose

PNG, unless something you'll use the photos with wants TIFF. A PNG is a little smaller; a TIFF holds the photo's
details the way the camera's own file does.

To have both, run it twice, into two output folders, or into the same one: a `.png` and a `.tif` of the same photo
don't have the same name, so neither is taken for the other.

← [Running it again](running-again.md) · [Contents](README.md) · [What's kept](whats-kept.md) →

<!-- strongthium-links -->

---

<p align="center"><sub>
<b>Strongthium open-source tools:</b> <a href="https://github.com/strongthium/emudhra-security-patch">eMudhra Security Patch</a> · <a href="https://github.com/strongthium/folder-size">Folder Size</a> · <a href="https://github.com/strongthium/health-reminder">Health Reminder</a> · <a href="https://github.com/strongthium/host-dossier">Host Dossier</a> · <a href="https://github.com/strongthium/outbound-watch">Outbound Watch</a> · <b>RAW Convert</b> · <a href="https://github.com/strongthium/video-dispatch">Video Dispatch</a> · <a href="https://github.com/strongthium/webdav-server">WebDAV Server</a><br>
<b>More from Strongthium:</b> <a href="https://strongthium.com/linux-server-firewall">Guard</a> · <a href="https://strongthium.com/self-hosted-mail-server">Mail</a> · <a href="https://strongthium.com/self-hosted-live-chat">Chat</a> · <a href="https://strongthium.com/windows-ssh-client">Remote</a> · <a href="https://strongthium.com/twilio-sms-desktop-app">SMS</a> · <a href="https://strongthium.com/time-tracking-invoicing-software">Work</a> · <a href="https://strongthium.com/windows-registry-editor">Registry</a> · <a href="https://strongthium.com/cfml-server">DotCF</a> · <a href="https://strongthium.com/video-conferencing-dial-up-modem">Convey</a><br>
<a href="https://strongthium.com">Strongthium Labs</a> · Take back control of your internet.
</sub></p>
