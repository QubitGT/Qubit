using System;
using Valve.Newtonsoft.Json;

namespace Qubit.Libraries.DiscordRPC.Entities
{
    


    public class User
    {
        


        public enum AvatarFormat
        {
            



            PNG,

            



            JPEG,

            



            WebP,

            



            GIF                 
        }

        


        public enum AvatarSize
        {
            
            x16 = 16,
            
            x32 = 32,
            
            x64 = 64,
            
            x128 = 128,
            
            x256 = 256,
            
            x512 = 512,
            
            x1024 = 1024,
            
            x2048 = 2048
        }

        


        public struct AvatarDecorationData
        {
            


            [JsonProperty("asset")]
            public string Asset { get; private set; }
            


            [JsonProperty("skuId")]
            public string SKU { get; private set; }
        }

        


        [JsonProperty("id")]
        public ulong ID { get; private set; }

        


        [JsonProperty("username")]
        public string Username { get; private set; }

        



        [JsonProperty("discriminator"), Obsolete("Discord no longer uses discriminators.")]
        public int Discriminator { get; private set; }

        



        [JsonProperty("global_name")]
        public string DisplayName { get; private set; }

        






        [JsonProperty("avatar")]
        public string Avatar { get; private set; }

        


        public bool IsAvatarAnimated => Avatar != null && Avatar.StartsWith("a_");

        



        [JsonProperty("avatar_decoration_data")]
        public AvatarDecorationData? AvatarDecoration { get; private set; }

        


        [JsonProperty("bot")]
        public bool Bot { get; private set; }

        


        [JsonProperty("flags", NullValueHandling = NullValueHandling.Ignore)]
        public Flag Flags { get; private set; }

        


        [Flags]
        public enum Flag
        {
            
            None = 0,

            
            Employee = 1 << 0,

            
            Partner = 1 << 1,

            
            HypeSquad = 1 << 2,

            
            BugHunter = 1 << 3,

            
            
            

            
            HouseBravery = 1 << 6,

            
            HouseBrilliance = 1 << 7,

            
            HouseBalance = 1 << 8,

            
            EarlySupporter = 1 << 9,

            
            TeamUser = 1 << 10,

            
            
            
            

            
            BugHunterLevel2 = 1 << 14,

            
            

            
            VerifiedBot = 1 << 16,

            
            VerifiedDeveloper = 1 << 17,

            
            CertifiedModerator = 1 << 18,

            
            BotHttpInteractions = 1 << 19,

            
            
            

            
            ActiveDeveloper = 1 << 22
        }

        


        [JsonProperty("premium_type", NullValueHandling = NullValueHandling.Ignore)]
        public PremiumType Premium { get; private set; }

        


        public enum PremiumType
        {
            
            None = 0,

            
            NitroClassic = 1,

            
            Nitro = 2,

            
            NitroBasic = 3
        }

        


        public string CdnEndpoint { get; private set; }

        


        internal User()
        {
            CdnEndpoint = "cdn.discordapp.com";
        }

        



        internal void SetConfiguration(Configuration configuration)
        {
            CdnEndpoint = configuration.CdnHost;
        }

        







        public string GetAvatarURL()
            => GetAvatarURL(AvatarFormat.PNG, AvatarSize.x128);

        










        public string GetAvatarURL(AvatarFormat format)
            => GetAvatarURL(format, AvatarSize.x128);

        










        public string GetAvatarURL(AvatarFormat format, AvatarSize size)
        {
            
            string endpoint = $"/avatars/{ID}/{Avatar}";

            
            if (string.IsNullOrEmpty(Avatar))
            {
                if (format != AvatarFormat.PNG)
                    throw new BadImageFormatException("The user has no avatar and the requested format " + format + " is not supported. (Only supports PNG).");

                
                int index = (int)((ID >> 22) % 6);
#pragma warning disable CS0618 // Disable the obsolete warning as we know the discriminator is obsolete and we are validating it here.
                if (Discriminator > 0)
                    index = Discriminator % 5;
#pragma warning restore CS0618

                endpoint = $"/embed/avatars/{index}";
            }

            
            return string.Format("https://{0}{1}{2}?size={3}&animated=true", CdnEndpoint, endpoint, GetAvatarExtension(format), (int)size);
        }

        




        public string GetAvatarDecorationURL()
            => GetAvatarDecorationURL(AvatarFormat.PNG);

        















        public string GetAvatarDecorationURL(AvatarFormat format)
        {
            if (AvatarDecoration == null)
                return null;

            string endpoint = $"/avatar-decoration-presets/{AvatarDecoration.Value.Asset}";
            return string.Format("https://{0}{1}{2}", CdnEndpoint, endpoint, GetAvatarExtension(format));
        }

        




        public string GetAvatarExtension(AvatarFormat format)
        {
            return "." + format.ToString().ToLowerInvariant();
        }

        




        public override string ToString()
        {
            if (!string.IsNullOrEmpty(DisplayName))
                return DisplayName;

#pragma warning disable CS0618 // Disable the obsolete warning as we know the discriminator is obsolete and we are validating it here.
            if (Discriminator != 0)
                return Username + "#" + Discriminator.ToString("D4");
#pragma warning restore CS0618

            return Username;
        }
    }
}
