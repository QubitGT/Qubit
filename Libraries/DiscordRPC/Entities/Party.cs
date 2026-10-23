using Qubit.Libraries.DiscordRPC.Helper;
using System;
using Valve.Newtonsoft.Json;

namespace Qubit.Libraries.DiscordRPC.Entities
{
    [Serializable]
    public class Party
    {
        public enum PrivacySetting
        {
            Private = 0,

            Public = 1
        }

        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public string ID { get { return _partyid; } set { _partyid = value.GetNullOrString(); } }
        private string _partyid;

        [JsonIgnore]
        public int Size { get; set; }

        [JsonIgnore]
        public int Max { get; set; }

        [JsonProperty("privacy", NullValueHandling = NullValueHandling.Include, DefaultValueHandling = DefaultValueHandling.Include)]
        public PrivacySetting Privacy { get; set; }

        [JsonProperty("size", NullValueHandling = NullValueHandling.Ignore)]
        private int[] _size
        {
            get
            {
                int size = Math.Max(1, Size);
                return new[] { size, Math.Max(size, Max) };
            }

            set
            {
                if (value.Length != 2)
                {
                    Size = 0;
                    Max = 0;
                }
                else
                {
                    Size = value[0];
                    Max = value[1];
                }
            }
        }
    }
}
