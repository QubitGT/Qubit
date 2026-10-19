using Qubit.Libraries.DiscordRPC.RPC.Payload;
using Valve.Newtonsoft.Json;

namespace Qubit.Libraries.DiscordRPC.RPC.Commands
{
    internal class CloseCommand : ICommand
    {



        [JsonProperty("pid")]
        public int PID { get; set; }




        [JsonProperty("close_reason")]
        public string value = "Unity 5.5 doesn't handle thread aborts. Can you please close me discord?";

        public IPayload PreparePayload(long nonce)
        {
            return new ArgumentPayload()
            {
                Command = Command.Dispatch,
                Nonce = null,
                Arguments = null
            };
        }
    }
}
