using System;

namespace Qubit.Libraries.DiscordRPC.Message
{
    


    public abstract class IMessage
    {
        


        public abstract MessageType Type { get; }

        


        public DateTime TimeCreated { get { return _timecreated; } }
        private readonly DateTime _timecreated;

        


        public IMessage()
        {
            _timecreated = DateTime.Now;
        }
    }
}
