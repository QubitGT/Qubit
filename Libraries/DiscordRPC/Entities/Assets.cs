using Qubit.Libraries.DiscordRPC.Exceptions;
using System;
using Valve.Newtonsoft.Json;

namespace Qubit.Libraries.DiscordRPC.Entities
{



    [Serializable]
    public class Assets
    {
        private const string EXTERNAL_KEY_PREFIX = "mp:external";

        #region Large Image





        [JsonProperty("large_image", NullValueHandling = NullValueHandling.Ignore)]
        public string LargeImageKey
        {
            get { return _largeimagekey; }
            set
            {
                if (!BaseRichPresence.ValidateString(value, out _largeimagekey, false, 256))
                    throw new StringOutOfRangeException(256);


                LargeImageID = null;
            }
        }
        private string _largeimagekey;





        [JsonProperty("large_text", NullValueHandling = NullValueHandling.Ignore)]
        public string LargeImageText
        {
            get { return _largeimagetext; }
            set
            {
                if (!BaseRichPresence.ValidateString(value, out _largeimagetext, false, 128))
                    throw new StringOutOfRangeException(128);
            }
        }
        private string _largeimagetext;





        [JsonProperty("large_url", NullValueHandling = NullValueHandling.Ignore)]
        public string LargeImageUrl
        {
            get { return _largeimageurl; }
            set
            {
                if (!BaseRichPresence.ValidateString(value, out _largeimageurl, false, 256))
                    throw new StringOutOfRangeException(256);

                if (!BaseRichPresence.ValidateUrl(_largeimageurl))
                    throw new ArgumentException("Url must be a valid URI");
            }
        }
        private string _largeimageurl;




        [JsonIgnore]
        public string LargeImageID { get; private set; }




        [JsonIgnore]
        public bool IsLargeImageKeyExternal { get; private set; }
        #endregion

        #region Small Image





        [JsonProperty("small_image", NullValueHandling = NullValueHandling.Ignore)]
        public string SmallImageKey
        {
            get { return _smallimagekey; }
            set
            {
                if (!BaseRichPresence.ValidateString(value, out _smallimagekey, false, 256))
                    throw new StringOutOfRangeException(256);


                SmallImageID = null;
            }
        }
        private string _smallimagekey;





        [JsonProperty("small_text", NullValueHandling = NullValueHandling.Ignore)]
        public string SmallImageText
        {
            get { return _smallimagetext; }
            set
            {
                if (!BaseRichPresence.ValidateString(value, out _smallimagetext, false, 128))
                    throw new StringOutOfRangeException(128);
            }
        }
        private string _smallimagetext;





        [JsonProperty("small_url", NullValueHandling = NullValueHandling.Ignore)]
        public string SmallImageUrl
        {
            get { return _smallimageurl; }
            set
            {
                if (!BaseRichPresence.ValidateString(value, out _smallimageurl, false, 256))
                    throw new StringOutOfRangeException(256);

                if (!BaseRichPresence.ValidateUrl(_smallimageurl))
                    throw new ArgumentException("Url must be a valid URI");
            }
        }
        private string _smallimageurl;




        [JsonIgnore]
        public string SmallImageID { get; private set; }




        [JsonIgnore]
        public bool IsSmallImageKeyExternal { get; private set; }
        #endregion






        internal void Merge(Assets other)
        {

            _smallimagetext = other._smallimagetext;
            _smallimageurl = other._smallimageurl;
            _largeimagetext = other._largeimagetext;
            _largeimageurl = other._largeimageurl;


            string largeKey = other._largeimagekey ?? "";
            if (largeKey.StartsWith(EXTERNAL_KEY_PREFIX))
            {
                IsLargeImageKeyExternal = true;
                LargeImageID = largeKey;
            }
            else if (ulong.TryParse(largeKey, out _))
            {
                IsLargeImageKeyExternal = false;
                LargeImageID = largeKey;
            }
            else
            {
                IsLargeImageKeyExternal = false;
                LargeImageID = null;
                _largeimagekey = largeKey;
            }



            string smallKey = other._smallimagekey ?? "";
            if (smallKey.StartsWith(EXTERNAL_KEY_PREFIX))
            {
                IsSmallImageKeyExternal = true;
                SmallImageID = smallKey;
            }
            else if (ulong.TryParse(smallKey, out _))
            {
                IsSmallImageKeyExternal = false;
                SmallImageID = smallKey;
            }
            else
            {
                IsSmallImageKeyExternal = false;
                SmallImageID = null;
                _smallimagekey = smallKey;
            }
        }
    }
}
