# OBS Twitch Alerts Action using OBS sound sources

Inspired by the fantastic tutorial by [Mustached Maniac](https://www.youtube.com/channel/UC-OzuJhl8Oriw6gfyPxEhLA).

Video Link: [How To Make Your Own Twitch Alerts in OBS with Streamer Bot]([sdkjdskj](https://youtu.be/6kgFgWlWcTo))

This code should be only followed after checking out and following the tutorial seen there.

## Demo

This feature is *coming soon*

## Basic Installation

Click Import in streamerbot and drag the file `OBSTwitchAlertsOBSAudioClips.sb` into the `Input String` box.

## Advanced Installation

- Create a new Queue in `Action Queues` -> `Queues`
  - Name: `Channel Alerts`
  - Make sure to check `Blocking`
- Create a new Action in `Actions`
  - Name: `Channel Alert`
  - Enabled: `Checked`
  - Group: `Channel Events`
  - Queue: `Channel Alerts`
  - Random Action: `Unchecked`
  - Concurrent: `Unchecked`
  - Always Run: `Unchecked`
  - Exclude from Action Queue: `Unchecked`
- Create Triggers in `Channel Alert` Action
  - Twitch Gift Bomb
  - Twitch Gift Subscription
  - Twitch Follow
  - Twitch Subscription
  - Twitch Resubscription
  - Twitch Cheer
- Create Sub-action `Core` -> `C#` -> `Execute C# Action`
  - Copy the contents of the `OBSTwitchAlerts.cs` and paste it in the Code window.
  - Modify any user properties
    - AlertScene (the name of the OBS Scene where the alert sources are located)
    - AlertSoundSources (map of OBS media sources for each alert type)
    - `GetTwitchMessage()` if you need to, change the messages sent based on event type
    - `GetChannelEvent()` if you need to, change the alert channelEvent text to display in OBS for each event type

## Required framework References (found in .Net installation folder)

Usually located in `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\` but it may vary based on specific version and build installed

- System.dll
- System.Core.dll
- System.Linq.dll
- System.Linq.Queryable.dll
- System.ComponentModel.dll
- System.ComponentModel.EventBasedAsync.dll
- netstandard.dll
