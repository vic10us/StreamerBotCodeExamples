using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.ComponentModel;
using System.Threading;

public class CPHInline
{
    /// <summary>
    /// This is the scene name that contains the Sources
    /// eventUser: Text (GDI+)
    /// channelEvent: Text (GDI+)
    /// profilePic: Browser source. This is the placeholder for the users profile image.
    /// alertRibbon: any visible element, usually color source. be creative
    /// </summary>
    public string AlertScene = "[S] Alerts";
    
    /// <summary>
    /// This is the mapping of twitch event types to the specific sound to be played.
    /// The key is the event type and the value is the sound file name.
    /// If the event type is not found in the dictionary, the default sound will be played.
    /// The default sound is "newSub.mp3".
    /// </summary>
    public Dictionary<string, string> AlertSoundSources = new Dictionary<string, string>()
    {
        {
            "TwitchGiftBomb", "giftBombSound"
        },
        {
            "TwitchGiftSub", "giftSubSound"
        },
        {
            "TwitchFollow", "followSound"
        },
        {
            "TwitchSub", "newSubSound"
        },
        {
            "TwitchReSub", "reSubSound"
        },
        {
            "TwitchCheer", "cheerSound"
        },
        {
            "Default", "cheerSound"
        }
    };

    /// <summary>
    /// If this is set to true, verbose logging will happen ;)
    /// </summary>
    public bool DebugMode = false;

    public bool Execute()
    {
        // this will only run if DebugMode = true
        DumpArgs();
        
        // This is needed to get the user profile image url
        var userInfo = CPH.TwitchGetExtendedUserInfoByLogin(args["userName"].ToString());
        
        // This is the type of alert that was triggered
        string alertType = args["__source"].ToString();
        // This is the user that triggered the alert
        string targetUser = userInfo.UserName;
        // This is the url to the users profile image
        string targetUserProfileImageUrl = userInfo.ProfileImageUrl;

        // This is a browser source that is set to the users profile image
        CPH.ObsSetBrowserSource(AlertScene, "profilePic", targetUserProfileImageUrl);

        // Get the sound source in OBS for the alert type
        var eventSoundSource = GetEventSoundSource(alertType);
        // Play the sound
        CPH.ObsSetSourceVisibility(AlertScene, eventSoundSource, true);

        // Get the text for the alert
        var channelEventText = GetChannelEvent(alertType);
        LogMessage(channelEventText);

        // Get the text for the twitch chat message
        var twitchMessage = GetTwitchMessage(alertType, targetUser);
        LogMessage(twitchMessage);

        // Set the text for the targetUser
        CPH.ObsSetGdiText(AlertScene, "eventUser", targetUser);
        // Set the text for the channelEvent
        CPH.ObsSetGdiText(AlertScene, "channelEvent", channelEventText);
        // Show the profilePic source
        CPH.ObsSetSourceVisibility(AlertScene, "profilePic", true);
        // Wait 500ms for the animation to complete
        CPH.Wait(500);
        // Show the alertRibbon source
        CPH.ObsSetSourceVisibility(AlertScene, "alertRibbon", true);
        // Show the eventUser source
        CPH.ObsSetSourceVisibility(AlertScene, "eventUser", true);
        // Show the channelEvent source
        CPH.ObsSetSourceVisibility(AlertScene, "channelEvent", true);
        // Send the twitch chat message
        CPH.SendMessage(twitchMessage, true);
        // Wait for the sound to complete (7 seconds should be enough)
        CPH.Wait(7000);

        // Stop the sound
        CPH.ObsSetSourceVisibility(AlertScene, eventSoundSource, false);

        // Hide the alertRibbon source
        CPH.ObsSetSourceVisibility(AlertScene, "channelEvent", false);
        // Hide the eventUser source
        CPH.ObsSetSourceVisibility(AlertScene, "eventUser", false);
        // Hide the profilePic source
        CPH.ObsSetSourceVisibility(AlertScene, "alertRibbon", false);
        // Wait 500ms for the animation to complete
        CPH.Wait(500);
        // Hide the profilePic source
        CPH.ObsSetSourceVisibility(AlertScene, "profilePic", false);
        return true;
    }

    private void LogMessage(string message)
    {
        if (!DebugMode)
            return;
        CPH.LogDebug(message);
    }

    private void DumpArgs()
    {
        if (!DebugMode) return;
        foreach (var arg in args)
        {
            CPH.LogDebug(string.Format("Key: '{0}', Value: '{1}'", arg.Key, arg.Value));
        }
    }

    /// <summary>
    /// This is where the twitch chat message is generated.
    /// This is where you can customize the message to your liking.
    /// </summary>
    /// <param name="alertType">The alertType is the type of alert that was triggered.</param>
    /// <param name="targetUser">The targetUser is the user that triggered the alert.</param>
    /// <returns>
    /// The message that will be sent to twitch chat.
    /// </returns>
    private string GetTwitchMessage(string alertType, string targetUser)
    {
        switch (alertType)
        {
            case "TwitchGiftBomb":
                var gifts = args["gifts"].ToString();
                return string.Format("@{0} just gifted {1} subs to the channel!", targetUser, gifts);
            case "TwitchGiftSub":
                var recipientUserName = args["recipientUserName"].ToString();
                return string.Format("@{0} just gifted a sub to @{1}!", targetUser, recipientUserName);
            case "TwitchFollow":
                return string.Format("@{0} just dropped the follow!  Welcome in you MANiAC!", targetUser);
            case "TwitchSub":
                var tier = args["tier"].ToString();
                var x = (tier == "prime") ? "with prime" : "at " + tier;
                return string.Format("@{0} just subscribed {1}!", targetUser, x);
            case "TwitchReSub":
                var cumulative = args["cumulative"].ToString();
                return string.Format("@{0} just resubscribed for a total of {1} months!", targetUser, cumulative);
            case "TwitchCheer":
                var bits = args["bits"].ToString();
                return string.Format("@{0} just cheered {1} bits!", targetUser, bits);
            default:
                return string.Format("No idea what @{0} just did!!", targetUser);
        }
    }

    /// <summary>
    /// This is where the channelEvent text is generated.
    /// This is where you can customize the message to your liking.
    /// </summary>
    /// <param name="alertType">The alertType is the type of alert that was triggered.</param>
    /// <returns>
    /// The message that will be displayed in the channelEvent source.
    /// </returns>
    private string GetChannelEvent(string alertType)
    {
        switch (alertType)
        {
            case "TwitchGiftBomb":
                var gifts = args["gifts"].ToString();
                return string.Format("Gifted {0} subs!!", gifts);
            case "TwitchGiftSub":
                return "Gifted a sub!!";
            case "TwitchFollow":
                return "Just Followed!!";
            case "TwitchSub":
                return "Just Subscribed!!";
            case "TwitchReSub":
                return "Just Resubscribed!!";
            case "TwitchCheer":
                var bits = args["bits"].ToString();
                return string.Format("Cheered {0} bits!!", bits);
            default:
                return "No idea what just happened!!";
        }
    }

    /// <summary>
    /// This is where the sound file is selected.
    /// If the alertType is not found in the AlertSoundFiles dictionary, the default sound will be played.
    /// </summary>
    /// <param name="alertType">The alertType is the type of alert that was triggered.</param>
    /// <returns>
    /// The full path to the sound file.
    /// </returns>
    private string GetEventSoundSource(string alertType)
    {
        AlertSoundSources.TryGetValue(alertType, out var soundSource);
        if (string.IsNullOrWhiteSpace(soundSource))
            soundSource = AlertSoundSources["Default"];
        return soundSource;
    }
}