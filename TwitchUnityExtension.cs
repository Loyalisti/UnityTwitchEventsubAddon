using UnityEngine;
using UnityEngine.Networking;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using TwitchClassDefinitions;
using System.Net;
using System.Numerics;
using System.Threading.Tasks;
using System.IO;
using System;

public class TwitchUnityExtension : EditorWindow 
{
    private HttpListener listener; //redirect listener for authcode

    private string appID;
    private string appSecret;
    private string authCode;
    private string authToken;
    private string refreshToken;


    private SerializedObject ExtensionObject;
    private ReorderableList scopeReorderableList;
    private ReorderableList eventReorderableList;
    
    [SerializeField] 
    private List<string> scopeList = new List<string>(); // The actual list of strings
    [SerializeField] 
    private List<string> eventList = new List<string>(); // The actual list of strings


    private string redirect_url = "http://localhost:2750/twitch-api/"; //This needs to be same as in dev.twitch application settings

    [MenuItem("Window/Twitch Unity Extension")]
    public static void ShowWindow()
    {
        GetWindow<TwitchUnityExtension>("Settings");
    }

    private void OnEnable()
    {
        //Load default saved values
        appID = EditorPrefs.GetString("TUE_AppID", null);
        appSecret = EditorPrefs.GetString("TUE_AppSecret", null);
        scopeList = new List<string>(EditorPrefs.GetString("TUE_Scopes", null).Split('+'));
        eventList = new List<string>(EditorPrefs.GetString("TUE_Events", null).Split('+'));

        ExtensionObject = new SerializedObject(this);
        scopeReorderableList = new ReorderableList(ExtensionObject,ExtensionObject.FindProperty("scopeList") , true, false, true, true);
        eventReorderableList = new ReorderableList(ExtensionObject,ExtensionObject.FindProperty("eventList") , true, false, true, true);

        scopeReorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            if(scopeList.Count > 0)
            {
                scopeList[index] = EditorGUI.TextField(rect,scopeList[index]);
            }
        };
        scopeReorderableList.onAddCallback = (rect) =>
        {
            scopeList.Add("");
        };
        scopeReorderableList.onRemoveCallback = (ReorderableList list) =>
        {
            if(list.index >= 0)
            {
                scopeList.RemoveAt(list.index);
                ExtensionObject.ApplyModifiedProperties();
            }
        };

        eventReorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            if(eventList.Count > 0)
            {
                eventList[index] = EditorGUI.TextField(rect,eventList[index]);
            }
        };
        eventReorderableList.onAddCallback = (rect) =>
        {
            eventList.Add("");
        };
        eventReorderableList.onRemoveCallback = (ReorderableList list) =>
        {
            if(list.index >= 0)
            {
                eventList.RemoveAt(list.index);
                ExtensionObject.ApplyModifiedProperties();
            }
        };
    }

    private void OnGUI()
    {
        GUILayout.Label("Twitch Application Keys", EditorStyles.boldLabel);

        appID = EditorGUILayout.TextField("Application ID",appID);
        appSecret = EditorGUILayout.TextField("Application Secret",appSecret);

        GUILayout.Label("Scopes", EditorStyles.boldLabel);
        scopeReorderableList.DoLayoutList();


        GUILayout.Label("Events", EditorStyles.boldLabel);

        eventReorderableList.DoLayoutList();
        ExtensionObject.Update();
        ExtensionObject.ApplyModifiedProperties();

        if (GUILayout.Button("Authenticate"))
        {
            //Open browser with correct authorization url + parameters
            Application.OpenURL($"https://id.twitch.tv/oauth2/authorize?response_type=code&client_id={appID}&redirect_uri=http://localhost:2750/twitch-api/&scope={string.Join("+",scopeList)}&force_verify=true");
            startRedirectListener(); //used to listen for redirect into the redirect url;
            //Task.Run(startRedirectListener);
            AuthorizeApplicationCode();
        }

        if (GUILayout.Button("Save Settings"))
        {
            EditorPrefs.SetString("TUE_AppID", appID);
            EditorPrefs.SetString("TUE_AppSecret", appSecret);
            EditorPrefs.SetString("TUE_Scopes", string.Join("+", scopeList));
            EditorPrefs.SetString("TUE_Events", string.Join("+", eventList));
        }

        if (GUILayout.Button("Load Settings"))
        {
            appID = EditorPrefs.GetString("TUE_AppID", null);
            appSecret = EditorPrefs.GetString("TUE_AppSecret", null);
            scopeList = new List<string>(EditorPrefs.GetString("TUE_Scopes", null).Split('+'));
            eventList = new List<string>(EditorPrefs.GetString("TUE_Events", null).Split('+'));

            ExtensionObject.Update();
            ExtensionObject.ApplyModifiedProperties();
        }
    }

    private void startRedirectListener()
    {
        listener = new HttpListener();
        listener.Prefixes.Add(redirect_url);
        listener.Start();
        listener.BeginGetContext(redirectListenerCallback, null);
        while (listener.IsListening) { }
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
            authCode = url.Split('=')[1].Split('&')[0]; //this contains the authorization code that is used to get auth token later.
            listener.Stop();
        }
    } 
    private void AuthorizeApplicationCode()
    {
        Debug.Log("Entered Authorization");
        var request = new UnityWebRequest($"https://id.twitch.tv/oauth2/token?client_id={appID}&redirect_uri={redirect_url}&code={authCode}&grant_type=authorization_code&client_secret={appSecret}", "POST");
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SendWebRequest();
        while (!request.isDone)
        {

        }
        Debug.Log("Sent webrequest");

        authcoderesponse response = JsonUtility.FromJson<authcoderesponse>(request.downloadHandler.text);
        if (response.access_token != null)
        {
            authToken = response.access_token;
            refreshToken = response.refresh_token;
            Debug.Log($"Authentication Token {response.refresh_token}");
            SaveRefreshToken();
        }
        else
        {
            Debug.Log("Failed to received Authentication Token");
        }
    }
    /*
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
            refreshToken = response.refresh_token;
            SaveRefreshToken();
            Debug.Log("authToken received");
            StartCoroutine(SubscribeToInitialEvent());
            foreach (string event_name in eventsToSub)
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
    */
    void SaveRefreshToken()
    {
        //TODO
        File.WriteAllText(Application.persistentDataPath+"extension-token", refreshToken);
        Debug.Log("Refresh token written to secure txt file");
    }
}
