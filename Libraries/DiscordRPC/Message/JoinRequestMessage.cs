using Qubit.Libraries.DiscordRPC.Entities;
using Valve.Newtonsoft.Json;

namespace Qubit.Libraries.DiscordRPC.Message
{
    


    public class JoinRequestMessage : IMessage
    {
        


        public override MessageType Type { get { return MessageType.JoinRequest; } }

        


        [JsonProperty("user")]
        public User User { get; internal set; }
    }
}
