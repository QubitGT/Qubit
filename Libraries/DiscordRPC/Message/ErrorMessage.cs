using Valve.Newtonsoft.Json;

namespace Qubit.Libraries.DiscordRPC.Message
{
    public class ErrorMessage : IMessage
    {
        public override MessageType Type { get { return MessageType.Error; } }

        [JsonProperty("code")]
        public ErrorCode Code { get; internal set; }

        [JsonProperty("message")]
        public string Message { get; internal set; }
    }

    public enum ErrorCode
    {
        Success = 0,

        PipeException = 1,

        ReadCorrupt = 2,

        NotImplemented = 10,

        UnknownError = 1000,

        InvalidPayload = 4000,

        InvalidCommand = 4002,

        InvalidEvent = 4004,

        /*
		InvalidGuild = 4003,
		InvalidChannel = 4005,
		InvalidPermissions = 4006,
		InvalidClientID = 4007,
		InvalidOrigin = 4008,
		InvalidToken = 4009,
		InvalidUser = 4010,
		OAuth2Error = 5000,
		SelectChannelTimeout = 5001,
		GetGuildTimeout = 5002,
		SelectVoiceForceRequired = 5003,
		CaptureShortcutAlreadyListening = 5004
		*/
    }
}
