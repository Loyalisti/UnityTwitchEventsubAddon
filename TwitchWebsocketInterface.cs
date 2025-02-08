using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NativeWebSocket;
using System;
using TwitchClassDefinitions;
using System.Linq;

public class TwitchWebsocketInterface
{
    public WebSocket ws; //our lovely websocket that will have connection to eventsub for messages; do not send messages to here or twitch will get maj and close the connection.
    public Boolean readyToConnect;
    public string wsSession;
    public string[] messageQueue = { };

    public void SetupWebsocketConnection()
    {
        //register callbacks and attempt connection
        ws = new WebSocket("wss://eventsub.wss.twitch.tv/ws");
        ws.OnMessage += OnMessage;
        ws.OnOpen += OnOpen;
        ws.OnError += OnError;
        ws.OnClose += OnClose;
        
        ws.Connect();
    }

    public void Update()
    {
        if(ws != null)
        {
            ws.DispatchMessageQueue(); //required to process messages in que for OnMessage callback;
        }
    }

    void OnMessage(byte[] data)
    {
        string message = System.Text.Encoding.UTF8.GetString(data);

        //Reading for message with this message type so we can yoink the transportID used to register into eventsub events
        if (message.Contains("session_welcome"))
        {
            WelcomeMessageObject test = JsonUtility.FromJson<WelcomeMessageObject>(message.Replace('/',' '));
            wsSession = test.payload.session.id;
            Debug.Log("Welcome received, session id fetched.");
        }
        else if (message.Contains("channel.chat.message"))
        {
            ChannelChatMessageObject test = JsonUtility.FromJson<ChannelChatMessageObject>(message);
            Debug.Log("Received from api: " + test.payload.@event.message.text);
            //messageQueue.Append(test.payload.subscription.type);
        }
        else if (message.Contains("channel.channel_points_custom_reward_redemption.add"))
        {
            ChannelPointCustomRewardRedemptionObject test = JsonUtility.FromJson<ChannelPointCustomRewardRedemptionObject>(message);
            Debug.Log("Received from api: " + test.payload.@event.reward.title);
            if(messageQueue == null) { messageQueue.Initialize(); }
            messageQueue.Append(test.payload.subscription.type);
        }

        else { Debug.Log("Received message from WebSocket: " + message); }
        //more here for the events
    }

    public void DisconnectFromWebSocket()
    {
        if (ws != null && ws.State == WebSocketState.Open)
        {
            ws.Close();
        }
    }
    void OnOpen()
    {
        Debug.Log("WebSocket connected.");
    }

    void OnError(string e)
    {
        Debug.LogError("WebSocket error: " + e);
    }

    void OnClose(WebSocketCloseCode e)
    {
        Debug.Log("WebSocket closed with code: " + e);
    }
}
