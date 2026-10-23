using Qubit.Libraries.DiscordRPC.Exceptions;
using Qubit.Libraries.DiscordRPC.Helper;
using System;
using System.Text;
using Valve.Newtonsoft.Json;

namespace Qubit.Libraries.DiscordRPC.Entities
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    [Serializable]
    public class BaseRichPresence
    {
        [JsonProperty("state", NullValueHandling = NullValueHandling.Ignore)]
        public string State
        {
            get { return _state; }
            set
            {
                if (!ValidateString(value, out _state, false, 128))
                    throw new StringOutOfRangeException("State", 0, 128);
            }
        }

        protected internal string _state;

        [JsonProperty("state_url", NullValueHandling = NullValueHandling.Ignore)]
        public string StateUrl
        {
            get { return _stateUrl; }
            set
            {
                if (!ValidateString(value, out _stateUrl, false, 256))
                    throw new StringOutOfRangeException(256);

                if (!ValidateUrl(_stateUrl))
                    throw new ArgumentException("Url must be a valid URI");
            }
        }

        protected internal string _stateUrl;

        [JsonProperty("details", NullValueHandling = NullValueHandling.Ignore)]
        public string Details
        {
            get { return _details; }
            set
            {
                if (!ValidateString(value, out _details, false, 128))
                    throw new StringOutOfRangeException(128);
            }
        }

        protected internal string _details;

        [JsonProperty("details_url", NullValueHandling = NullValueHandling.Ignore)]
        public string DetailsUrl
        {
            get { return _detailsUrl; }
            set
            {
                if (!ValidateString(value, out _detailsUrl, false, 256))
                    throw new StringOutOfRangeException(256);

                if (!ValidateUrl(_detailsUrl))
                    throw new ArgumentException("Url must be a valid URI");
            }
        }

        protected internal string _detailsUrl;

        [JsonProperty("timestamps", NullValueHandling = NullValueHandling.Ignore)]
        public Timestamps Timestamps { get; set; }

        [JsonProperty("assets", NullValueHandling = NullValueHandling.Ignore)]
        public Assets Assets { get; set; }

        [JsonProperty("party", NullValueHandling = NullValueHandling.Ignore)]
        public Party Party { get; set; }

        [JsonProperty("secrets", NullValueHandling = NullValueHandling.Ignore)]
        public Secrets Secrets { get; set; }

        [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
        public ActivityType Type { get; set; }

        [JsonProperty("status_display_type", NullValueHandling = NullValueHandling.Ignore)]
        public StatusDisplayType StatusDisplay { get; set; }

        #region Has Checks

        public bool HasTimestamps()
        {
            return Timestamps != null && (Timestamps.Start != null || Timestamps.End != null);
        }

        public bool HasAssets()
        {
            return Assets != null;
        }

        public bool HasParty()
        {
            return Party != null && Party.ID != null;
        }

        public bool HasSecrets()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            return Secrets != null && (Secrets.Join != null || Secrets.SpectateSecret != null);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        #endregion

        internal static bool ValidateString(string str, out string result, bool useBytes, int length, Encoding encoding = null)
        {
            result = str;
            if (str == null)
                return true;

            var s = str.Trim();

            if (useBytes && !s.WithinLength(length, encoding) || s.Length > length)
                return false;

            result = s.GetNullOrString();
            return true;
        }

        internal static bool ValidateUrl(string url)
        {
            return string.IsNullOrEmpty(url) ? true : Uri.TryCreate(url, UriKind.Absolute, out _);
        }

        public static implicit operator bool(BaseRichPresence presence)
        {
            return presence != null;
        }

        internal virtual bool Matches(RichPresence other)
        {
            if (other == null)
                return false;

            if (State != other.State ||
                StateUrl != other.StateUrl ||
                Details != other.Details ||
                DetailsUrl != other.DetailsUrl ||
                Type != other.Type)
                return false;

            if (Timestamps != null)
            {
                if (other.Timestamps == null ||
                    other.Timestamps.StartUnixMilliseconds != Timestamps.StartUnixMilliseconds ||
                    other.Timestamps.EndUnixMilliseconds != Timestamps.EndUnixMilliseconds)
                    return false;
            }
            else if (other.Timestamps != null)
            {
                return false;
            }

            if (Secrets != null)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                if (other.Secrets == null ||
                    other.Secrets.Join != Secrets.Join ||
                    other.Secrets.SpectateSecret != Secrets.SpectateSecret)
                    return false;
#pragma warning restore CS0618 // Type or member is obsolete
            }
            else if (other.Secrets != null)
            {
                return false;
            }

            if (Party != null)
            {
                if (other.Party == null ||
                    other.Party.ID != Party.ID ||
                    other.Party.Max != Party.Max ||
                    other.Party.Size != Party.Size ||
                    other.Party.Privacy != Party.Privacy)
                    return false;
            }
            else if (other.Party != null)
            {
                return false;
            }

            if (Assets != null)
            {
                if (other.Assets == null ||
                    other.Assets.LargeImageKey != Assets.LargeImageKey ||
                    other.Assets.LargeImageText != Assets.LargeImageText ||
                    other.Assets.LargeImageUrl != Assets.LargeImageUrl ||
                    other.Assets.SmallImageKey != Assets.SmallImageKey ||
                    other.Assets.SmallImageText != Assets.SmallImageText ||
                    other.Assets.SmallImageUrl != Assets.SmallImageUrl)
                    return false;
            }
            else if (other.Assets != null)
            {
                return false;
            }

            return true;
        }

        public RichPresence ToRichPresence()
        {
            var presence = new RichPresence
            {
                State = State,
                StateUrl = StateUrl,
                Details = Details,
                DetailsUrl = DetailsUrl,
                Type = Type,
                StatusDisplay = StatusDisplay,

                Party = !HasParty() ? Party : null,
                Secrets = !HasSecrets() ? Secrets : null
            };

            if (HasAssets())
            {
                presence.Assets = new Assets()
                {
                    SmallImageKey = Assets.SmallImageKey,
                    SmallImageText = Assets.SmallImageText,
                    SmallImageUrl = Assets.SmallImageUrl,

                    LargeImageKey = Assets.LargeImageKey,
                    LargeImageText = Assets.LargeImageText,
                    LargeImageUrl = Assets.LargeImageUrl
                };
            }

            if (HasTimestamps())
            {
                presence.Timestamps = new Timestamps();
                if (Timestamps.Start.HasValue) presence.Timestamps.Start = Timestamps.Start;
                if (Timestamps.End.HasValue) presence.Timestamps.End = Timestamps.End;
            }

            return presence;
        }
    }

    public sealed class RichPresence : BaseRichPresence
    {
        [JsonProperty("buttons", NullValueHandling = NullValueHandling.Ignore)]
        public Button[] Buttons { get; set; }

        public bool HasButtons()
        {
            return Buttons != null && Buttons.Length > 0;
        }

        #region Builder

        public RichPresence WithState(string state)
        {
            State = state;
            return this;
        }

        public RichPresence WithStateUrl(string stateUrl)
        {
            StateUrl = stateUrl;
            return this;
        }

        public RichPresence WithDetails(string details)
        {
            Details = details;
            return this;
        }

        public RichPresence WithDetailsUrl(string detailsUrl)
        {
            DetailsUrl = detailsUrl;
            return this;
        }

        public RichPresence WithType(ActivityType type)
        {
            Type = type;
            return this;
        }

        public RichPresence WithStatusDisplay(StatusDisplayType statusDisplay)
        {
            StatusDisplay = statusDisplay;
            return this;
        }

        public RichPresence WithTimestamps(Timestamps timestamps)
        {
            Timestamps = timestamps;
            return this;
        }

        public RichPresence WithAssets(Assets assets)
        {
            Assets = assets;
            return this;
        }

        public RichPresence WithParty(Party party)
        {
            Party = party;
            return this;
        }

        public RichPresence WithSecrets(Secrets secrets)
        {
            Secrets = secrets;
            return this;
        }

        public RichPresence WithButtons(Button topButton, Button bottomButton = null)
        {
            Buttons = topButton != null && bottomButton != null
                ? (new[] { topButton, bottomButton })
                : topButton == null && bottomButton == null ? default : (new[] { topButton ?? bottomButton });

            return this;
        }
        #endregion

        #region Cloning and Merging 

        public RichPresence Clone()
        {
            return new RichPresence
            {
                State = _state != null ? _state.Clone() as string : null,
                StateUrl = _stateUrl != null ? _stateUrl.Clone() as string : null,
                Details = _details != null ? _details.Clone() as string : null,
                DetailsUrl = _detailsUrl != null ? _detailsUrl.Clone() as string : null,
                Type = Type,
                StatusDisplay = StatusDisplay,
                Buttons = !HasButtons() ? null : Buttons.Clone() as Button[],

#pragma warning disable CS0618 // Type or member is obsolete
                Secrets = !HasSecrets() ? null : new Secrets
                {
                    Join = Secrets.Join != null ? Secrets.Join.Clone() as string : null,
                    SpectateSecret = Secrets.SpectateSecret != null ? Secrets.SpectateSecret.Clone() as string : null
                },
#pragma warning restore CS0618 // Type or member is obsolete

                Timestamps = !HasTimestamps() ? null : new Timestamps
                {
                    Start = Timestamps.Start,
                    End = Timestamps.End
                },

                Assets = !HasAssets() ? null : new Assets
                {
                    LargeImageKey = Assets.LargeImageKey != null ? Assets.LargeImageKey.Clone() as string : null,
                    LargeImageText = Assets.LargeImageText != null ? Assets.LargeImageText.Clone() as string : null,
                    LargeImageUrl = Assets.LargeImageUrl != null ? Assets.LargeImageUrl.Clone() as string : null,
                    SmallImageKey = Assets.SmallImageKey != null ? Assets.SmallImageKey.Clone() as string : null,
                    SmallImageText = Assets.SmallImageText != null ? Assets.SmallImageText.Clone() as string : null,
                    SmallImageUrl = Assets.SmallImageUrl != null ? Assets.SmallImageUrl.Clone() as string : null,
                },

                Party = !HasParty() ? null : new Party
                {
                    ID = Party.ID,
                    Size = Party.Size,
                    Max = Party.Max,
                    Privacy = Party.Privacy,
                },
            };
        }

        internal RichPresence Merge(BaseRichPresence presence)
        {
            _state = presence.State;
            _stateUrl = presence.StateUrl;
            _details = presence.Details;
            _detailsUrl = presence.DetailsUrl;
            Type = presence.Type;
            StatusDisplay = presence.StatusDisplay;
            Party = presence.Party;
            Timestamps = presence.Timestamps;
            Secrets = presence.Secrets;

            if (presence.HasAssets())
            {
                if (!HasAssets())
                {
                    Assets = presence.Assets;
                }
                else
                {
                    Assets.Merge(presence.Assets);
                }
            }
            else
            {
                Assets = null;
            }

            return this;
        }

        internal override bool Matches(RichPresence other)
        {
            if (!base.Matches(other)) return false;

            if (Buttons == null ^ other.Buttons == null) return false;
            if (Buttons != null)
            {
                if (Buttons.Length != other.Buttons.Length) return false;
                for (int i = 0; i < Buttons.Length; i++)
                {
                    var a = Buttons[i];
                    var b = other.Buttons[i];
                    if (a.Label != b.Label || a.Url != b.Url)
                        return false;
                }
            }

            return true;
        }

        #endregion

        public static implicit operator bool(RichPresence presesnce)
        {
            return presesnce != null;
        }
    }

    internal sealed class RichPresenceResponse : BaseRichPresence
    {
        [JsonProperty("application_id")]
        public string ClientID { get; private set; }

        [JsonProperty("name")]
        public string Name { get; private set; }
    }
}
