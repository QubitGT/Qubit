using System;
using Valve.Newtonsoft.Json;

namespace Qubit.Libraries.DiscordRPC.Entities
{
    [Serializable]
    public class Timestamps
    {
        public static Timestamps Now { get { return new Timestamps(DateTime.UtcNow); } }

        public static Timestamps FromTimeSpan(double seconds) { return FromTimeSpan(TimeSpan.FromSeconds(seconds)); }

        public static Timestamps FromTimeSpan(TimeSpan timespan)
        {
            return new Timestamps()
            {
                Start = DateTime.UtcNow,
                End = DateTime.UtcNow + timespan
            };
        }

        [JsonIgnore]
        public DateTime? Start { get; set; }

        [JsonIgnore]
        public DateTime? End { get; set; }

        public Timestamps()
        {
            Start = null;
            End = null;
        }

        public Timestamps(DateTime start)
        {
            Start = start;
            End = null;
        }

        public Timestamps(DateTime start, DateTime end)
        {
            Start = start;
            End = end;
        }

        [JsonProperty("start", NullValueHandling = NullValueHandling.Ignore)]
        public ulong? StartUnixMilliseconds
        {
            get
            {
                return Start.HasValue ? ToUnixMilliseconds(Start.Value) : (ulong?)null;
            }

            set
            {
                Start = value.HasValue ? FromUnixMilliseconds(value.Value) : (DateTime?)null;
            }
        }

        [JsonProperty("end", NullValueHandling = NullValueHandling.Ignore)]
        public ulong? EndUnixMilliseconds
        {
            get
            {
                return End.HasValue ? ToUnixMilliseconds(End.Value) : (ulong?)null;
            }

            set
            {
                End = value.HasValue ? FromUnixMilliseconds(value.Value) : (DateTime?)null;
            }
        }

        public static DateTime FromUnixMilliseconds(ulong unixTime)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddMilliseconds(Convert.ToDouble(unixTime));
        }

        public static ulong ToUnixMilliseconds(DateTime date)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return Convert.ToUInt64((date - epoch).TotalMilliseconds);
        }
    }
}
