using Qubit.Libraries.DiscordRPC.Converters;
using Valve.Newtonsoft.Json;
using Valve.Newtonsoft.Json.Linq;

namespace Qubit.Libraries.DiscordRPC.RPC.Payload
{
    


    internal class EventPayload : IPayload
    {
        


        [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Data { get; set; }

        


        [JsonProperty("evt"), JsonConverter(typeof(EnumSnakeCaseConverter))]
        public ServerEvent? Event { get; set; }

        


		public EventPayload() : base() { Data = null; }

        



		public EventPayload(long nonce) : base(nonce) { Data = null; }

        




        public T GetObject<T>()
        {
            return Data == null ? default(T) : Data.ToObject<T>();
        }

        



		public override string ToString()
        {
            return "Event " + base.ToString() + ", Event: " + (Event.HasValue ? Event.ToString() : "N/A");
        }
    }


}
