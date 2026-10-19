using Qubit.Libraries.DiscordRPC.Converters;

namespace Qubit.Libraries.DiscordRPC.RPC.Payload
{



    internal enum ServerEvent
    {




        [EnumValue("READY")]
        Ready,




        [EnumValue("ERROR")]
        Error,




        [EnumValue("ACTIVITY_JOIN")]
        ActivityJoin,




        [EnumValue("ACTIVITY_SPECTATE")]
        ActivitySpectate,




        [EnumValue("ACTIVITY_JOIN_REQUEST")]
        ActivityJoinRequest,

#if INCLUDE_FULL_RPC
        
        [Obsolete("This value is appart of the RPC API and is not supported by this library.", true)]
        [EnumValue("GUILD_STATUS")]
        GuildStatus,
		[Obsolete("This value is appart of the RPC API and is not supported by this library.", true)]
        [EnumValue("GUILD_CREATE")]
        GuildCreate,
		[Obsolete("This value is appart of the RPC API and is not supported by this library.", true)]
        [EnumValue("CHANNEL_CREATE")]
        ChannelCreate,
		[Obsolete("This value is appart of the RPC API and is not supported by this library.", true)]
        [EnumValue("VOICE_CHANNEL_SELECT")]
        VoiceChannelSelect,
		[Obsolete("This value is appart of the RPC API and is not supported by this library.", true)]
        [EnumValue("VOICE_STATE_CREATED")]
        VoiceStateCreated,
		[Obsolete("This value is appart of the RPC API and is not supported by this library.", true)]
        [EnumValue("VOICE_STATE_UPDATED")]
        VoiceStateUpdated,
		[Obsolete("This value is appart of the RPC API and is not supported by this library.", true)]
        [EnumValue("VOICE_STATE_DELETE")]
        VoiceStateDelete,
		[Obsolete("This value is appart of the RPC API and is not supported by this library.", true)]
        [EnumValue("VOICE_SETTINGS_UPDATE")]
        VoiceSettingsUpdate,
		[Obsolete("This value is appart of the RPC API and is not supported by this library.", true)]
        [EnumValue("VOICE_CONNECTION_STATUS")]
        VoiceConnectionStatus,
		[Obsolete("This value is appart of the RPC API and is not supported by this library.", true)]
        [EnumValue("SPEAKING_START")]
        SpeakingStart,
		[Obsolete("This value is appart of the RPC API and is not supported by this library.", true)]
        [EnumValue("SPEAKING_STOP")]
        SpeakingStop,
		[Obsolete("This value is appart of the RPC API and is not supported by this library.", true)]
        [EnumValue("MESSAGE_CREATE")]
        MessageCreate,
		[Obsolete("This value is appart of the RPC API and is not supported by this library.", true)]
        [EnumValue("MESSAGE_UPDATE")]
        MessageUpdate,
		[Obsolete("This value is appart of the RPC API and is not supported by this library.", true)]
        [EnumValue("MESSAGE_DELETE")]
        MessageDelete,
        [Obsolete("This value is appart of the RPC API and is not supported by this library.", true)]
        [EnumValue("NOTIFICATION_CREATE")]
        NotificationCreate,
        [Obsolete("This value is appart of the RPC API and is not supported by this library.", true)]
        [EnumValue("CAPTURE_SHORTCUT_CHANGE")]
        CaptureShortcutChange
#endif
    }
}
