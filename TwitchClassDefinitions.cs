using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

//public class TwitchClassDefinitions : MonoBehaviour
//{

//}

namespace TwitchClassDefinitions
{
    [System.Serializable]
    public struct authcoderesponse
    {
        public string access_token;
        public string expires_in;
        public string refresh_token;
        public string[] scope;
        public string token_type;
    }

    [System.Serializable]
    public struct refreshtokenresponse 
    {
        public string access_token;
        public string refresh_token;
        public string[] scope;
        public string token_type;
    }

    [System.Serializable]
    public struct RequestObject
    {
        public string type;
        public string version;
        public ConditionObject condition;
        public TransportObject transport;
    }

    [System.Serializable]
    public class MetadataObject 
    {
        public string message_id;
        public string message_type;
        public string message_timestamp;
        public string subscription_type;
        public string subscription_version;
    }

    [System.Serializable]
    public class SessionObject
    {
        public string id;
        public string status;
        public string connected_at;
        public string keepalive_timeout_seconds;
        public string reconnect_url;
    }

    [System.Serializable]
    public class PayloadObject 
    {
        public SessionObject session;
        public SubscriptionObject subscription;
    }

    [System.Serializable]
    public class ConditionObject
    {
        public string broadcaster_user_id;
        public string user_id;
    }

    [System.Serializable]
    public struct TransportObject
    {
        public string method;
        public string session_id;
    }
    //Main object returned on for all events, describing which event was triggered
    [System.Serializable]
    public class SubscriptionObject
    {
        public string id;
        public string type;
        public string version;
        public string status;
        public int cost;
        public ConditionObject condition;
        public string created_at;
    }

    [System.Serializable]
    public struct FragmentObject
    {
        public string type;
        public string text;
        public CheermoteObject cheermote;
        public EmoteObject emote;
        public MentionObject mention;
    }

    [System.Serializable]
    public struct CheermoteObject 
    {
        public string prefix;
        public int bits;
        public int tier;
    }

    [System.Serializable]
    public struct EmoteObject
    {
        public string id;
        public string emote_set_id;
        public string owner_id;
        public string format;
    }

    [System.Serializable]
    public struct MentionObject
    {
        public string user_id;
        public string user_name;
        public string user_login;
    }

    [System.Serializable]
    public struct BadgesObject
    {
        public string set_id;
        public string id;
        public string info;
    }

    [System.Serializable]
    public struct CheerObject
    {
        public int bits;
    }

    [System.Serializable]
    public struct ReplyObject
    {
        public string parent_message_id;
        public string parent_message_body;
        public string parent_user_id;
        public string parent_user_name;
        public string parent_user_login;
        public string thread_message_id;
        public string thread_user_id;
        public string thread_user_name;
        public string thread_user_login;
    }

    [System.Serializable]
    public struct MessageObject
    {
        public string text;
        public FragmentObject[] fragments;
    }

    [System.Serializable]
    public struct RewardObject
    {
        public string id;
        public string title;
        public int cost;
        public string prompt;
    }

    /*EVENT BASED OBJECTS*/
    [System.Serializable]
    public class WelcomeMessageObject
    {
        public MetadataObject metadata;
        public PayloadObject payload;
    }

    [System.Serializable]
    public class ChannelChatMessageObject
    {
        public MetadataObject metadata;
        public PayloadObject payload;
        [System.Serializable]
        public class PayloadObject 
        {
            public SubscriptionObject subscription;
            [System.Serializable]
            public class EventInfoObject 
            {
                public string broadcaster_user_id;
                public string broadcaster_user_name;
                public string broadcaster_user_login;
                public string chatter_user_id;
                public string chatter_user_name;
                public string chatter_user_login;
                public string message_id;
                public MessageObject message;
                public string color;
                public BadgesObject[] badges;
                public string message_type;
                public CheerObject cheer;
                public ReplyObject reply;
                public string channel_points_custom_reward_id;
            }
            public EventInfoObject @event;
        }
    }

    [System.Serializable]
    public class ChannelPointCustomRewardRedemptionObject
    {
        public MetadataObject metadata;
        public PayloadObject payload;
        [System.Serializable]
        public class PayloadObject 
        {
            public SubscriptionObject subscription;
            [System.Serializable]
            public class EventInfoObject 
            {
                public string id;
                public string broadcaster_user_id;
                public string broadcaster_user_name;
                public string broadcaster_user_login;
                public string user_id;
                public string user_login;
                public string user_name;
                public string user_input;
                public string status;
                public RewardObject reward;
                public string redeemed_at;
            }
            public EventInfoObject @event;
        }
    }

    /*EVENT NESTED STRUCTURES*/
}
