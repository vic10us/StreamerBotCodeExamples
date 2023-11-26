using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

/// <summary>
///   Update OBS Media Player
/// </summary>
/// <triggers>
///   <trigger source="Websocket Client" 
///            type="Websocket Client Message"
///            criteria="ytmWS" 
///            enabled="Yes" 
///   />
/// </triggers>
/// <references>
///   <reference source="netstandard.dll" />
///   <reference source="System.Linq.dll" />
///   <reference source="System.Linq.Queryable.dll" />
///   <reference source="System.ComponentModel.dll" />
///   <reference source="System.ComponentModel.EventBasedAsync.dll" />
///   <reference source="System.dll" />
///   <reference source="netstandard.dll" />
/// </references>
/// <settings name="YouTubeMusicDesktopV1" 
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
    public const string NAME = "YouTubeMusicDesktopV1";

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

	public bool Execute()
	{
		var message = args["message"].ToString();
		var x = ProcessMessage(message);
		if (x == null) return true;
		var json = JsonConvert.SerializeObject(x);

		var lastMediaEventJson = CPH.GetGlobalVar<string>("lastMediaEvent", false);
		MediaEvent lastMediaEvent = new MediaEvent();
		if (!string.IsNullOrWhiteSpace(lastMediaEventJson)) {
			lastMediaEvent = JsonConvert.DeserializeObject<MediaEvent>(lastMediaEventJson);
		}
		var updated = false;
		if (lastMediaEvent.track.cover != x.track.cover) {
			CPH.ObsSetBrowserSource(MediaPlayerSceneName, MediaPictureSourceName, x.track.cover);
			updated = true;
		}
		if (lastMediaEvent.player.seekbarCurrentPositionHuman != x.player.seekbarCurrentPositionHuman) {
			CPH.ObsSetGdiText(MediaPlayerSceneName, MediaPositionSourceName, $"{x.player.seekbarCurrentPositionHuman} / {x.track.durationHuman}");
			updated = true;
		}
		if (lastMediaEvent.track.title != x.track.title) {
			CPH.ObsSetGdiText(MediaPlayerSceneName, MediaTitleSourceName, x.track.title);
			updated = true;
		}
		if (lastMediaEvent.track.album != x.track.album) {
			CPH.ObsSetGdiText(MediaPlayerSceneName, MediaAlbumSourceName, $"{x.track.author} • {x.track.album}");
			updated = true;
		}
		if (updated) CPH.SetGlobalVar("lastMediaEvent", json, false);
		if (updated) setProgress(x.player.statePercent);
		return true;
	}

    /// <summary>
    /// Processes the message received from the Youtube Music Desktop App webook
    /// </summary>
    /// <param name="message">
    /// The message received from the Youtube Music Desktop App webhook
    /// </param>
    /// <returns>
    /// The <see cref="MediaEvent"/> object representing the message
    /// </returns>
	public MediaEvent? ProcessMessage(string message) 
	{
        // If the message does not start with "42[" then it is not a valid event message
		if (!message.StartsWith("42[")) return null;
        // The message is a comma separated list of two items
		var start = message.IndexOf(",")+1;
		var end = message.Length-start-1;
		var eventJson = message.Substring(start, end);
		var @event = JsonConvert.DeserializeObject<MediaEvent>(eventJson);
		return @event;
	}
	
    /// <summary>
    /// Sets the progress of the progress bar
    /// </summary>
    /// <param name="pos">
    /// The position of the progress bar between 0 and 1
    /// </param>
	public void setProgress(double pos) 
	{
		var sceneListRequest = new GetSceneItemListRequestData() {
			sceneName = MediaProgressBarSceneName
		};
		var sceneListRequestJson = JsonConvert.SerializeObject(sceneListRequest);
		var sceneListJson = CPH.ObsSendRaw("GetSceneItemList", sceneListRequestJson);
		var sceneList = JsonConvert.DeserializeObject<SceneItemList>(sceneListJson);
		var progressBarItem = sceneList.sceneItems.FirstOrDefault(c => c.sourceName.Equals(MediaProgressBarSourceName));
		var barWidth = progressBarItem.sceneItemTransform.sourceWidth;
		int progressWidth = (int)Math.Ceiling(barWidth * pos);
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
}

/// <summary>
/// Models/Entities for Youtube Music Desktop App
/// Do not modify unless you know what you are doing
/// </summary>

public class MediaEvent
{
	public Player player { get; set; } = new Player();
	public Track track { get; set; } = new Track();
}

public class Player
{
	public bool hasSong { get; set; }
	public bool isPaused { get; set; }
	public int volumePercent { get; set; }
	public int seekbarCurrentPosition { get; set; }
	public string seekbarCurrentPositionHuman { get; set; }
	public float statePercent { get; set; }
	public string likeStatus { get; set; }
	public object repeatType { get; set; }
}

public class Track
{
	public string author { get; set; }
	public string title { get; set; }
	public string album { get; set; }
	public string cover { get; set; }
	public int duration { get; set; }
	public string durationHuman { get; set; }
	public string url { get; set; }
	public string id { get; set; }
	public bool isVideo { get; set; }
	public bool isAdvertisement { get; set; }
	public bool inLibrary { get; set; }
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