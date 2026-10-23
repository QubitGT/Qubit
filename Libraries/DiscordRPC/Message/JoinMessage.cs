using Valve.Newtonsoft.Json;

namespace Qubit.Libraries.DiscordRPC.Message
{
    public class JoinMessage : IMessage
    {
        public override MessageType Type { get { return MessageType.Join; } }

        [JsonProperty("secret")]
        public string Secret { get; internal set; }
    }
}
