using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

/// <summary>
///   Updates the OBS Media Player when updates are received 
///   by the YouTube Muisic Desktop App Socket.io webservice.
/// </summary>
/// <triggers>
///   <trigger source="Websocket Client" type="Websocket Client Opened" criteria="ytmWS" enabled="Yes" />
///   <trigger source="Websocket Client" type="Websocket Client Closed" criteria="ytmWS" enabled="Yes" />
///   <trigger source="Websocket Client" type="Websocket Client Message" criteria="ytmWS" enabled="Yes" />
/// </triggers>
/// <variables>
///   <variable name="ytmDesktopAuthToken" type="string" scope="global" persisted="true" />
///   <variable name="isYTMDesktopAuthenticated" type="bool" scope="global" persisted="false" />
/// </variables>
/// <references>
///   <reference source="netstandard.dll" />
///   <reference source="System.Linq.dll" />
///   <reference source="System.Linq.Queryable.dll" />
///   <reference source="System.ComponentModel.dll" />
///   <reference source="System.ComponentModel.EventBasedAsync.dll" />
///   <reference source="System.dll" />
///   <reference source="netstandard.dll" />
/// </references>
/// <settings name="YouTubeMusicDesktopV2" 
///           description="Updates the OBS Media Player when updates are received by the YouTube Muisic Desktop App Socket.io webservice" 
///           keepInstanceActive="false"
///           precompileOnApplicationStart="true" 
///           delayedStart="false" 
///           saveResultToVariable="false"
///           variableName="" />
public class CPHInline
{
    /// <summary>
    /// The name of the plugin
    /// </summary>
    public const string NAME = "YouTubeMusicDesktopV2";

    /// <summary>
    /// Main scene containing the media player elements
    /// </summary>
    public const string MediaPlayerSceneName = "[VS] Media Player";

    /// <summary>
    /// Scene containing the progress bar
    /// </summary>
    public const string MediaProgressBarSceneName = "[VS] MediaProgressBar";

    /// <summary>
    /// Source name for the media picture
    /// </summary>
    public const string MediaPictureSourceName = "mediaPicture";

    /// <summary>
    /// Source name for the media position
    /// </summary>
    public const string MediaPositionSourceName = "mediaPosition";

    /// <summary>
    /// Source name for the media title
    /// </summary>
    public const string MediaTitleSourceName = "mediaTitle";

    /// <summary>
    /// Source name for the media album
    /// </summary>
    public const string MediaAlbumSourceName = "mediaAlbum";

    /// <summary>
    /// Source name for the media progress bar
    /// </summary>
    public const string MediaProgressBarSourceName = "progressBar";

    /// <summary>
    /// Whether or not to log debug messages
    /// </summary>
    public const bool DEBUG = false;

    /// <summary>
    /// Name of the property to change in the progress bar scene item transform
    /// </summary>
    public const string MediaProgressBarSceneItemTransformBoundsWidthName = "boundsWidth";

	public bool Execute()
	{
        switch (args["triggerName"].ToString())
        {
            case "Websocket Client Opened":
                if (DEBUG) CPH.LogInfo("Got Websocket Client Opened");
                return OnWebsocketClientOpened();
            case "Websocket Client Closed":
                if (DEBUG) CPH.LogInfo("Got Websocket Client Closed");
                return OnWebsocketClientClosed();
            case "Websocket Client Message":
                if (DEBUG) CPH.LogInfo("Got Websocket Client Message");
                return OnWebsocketClientMessage();
            default:
                return true;
        }
    }

    public bool OnWebsocketClientOpened()
    {
		// need to login
        // get the auth token from the global variable ytmDesktopAuthToken
		var authToken = CPH.GetGlobalVar<string>("ytmDesktopAuthToken", true);
		var authMessage = "40/api/v1/realtime,{\"token\":\"" + authToken + "\"}";
		CPH.LogError(JsonConvert.SerializeObject(args));
		var connectArgs = GetArgumentObject<WebSocketConnectArgs>();
		CPH.WebsocketSend(authMessage, connectArgs.wsIdx);
		CPH.SetGlobalVar("isYTMDesktopAuthenticated", true, false);
		return false;
	}
	
    public bool OnWebsocketClientClosed()
	{
		CPH.SetGlobalVar("isYTMDesktopAuthenticated", false, false);
		return false;
	}

    public bool OnWebsocketClientMessage() {
        var messageArgs = GetArgumentObject<WebSocketMessageArgs>();
        var actionType = ExtractNumber(messageArgs.message);
        if (DEBUG) CPH.LogInfo($"Got actionType: {actionType}");
        switch (actionType) {
            case 2:
                // ping
                return ProcessPing(messageArgs);
            case 42:
                // message payload
                return ProcessMessage(messageArgs);
            default:
                // not a handled message
                return true;
        }
    }

