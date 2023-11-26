using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

/// <summary>
///   Keep the websocket connection alive by responding to ping messages with pong
/// </summary>
/// <triggers>
///   <trigger source="Websocket Client" type="Websocket Client Message" criteria="websocketClientName" enabled="Yes" />
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
/// </references>
/// <settings name="SocketIOKeppAlive" 
///           description="Keep the websocket connection alive by responding to ping messages with pong" 
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
    public const string NAME = "SocketIOKeppAlive";

    /// <summary>
    /// Whether or not to log debug messages
    /// </summary>
    public const bool DEBUG = false;

	public bool Execute()
	{
        switch (args["triggerName"].ToString())
        {
            case "Websocket Client Message":
                if (DEBUG) CPH.LogInfo("Got Websocket Client Message");
                return OnWebsocketClientMessage();
            default:
                return true;
        }
    }

    public bool OnWebsocketClientMessage() {
        var messageArgs = GetArgumentObject<WebSocketMessageArgs>();
        var actionType = ExtractNumber(messageArgs.message);
        if (DEBUG) CPH.LogInfo($"Got actionType: {actionType}");
        switch (actionType) {
            case 2:
                // ping
                return ProcessPing(messageArgs);
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
