using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Net;
using TwitchClassDefinitions;

public class Twitch_connection : MonoBehaviour
{
    // Start is called before the first frame update

    public class AuthKeys
    {
        public string authCode; //Key we receive from website popup
        public string authToken; //key we get from twitch for the session by using authCode
        public string wsSessionID;
    }

    [SerializeField] public string clientID;//application ID from dev.twitch
    [SerializeField] public string clientSecret;//application Secret from dev.twitch
    [SerializeField] string targetChannel;
    private HttpListener listener; //redirect listener for authcode
    TwitchWebsocketInterface wsInterface; //main class used to read events
    private AuthKeys keys = new AuthKeys();
    private string redirect_url = "http://localhost:2750/twitch-api/"; //This needs to be same as in dev.twitch application settings
    [SerializeField] private string[] scope;

    void Start()
    {
        wsInterface = new TwitchWebsocketInterface();
        //Open browser with correct authorization url + parameters
        Application.OpenURL($"https://id.twitch.tv/oauth2/authorize?response_type=code&client_id={clientID}&redirect_uri={redirect_url}&scope={string.Join('+',scope)}&force_verify=true");
        startRedirectListener(); //used to listen for redirect into the redirect url;
        StartCoroutine(AuthorizeApplicationCode());
    }

    void OnDestroy()
    {
        wsInterface.DisconnectFromWebSocket();
    }

    private void Update()
    {
        wsInterface.Update(); //just overwrite for dispatch message
    }

    void startRedirectListener()
    {
        listener = new HttpListener();
        listener.Prefixes.Add(redirect_url);
        listener.Start();
        listener.BeginGetContext(redirectListenerCallback, null);
    }

    //this is kinda finicy. Sometimes if old authentication site is left open this will trigger and cause invalid token to be created.
    //Please make sure there arent such windows open.
    void redirectListenerCallback(System.IAsyncResult result)
    {
        HttpListenerContext context = listener.EndGetContext(result);
        HttpListenerRequest request = context.Request;
        string url = request.Url.ToString();

        if (url.Contains("code"))
        { //Redirect found
            Debug.Log("authCode received");
            keys.authCode = url.Split('=')[1].Split('&')[0]; //this contains the authorization code that is used to get auth token later.
            wsInterface.readyToConnect = true;
            wsInterface.SetupWebsocketConnection();
            listener.Stop();
        }
    }
    IEnumerator AuthorizeApplicationCode()
    {
        while (!wsInterface.readyToConnect)
        {
            yield return new WaitForSeconds(.5f);
        }
        var request = new UnityWebRequest($"https://id.twitch.tv/oauth2/token?client_id={clientID}&redirect_uri={redirect_url}&code={keys.authCode}&grant_type=authorization_code&client_secret={clientSecret}", "POST");
        request.downloadHandler = new DownloadHandlerBuffer();
        yield return request.SendWebRequest();
        authcoderesponse response = JsonUtility.FromJson<authcoderesponse>(request.downloadHandler.text);
        if (response.access_token != null)
        {
            keys.authToken = response.access_token;
            Debug.Log("authToken received");
            StartCoroutine(SubscribeToInitialEvent());
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
        request.SetRequestHeader("Authorization", "Bearer " + keys.authToken);
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();
        Debug.Log("Subscription response: ");
    }
}
