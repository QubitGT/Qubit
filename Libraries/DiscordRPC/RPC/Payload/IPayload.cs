using Qubit.Libraries.DiscordRPC.Converters;
using Valve.Newtonsoft.Json;

namespace Qubit.Libraries.DiscordRPC.RPC.Payload
{



    internal abstract class IPayload
    {



        [JsonProperty("cmd"), JsonConverter(typeof(EnumSnakeCaseConverter))]
        public Command Command { get; set; }




        [JsonProperty("nonce")]
        public string Nonce { get; set; }

        protected IPayload() { }
        protected IPayload(long nonce)
        {
            Nonce = nonce.ToString();
        }

        public override string ToString()
        {
            return $"Payload || Command: {Command}, Nonce: {Nonce}";
        }
    }
}

