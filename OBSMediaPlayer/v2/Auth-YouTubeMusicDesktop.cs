using System;
using System.Text;
using System.Net.Http;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
///   Get Companion Auth Token from YouTube Music Desktop App
/// </summary>
/// <triggers>
/// </triggers>
/// <variables>
///   <variable name="ytmDesktopCompanionUrl" type="string" scope="global" persisted="true" />
///   <variable name="ytmDesktopAuthToken" type="string" scope="global" persisted="true" />
/// </variables>
/// <references>
///   <reference source="System.dll" />
///   <reference source="System.Net.Http.dll" />
/// </references>
/// <settings name="ytmDesktopCompanionAuth" 
///           description="Get Companion Auth Token from YouTube Music Desktop App" 
///           keepInstanceActive="false"
///           precompileOnApplicationStart="false" 
///           delayedStart="false" 
///           saveResultToVariable="false"
///           variableName="" />
public class CPHInline
{
	public bool Execute()
	{
		var client = new HttpClient();
		var url = CPH.GetGlobalVar<string>("ytmDesktopCompanionUrl", true);
		client.BaseAddress = new Uri(url);
		var codeRequestJson = JsonConvert.SerializeObject(new YTMDesktopAuthCodeRequest());
		var codeRequest = new StringContent(codeRequestJson, Encoding.UTF8, "application/json");
		
		var codeResponse = client.PostAsync("/api/v1/auth/requestcode", codeRequest).Result;
		CPH.LogInfo(codeResponse.Content.ReadAsStringAsync().Result);
		codeResponse.EnsureSuccessStatusCode();
		var codeResponseJson = codeResponse.Content.ReadAsStringAsync().Result;
		var ytmDesktopAuthCodeResponse = JsonConvert.DeserializeObject<YTMDesktopAuthCodeResponse>(codeResponseJson);
		
		var ytmDesktopAuthTokenRequest = new YTMDesktopAuthTokenRequest(ytmDesktopAuthCodeResponse.code);
		var tokenRequestJson = JsonConvert.SerializeObject(ytmDesktopAuthTokenRequest);
		var tokenRequest = new StringContent(tokenRequestJson, Encoding.UTF8, "application/json");
		
		var tokenResponse = client.PostAsync("/api/v1/auth/request", tokenRequest).Result;
		tokenResponse.EnsureSuccessStatusCode();
		var tokenResponseJson = tokenResponse.Content.ReadAsStringAsync().Result;
		var ytmDesktopAuthTokenResponse = JsonConvert.DeserializeObject<YTMDesktopAuthTokenResponse>(tokenResponseJson);
		
		CPH.SetGlobalVar("ytmDesktopAuthToken", ytmDesktopAuthTokenResponse.token, true);
		
		// YTMDesktopAuthTokenResponse
		return true;
	}
}

public class YTMDesktopAuthCodeRequest 
{
	public string appId {get;set;} = "streamerbot";
	public string appName {get;set;} = "Streamer.bot WS Client";
	public string appVersion {get;set;} = "1.0.0";
}

public class YTMDesktopAuthCodeResponse
{
	public string code {get;set;}
}

public class YTMDesktopAuthTokenRequest
{
	public string appId {get;set;} ="streamerbot";
	public string code {get;set;}
	
	public YTMDesktopAuthTokenRequest(string code) {
		this.code = code;
		}
}

public class YTMDesktopAuthTokenResponse
{
	public string token {get;set;}
}
