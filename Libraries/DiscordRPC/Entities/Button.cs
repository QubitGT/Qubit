using Qubit.Libraries.DiscordRPC.Exceptions;
using System;
using System.Text;
using Valve.Newtonsoft.Json;

namespace Qubit.Libraries.DiscordRPC.Entities
{
    


    public class Button
    {
        



        [JsonProperty("label")]
        public string Label
        {
            get { return _label; }
            set
            {
                if (!BaseRichPresence.ValidateString(value, out _label, true, 31, Encoding.UTF8))
                    throw new StringOutOfRangeException(31);
            }
        }
        private string _label;

        



        [JsonProperty("url")]
        public string Url
        {
            get { return _url; }
            set
            {
                if (!BaseRichPresence.ValidateString(value, out _url, false, 512))
                    throw new StringOutOfRangeException(512);

                if (!BaseRichPresence.ValidateUrl(_url))
                    throw new ArgumentException("Url must be a valid URI");
            }
        }
        private string _url;
    }

}
