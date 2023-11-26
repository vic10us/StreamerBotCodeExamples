# YouTube Music Desktop App Integration

This plugin integrates Streamer.bot with the YouTube Music Desktop App via the Socket.io interface. Due to the lack of native Socket.io support, we will be using the raw websocket connection and a manual keepalive (ping) so the socket doesn't close on us. 😊

## Demo

This feature is *coming soon* to a YouTube near you 😁

## Basic Installation

### Import Scene In OBS

- I use the Ubuntu font in the sample scenes provided, so go [here][ubuntu-font] to download and install them. The Ubuntu and Ubuntu Mono fonts
- Make sure to download and install the [Source Copy][source-copy] plugin into OBS if you haven't already
- Install the obs-shaderfilter plugin [OBS ShaderFilter Plugin][obs-shader] while not needed for the basic template, it is for the ✨`fancy`✨ one.
- In OBS Choose `Tools` -> `Source Copy` -> `Load Scene` from the menu
- Locate the file `MediaPlayer-1080p-(basic).json` (*or 1440p if your OBS setup is configured for 1440p*) then click the `Open` button

### Import Actions in Streamer.bot

- Click Import in streamerbot and depending on which version of YouTube Music Desktop App you are running, drag the corresponding file(s) `YouTRubeMusicDesktopV[X].sb` into the `Input String` box.

## Advanced Installation

TODO

## Required framework References (found in .Net installation folder)

Usually located in `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\` but it may vary based on specific version and build installed

- System.dll
- System.Core.dll
- System.Linq.dll
- System.Linq.Queryable.dll
- System.ComponentModel.dll
- System.ComponentModel.EventBasedAsync.dll
- netstandard.dll

## OBS Plugins Used

- [OBS Source Copy][source-copy] [*[GitHub][source-copy-gh]*]
- [OBS ShaderFilter Plugin][obs-shader] [*[GitHub][obs-shader-gh]*]

[source-copy]: https://obsproject.com/forum/resources/source-copy.1261/
[source-copy-gh]: https://github.com/exeldro/obs-source-copy
[obs-shader]: https://obsproject.com/forum/resources/obs-shaderfilter.1736/
[obs-shader-gh]: https://github.com/exeldro/obs-shaderfilter/
[ubuntu-font]: https://fonts.google.com/?query=Ubuntu
