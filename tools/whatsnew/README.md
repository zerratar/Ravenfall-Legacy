# What's new

`Shinobytes.UI.WhatsNew` shows a few pages about the version's new things the first time a streamer runs it (once per
streamer per `WhatsNew.Version` on the PC; settings, Interface, "What's new" opens it again). It uses the same window as
Ravebridge's introduction (`IntroWindow`).

For a new version: change `WhatsNew.Version`, write the pages (`UI_WHATSNEW_*` in Localization.cs), and draw their pictures
with `whatsnew-pictures.cs` (Image Generator, the user's key; it skips pictures already in Temp/ImageGen/whatsnew). Save
them as JPGs in `Assets/World/WhatsNew/Resources/WhatsNew/whatsnew-N.jpg` (1792x864) with the import settings of
Ravebridge's intro pictures (no mipmaps, no power-of-two scaling).
