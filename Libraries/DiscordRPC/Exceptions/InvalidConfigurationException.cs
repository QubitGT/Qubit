using System;

namespace Qubit.Libraries.DiscordRPC.Exceptions
{
    


    public class InvalidConfigurationException : Exception
    {
        internal InvalidConfigurationException(string message) : base(message) { }
    }
}