    public bool ProcessPing(WebSocketMessageArgs messageArgs) {
        if (DEBUG) CPH.LogInfo("Got PING -> Send PONG");
        CPH.WebsocketSend("3", messageArgs.wsIdx);
        return false;
    }

    public bool ProcessMessage(WebSocketMessageArgs messageArgs) {
        var x = GetMediaEvent(messageArgs.message);
        if (x == null) return true; // This is not a media event
        var json = JsonConvert.SerializeObject(x);
        var lastMediaEventJson = CPH.GetGlobalVar<string>("lastYTMDesktopV2MediaEvent", false);
        var lastMediaEvent = new MediaEvent();
        if (!string.IsNullOrWhiteSpace(lastMediaEventJson)) {
            lastMediaEvent = JsonConvert.DeserializeObject<MediaEvent>(lastMediaEventJson);
		}
        var updated = false;
		var lastCover = GetThumbnailUrl(lastMediaEvent.video?.thumbnails);
		var cover = GetThumbnailUrl(x.video?.thumbnails);
        if (lastCover != cover) {
            CPH.ObsSetBrowserSource(MediaPlayerSceneName, MediaPictureSourceName, cover);
            updated = true;
        }
        var lastPosition = lastMediaEvent.player.videoProgress;
		var position = x.player.videoProgress;
        if (lastPosition != position) {
            var statePercent = x.player.videoProgress / x.video.durationSeconds;
			SetProgressBar(statePercent);
			var posTimeSpan = TimeSpan.FromSeconds(position);
			var positionHuman = $"{posTimeSpan:m':'ss}";
			var durationTimeSpan = TimeSpan.FromSeconds(x.video.durationSeconds);
			var durationHuman = $"{durationTimeSpan:m':'ss}";
			CPH.ObsSetGdiText(MediaPlayerSceneName, MediaPositionSourceName, $"  {positionHuman} / {durationHuman}  ");
			updated = true;
        }
		if (lastMediaEvent.video.title != x.video.title) {
			CPH.ObsSetGdiText(MediaPlayerSceneName, MediaTitleSourceName, x.video.title);
			updated = true;
		}
		if (lastMediaEvent.video.album != x.video.album && !string.IsNullOrWhiteSpace(x.video.album)) {
			CPH.ObsSetGdiText(MediaPlayerSceneName, MediaAlbumSourceName, $"{x.video.author} • {x.video.album}");
			updated = true;
		}
        if (updated) CPH.SetGlobalVar("lastYTMDesktopV2MediaEvent", json, false);
        return false;
    }

    public void SetProgressBar(double pos) 
	{
		var sceneListRequest = new GetSceneItemListRequestData() {
			sceneName = MediaProgressBarSceneName
		};
		var sceneListRequestJson = JsonConvert.SerializeObject(sceneListRequest);
		var sceneListJson = CPH.ObsSendRaw("GetSceneItemList", sceneListRequestJson);
		var sceneList = JsonConvert.DeserializeObject<SceneItemList>(sceneListJson);
		var progressBarItem = sceneList.sceneItems.FirstOrDefault(c => c.sourceName.Equals(MediaProgressBarSourceName));
		var barWidth = progressBarItem.sceneItemTransform.sourceWidth;
		int progressWidth = (int)Math.Round(barWidth * pos);
		var sceneItemTransformRequest = new SetSceneItemTransformRequestData() {
			sceneName = MediaProgressBarSceneName,
			sceneItemId = progressBarItem.sceneItemId,
			sceneItemTransform = new SceneItemTransformBoundsWidth() 
			{
				boundsWidth = progressWidth
			}
		};
		var sceneItemTransformRequestJson = JsonConvert.SerializeObject(sceneItemTransformRequest);
		CPH.ObsSendRaw("SetSceneItemTransform", sceneItemTransformRequestJson);
	}

	public string GetThumbnailUrl(Thumbnail[]? thumbnails)
	{
		if (thumbnails == null || thumbnails.Length <= 0) return "";
		var last = thumbnails.LastOrDefault(x => !string.IsNullOrWhiteSpace(x.url));
		if (last == null) return "";
		return last.url;
	}

    public MediaEvent? GetMediaEvent(string message) 
	{
		var messagePrefix = "42/api/v1/realtime,[\"state-update\",";
		if (!message.StartsWith(messagePrefix)) return null;
		var start = messagePrefix.Length;
		var end = message.Length-start-1;
		var eventJson = message.Substring(start, end);
		try
        {
            var @event = JsonConvert.DeserializeObject<MediaEvent>(eventJson);
            return @event;
		}
        catch (Exception ex) 
		{
			CPH.LogError("SOMETHING BAD HAPPENED");
			CPH.LogError(ex.StackTrace);
			return null;
		}
	}

