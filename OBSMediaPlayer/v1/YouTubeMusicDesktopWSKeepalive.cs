using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

/// <summary>
///   Keep the websocket connection alive
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
/// <settings name="WebsocketKeepAlive" 
///           description="Updates the OBS Media Player when updates are received by the YouTube Muisic Desktop App Socket.io webservice" 
///           keepInstanceActive="true"
///           precompileOnApplicationStart="true" 
///           delayedStart="false" 
///           saveResultToVariable="false"
///           variableName="" />
public class CPHInline
{
    public const string NAME = "WebsocketKeepAlive";
    public const LogLevel logLevel = LogLevel.Error;
    public CPHInline()
    {
        // Set the keepalive interval
        ThrottleHelper.ThrottleIntervalMilliseconds = 4000;
        ThrottleHelper.logger = Log;
    }

    public bool Execute()
    {
        ThrottleHelper.ExecuteWithThrottle(KeepAlive);
        return true;
    }

    public void KeepAlive()
    {
        var argsJson = JsonConvert.SerializeObject(args);
        var webSocketArgs = JsonConvert.DeserializeObject<WebSocketArgs>(argsJson);
        Log("-------------------Executing Keepalive-------------------", LogLevel.Debug);
        var ws = CPH.WebsocketCustomServerGetConnectionByName("ytm");
        CPH.WebsocketSend("2", webSocketArgs.wsIdx);
    }

    public void Log(string message, LogLevel level = LogLevel.Debug)
    {
        if (level >= logLevel)
        {
            switch (level)
            {
                case LogLevel.Trace:
                    CPH.LogVerbose(message);
                    break;
                case LogLevel.Debug:
                    CPH.LogDebug(message);
                    break;
                case LogLevel.Information:
                    CPH.LogInfo(message);
                    break;
                case LogLevel.Warning:
                    CPH.LogWarn(message);
                    break;
                case LogLevel.Error:
                    CPH.LogError(message);
                    break;
                case LogLevel.Critical:
                    CPH.LogError(message);
                    break;
                case LogLevel.None:
                    break;
            }
        }
    }
}

public class WebSocketArgs
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
    public string message { get; set; }
}

public static class ThrottleHelper
{
    private static bool canExecute = true;
    private static DateTimeOffset lastExecutionTime = DateTime.MinValue;
    private static readonly object lockObject = new object ();
    public static int ThrottleIntervalMilliseconds = 4000;
    public static Action<string, LogLevel> logger = null;
    public static void ExecuteWithThrottle(Action action)
    {
        lock (lockObject)
        {
            if (canExecute)
            {
                try
                {
                    action.Invoke();
                    lastExecutionTime = DateTimeOffset.Now;
                    canExecute = false;
                    ResetCanExecuteFlag();
                }
                catch (Exception ex)
                {
                	logger?.Invoke($"Unexpected exception in WebsocketKeepAlive {ex.Message}", LogLevel.Error);
                }
            }
            else
            {
                logger?.Invoke("Throttled", LogLevel.Warning);
            }
        }
    }

    private static async void ResetCanExecuteFlag()
    {
        await Task.Delay(ThrottleIntervalMilliseconds);
        canExecute = true;
    }
}

public enum LogLevel
{
    Trace,
    Debug,
    Information,
    Warning,
    Error,
    Critical,
    None
}