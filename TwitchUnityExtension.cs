using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Net;
using TwitchClassDefinitions;
using System.IO;
using UnityEngine.Rendering;
using System;
using Unity.VisualScripting;
using UnityEditor;

public class Twitch_connection : MonoBehaviour
{
    // Start is called before the first frame update

    private string wsSessionID;
    private string clientID;
    private string clientSecret;
    private string authToken;
    private string refreshToken;

    [SerializeField] string targetChannel;

    TwitchWebsocketInterface wsInterface; //main class used to read events

    [SerializeField] private Boolean DebugWebSocket;

    void Start()
    {
        wsInterface = new TwitchWebsocketInterface();
        if (DebugWebSocket)
        { 
            wsInterface.readyToConnect = true;
            wsInterface.SetupWebsocketConnection(true);
            return; 
        } //Skip authentication part

        if (File.Exists(Application.persistentDataPath+"extension-token"))
        {
            refreshToken = File.ReadAllText(Application.persistentDataPath + "extension-token");
            Debug.Log("Refresh token received");
            
            clientID = EditorPrefs.GetString("TUE_AppID", null);
            clientSecret = EditorPrefs.GetString("TUE_AppSecret", null);

            StartCoroutine(RefreshAuthenticationToken());
            wsInterface.readyToConnect = true;
            wsInterface.SetupWebsocketConnection();
            return;
        }

        Debug.Log("Extension is not authenticated. Go to setting to reauthenticate");
    }

    void OnDestroy()
    {
        wsInterface.DisconnectFromWebSocket();
    }

    private void Update()
    {
        wsInterface.Update(); //just overwrite for dispatch message
    }

    //this is kinda finicy. Sometimes if old authentication site is left open this will trigger and cause invalid token to be created.
    //Please make sure there arent such windows open.
    IEnumerator RefreshAuthenticationToken()
    {
        while (!wsInterface.readyToConnect)
        {
            yield return new WaitForSeconds(.5f);
        }
        var request = new UnityWebRequest($"https://id.twitch.tv/oauth2/token?client_id={clientID}&refresh_token={refreshToken}&grant_type=refresh_token&client_secret={clientSecret}", "POST");
        request.downloadHandler = new DownloadHandlerBuffer();
        yield return request.SendWebRequest();
        refreshtokenresponse response = JsonUtility.FromJson<refreshtokenresponse>(request.downloadHandler.text);
        if (response.access_token != null)
        {
            authToken = response.access_token;
            refreshToken = response.refresh_token;
            SaveRefreshToken();
            Debug.Log($"New Refresh token is: {refreshToken}");
            Debug.Log("authToken received");
            StartCoroutine(SubscribeToInitialEvent());
            string[] eventList = EditorPrefs.GetString("TUE_Events", null).Split('+'); //Events from the setting page
            if (eventList[0] == "") { Debug.Log("No events defined in config"); yield break; }
            foreach (string event_name in eventList)
            {
                StartCoroutine(SubscribeToEvent(event_name.Split(",")[0], event_name.Split(",")[1]));
            }
        }
        else
        {
            Debug.Log("Failed to receive authToken");
        }
        yield return null;
    }

    IEnumerator SubscribeToInitialEvent()
    {
        RequestObject requestChat = new RequestObject();
        requestChat.condition = new ConditionObject();
        requestChat.transport = new TransportObject();
        requestChat.type = "channel.chat.message";
        requestChat.version = "1";
        requestChat.condition.broadcaster_user_id = targetChannel;
        requestChat.condition.user_id = targetChannel;
        requestChat.transport.method = "websocket";
        requestChat.transport.session_id = wsInterface.wsSession;

        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(requestChat));
        var request = new UnityWebRequest("https://api.twitch.tv/helix/eventsub/subscriptions", "POST");
        request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Client-ID", clientID);
        request.SetRequestHeader("Authorization", "Bearer " + authToken);
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();
        if (request.error != null) { Debug.Log("Subscription error response: " + request.error.ToString()); }
        else { Debug.Log($"Subscription to event: channel.chat.message was successful"); }
    }
    IEnumerator SubscribeToEvent(string event_name, string version)
    {
        RequestObject requestChat = new RequestObject();
        requestChat.condition = new ConditionObject();
        requestChat.transport = new TransportObject();
        requestChat.type = event_name;
        requestChat.version = version;
        requestChat.condition.broadcaster_user_id = targetChannel;
        requestChat.transport.method = "websocket";
        requestChat.transport.session_id = wsInterface.wsSession;

        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(requestChat));
        var request = new UnityWebRequest("https://api.twitch.tv/helix/eventsub/subscriptions", "POST");
        request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Client-ID", clientID);
        request.SetRequestHeader("Authorization", "Bearer " + authToken);
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();
        if (request.error != null) { Debug.Log("Subscription error response: " + request.error.ToString()); }
        else { Debug.Log($"Subscription to event: {event_name} was successful"); }
    }
    void SaveRefreshToken()
    {
        //TODO
        File.WriteAllText(Application.persistentDataPath+"extension-token", refreshToken);
        Debug.Log("Refresh token written to secure txt file");
    }
}

