using System;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NAudio.WinForms;
using NAudio.Vorbis;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.ComponentModel;
using System.Threading;

/// <summary>
///   Display Twitch Alerts in OBS
/// </summary>
/// <triggers>
///   <trigger source="Twitch" type="Gift Bomb" criteria="Any,Subs Gifted:Any" enabled="Yes" />
///   <trigger source="Twitch" type="Gift Subscription" criteria="Any,Milestone:Any" enabled="Yes" />
///   <trigger source="Twitch" type="Follow" criteria="" enabled="Yes" />
///   <trigger source="Twitch" type="Subscription" criteria="Any" enabled="Yes" />
///   <trigger source="Twitch" type="Resubscription" criteria="Any,Cumulative:Any" enabled="Yes" />
///   <trigger source="Twitch" type="Cheer" criteria="Any" enabled="Yes" />
/// </triggers>
/// <variables>
/// </variables>
/// <references>
///   <reference source="netstandard.dll" />
///   <reference source="System.Linq.dll" />
///   <reference source="System.Linq.Queryable.dll" />
///   <reference source="System.ComponentModel.dll" />
///   <reference source="System.ComponentModel.EventBasedAsync.dll" />
///   <reference source="System.dll" />
///   <reference source="netstandard.dll" />
///   <reference source="NAudio.dll" />
///   <reference source="NAudio.Core.dll" />
///   <reference source="NAudio.Wasapi.dll" />
///   <reference source="NAudio.Vorbis.dll" />
///   <reference source="NAudio.WinMM.dll" />
///   <reference source="NAudio.WinForms.dll" />
/// </references>
/// <settings name="OBSTwitchAlerts" 
///           description="Display Twitch Alerts in OBS" 
///           keepInstanceActive="false"
///           precompileOnApplicationStart="true" 
///           delayedStart="false" 
///           saveResultToVariable="false"
///           variableName="" />
public class CPHInline
{
    /// <summary>
    /// This is the audio device that will be used to play the alert sounds.
    /// If this is left null, the default audio device will be used.
    /// To get a list of all available devices, set DebugMode to true and run the script.
    /// The list of devices will be in the log file.
    /// </summary>
    public string AudioDevice = null; // Example: "VoiceMeeter Input (VB-Audio VoiceMeeter VAIO)";

    /// <summary>
    /// This is the path where your alert sounds are located.
    /// The sounds should be in mp3 format.
    /// The sound file names should match the event types in the AlertSoundFiles dictionary. 
    /// </summary>
    public string SoundsPath = @"M:\Streaming\OBS\Alerts\";
    
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
    public Dictionary<string, string> AlertSoundFiles = new Dictionary<string, string>()
    {
        {
            "TwitchGiftBomb", "giftBomb.mp3"
        },
        {
            "TwitchGiftSub", "giftSub.mp3"
        },
        {
            "TwitchFollow", "newFollow.mp3"
        },
        {
            "TwitchSub", "newSub.mp3"
        },
        {
            "TwitchReSub", "reSub.mp3"
        },
        {
            "TwitchCheer", "cheer.mp3"
        },
        {
            "Default", "newSub.mp3"
        }
    };

    /// <summary>
    /// If this is set to true, verbose logging will happen ;)
    /// </summary>
    public bool DebugMode = false;

    private IWavePlayer wavePlayer;
    private AudioFileReader audioFileReader;

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

        // Log all the audio devices
        // This is useful for finding the correct device name
        // if you want to use a specific device
        LogAllDevices();
        // Get the sound file for the alert type
        var eventSound = GetEventSound(alertType);
        // Play the sound
        PlaySound(eventSound);

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
    private string GetEventSound(string alertType)
    {
        AlertSoundFiles.TryGetValue(alertType, out var soundFile);
        if (string.IsNullOrWhiteSpace(soundFile))
            soundFile = AlertSoundFiles["Default"];
        return Path.Combine(SoundsPath, soundFile);
    }

    /// <summary>
    /// This is where the sound is played.
    /// </summary>
    /// <param name="filename">The full path to the sound file.</param>
    private void PlaySound(string filename)
    {
        if (!File.Exists(filename)) {
            CPH.LogError($"Sound File not found: {filename}");
            return; 
        }
        using (var bw = new BackgroundWorker())
        {
            bw.WorkerReportsProgress = true;
            bw.DoWork += new DoWorkEventHandler(delegate (object o, DoWorkEventArgs args)
            {
                BackgroundWorker b = o as BackgroundWorker;
                var device = GetDevice(AudioDevice);
                LogMessage($"DataFlow: {device.DataFlow} FriendlyName: '{device.FriendlyName}' DeviceFriendlyName: '{device.DeviceFriendlyName}' Status: {device.State}");
                using (var audioFile = new AudioFileReader(filename))
                using (var outputDevice = new WasapiOut(device, AudioClientShareMode.Shared, true, 0))
                {
                    outputDevice.Init(audioFile);
                    outputDevice.Play();
                    LogMessage($"BitsPerSample: {outputDevice.OutputWaveFormat.BitsPerSample}");
                    LogMessage($"Channels: {outputDevice.OutputWaveFormat.Channels}");
                    LogMessage($"SampleRate: {outputDevice.OutputWaveFormat.SampleRate}");
                    LogMessage($"SampleRate: {audioFile.Length}");
                    while (outputDevice.PlaybackState == PlaybackState.Playing)
                    {
                        var pos = outputDevice.GetPosition();
                        double pctComplete = ((double)pos / audioFile.Length) * 100;
                        LogMessage($"Posistion: {pctComplete} [{pos}/{audioFile.Length}]");
                        b.ReportProgress((int)pctComplete);
                        CPH.Wait(500);
                    }

                    b.ReportProgress(100);
                }
            });
            bw.ProgressChanged += new ProgressChangedEventHandler(delegate (object o, ProgressChangedEventArgs args)
            {
                LogMessage(string.Format("{0}% Completed", args.ProgressPercentage));
            });
            bw.RunWorkerAsync();
        }
    }

    /// <summary>
    /// This is where the audio device is selected.
    /// If the deviceName is null or empty, the default audio device will be used.
    /// To get a list of all available devices, set DebugMode to true and run the script.
    /// The list of devices will be in the log file.
    /// </summary>
    /// <param name="deviceName">The name of the audio device to use.</param>
    /// <returns>
    /// The MMDevice that will be used to play the sound.
    /// </returns>
    private MMDevice GetDevice(string deviceName)
    {
        var enumerator = new MMDeviceEnumerator();
        if (string.IsNullOrWhiteSpace(deviceName))
            return enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
        var device = devices.FirstOrDefault(wasapi => wasapi.FriendlyName.Equals(deviceName) || wasapi.DeviceFriendlyName.Equals(deviceName));
        return device;
    }

    /// <summary>
    /// This is where all the audio devices are logged.
    /// This is useful for finding the correct device name
    /// if you want to use a specific device.
    /// To get a list of all available devices, set DebugMode to true and run the script.
    /// </summary>
    private void LogAllDevices()
    {
        if (!DebugMode)
            return;
        var enumerator = new MMDeviceEnumerator();
        var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
        foreach (var wasapi in devices)
        {
            try
            {
                CPH.LogInfo($"ID: {wasapi.ID} DataFlow: {wasapi.DataFlow} FriendlyName: '{wasapi.FriendlyName}' DeviceFriendlyName: '{wasapi.DeviceFriendlyName}' Status: {wasapi.State}");
            }
            catch (Exception ex)
            {
                CPH.LogError(ex.Message);
            }
        }
    }
}