    public T? GetArgumentObject<T>() {
        var json = JsonConvert.SerializeObject(args);
        var result = JsonConvert.DeserializeObject<T>(json);
        return result;
    }

    static int ExtractNumber(string input)
    {
        int endIndex = 0;

        // Iterate through characters at the beginning of the string
        while (endIndex < input.Length && Char.IsDigit(input[endIndex]))
        {
            endIndex++;
        }

        if (endIndex > 0)
        {
            // Extract the number substring and convert it to an integer
            string numberString = input.Substring(0, endIndex);
            return int.Parse(numberString);
        }

        // Return -1 if no number is found
        return -1;
    }
}

/// <summary>
/// Models/Entities for Youtube Music Desktop App
/// Do not modify unless you know what you are doing
/// </summary>

public class MediaEvent
{
    public Player? player { get; set; } = new Player();
    public Video? video { get; set; } = new Video();
    public string playlistId { get; set; }
}

public class Player
{
    public int trackState { get; set; }
    public float videoProgress { get; set; }
    public int volume { get; set; }
    public bool? adPlaying { get; set; }
    public Queue? queue { get; set; }
}

public class Queue
{
    public bool? autoplay { get; set; }
    public Item[]? items { get; set; }
//    public object[] automixItems { get; set; }
    public bool? isGenerating { get; set; }
    public bool? isInfinite { get; set; }
    public int repeatMode { get; set; }
    public int selectedItemIndex { get; set; }
}

public class Item
{
    public Thumbnail[] thumbnails { get; set; }
    public string title { get; set; }
    public string author { get; set; }
    public string duration { get; set; }
    public bool? selected { get; set; }
    public string videoId { get; set; }
    public Item[]? counterparts { get; set; }
}

public class Thumbnail
{
    public string url { get; set; }
    public int width { get; set; }
    public int height { get; set; }
}

public class Video
{
    public string author { get; set; }
    public string channelId { get; set; }
    public string title { get; set; }
    public string album { get; set; }
    public string albumId { get; set; }
    public int likeStatus { get; set; }
    public Thumbnail[]? thumbnails { get; set; }
    public int durationSeconds { get; set; }
    public string id { get; set; }
}

public class GetSceneItemListRequestData
{
    public string sceneName { get; set; }
}

public class SetSceneItemTransformRequestData
{
    public string sceneName { get; set; }
    public int sceneItemId { get; set; }
    public SceneItemTransformBoundsWidth sceneItemTransform { get; set; }
}

public class SceneItemTransformBoundsWidth
{
    public int boundsWidth { get; set; }
}

public class SceneItemList {
    public List<SceneItemListItem> sceneItems { get; set; }
}

public class SceneItemListItem
{
    public object inputKind { get; set; }
    public bool? isGroup { get; set; }
    public string sceneItemBlendMode { get; set; }
    public bool? sceneItemEnabled { get; set; }
    public int sceneItemId { get; set; }
    public int sceneItemIndex { get; set; }
    public bool? sceneItemLocked { get; set; }
    public SceneItemTransform sceneItemTransform { get; set; }
    public string sourceName { get; set; }
    public string sourceType { get; set; }
}

public class SceneItemTransform
{
    public int alignment { get; set; }
    public int boundsAlignment { get; set; }
    public float boundsHeight { get; set; }
    public string boundsType { get; set; }
    public float boundsWidth { get; set; }
    public int cropBottom { get; set; }
    public int cropLeft { get; set; }
    public int cropRight { get; set; }
    public int cropTop { get; set; }
    public float height { get; set; }
    public float positionX { get; set; }
    public float positionY { get; set; }
    public float rotation { get; set; }
    public float scaleX { get; set; }
    public float scaleY { get; set; }
    public float sourceHeight { get; set; }
    public float sourceWidth { get; set; }
    public float width { get; set; }
}

public class BaseTriggerArgs {
    public int __source { get; set; }
    public string triggerId { get; set; }
    public string triggerName { get; set; }
    public string triggerCategory { get; set; }
    public string actionId { get; set; }
    public string actionName { get; set; }
    public string eventSource { get; set; }
    public string runningActionId { get; set; }
    public DateTime actionQueuedAt { get; set; }
}

public class BaseWebSocketArgs : BaseTriggerArgs
{
    public int wsIdx { get; set; }
    public string wsId { get; set; }
    public string wsName { get; set; }
    public string wsUrl { get; set; }
    public string wsScheme { get; set; }
    public string wsHost { get; set; }
    public int wsPort { get; set; }
    public string wsPath { get; set; }
    public string wsQuery { get; set; }
}

public class WebSocketMessageArgs : BaseWebSocketArgs
{
    public string message { get; set; }
}

public class WebSocketConnectArgs : BaseWebSocketArgs
{
}
