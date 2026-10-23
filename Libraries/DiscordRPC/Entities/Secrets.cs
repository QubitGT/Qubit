using Qubit.Libraries.DiscordRPC.Exceptions;
using System;
using System.Text;
using Valve.Newtonsoft.Json;

namespace Qubit.Libraries.DiscordRPC.Entities
{
    [Serializable]
    public class Secrets
    {
        [Obsolete("This feature has been deprecated my Mason in issue #152 on the offical library. Was originally used as a Notify Me feature, it has been replaced with Join / Spectate.", true)]
        [JsonIgnore]
        public string MatchSecret;

        [JsonProperty("join", NullValueHandling = NullValueHandling.Ignore)]
        public string Join
        {
            get => _joinSecret;
            set
            {
                if (!BaseRichPresence.ValidateString(value, out _joinSecret, false, 128))
                    throw new StringOutOfRangeException(128);
            }
        }
        private string _joinSecret;

        [Obsolete("Property name is redundant and replaced with Join.")]
        [JsonIgnore]
        public string JoinSecret
        {
            get => Join;
            set => Join = value;
        }

        [Obsolete("Spectating is no longer supported by Discord.")]
        [JsonIgnore]
        public string SpectateSecret { get; set; }
        #region Statics

        public static Encoding Encoding => Encoding.UTF8;

        public const int SecretLength = 128;

        public static string CreateSecret(Random random, int length = SecretLength)
        {
            if (length < 1 || length > SecretLength)
                throw new ArgumentOutOfRangeException(nameof(length), "Secret length must be between 1 and 128 characters.");

            byte[] bytes = new byte[length];
            random.NextBytes(bytes);

            return Encoding.GetString(bytes);
        }

        public static string CreateFriendlySecret(Random random, int length = SecretLength)
        {
            if (length < 1 || length > SecretLength)
                throw new ArgumentOutOfRangeException(nameof(length), "Secret length must be between 1 and 128 characters.");

            string charset = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < length; i++)
                builder.Append(charset[random.Next(charset.Length)]);

            return builder.ToString();
        }
        #endregion
    }
}
