using System;

namespace Qubit.Libraries.DiscordRPC.Exceptions
{



    public class BadPresenceException : Exception
    {
        internal BadPresenceException(string message) : base(message) { }
    }
}
