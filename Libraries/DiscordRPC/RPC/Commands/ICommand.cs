using Qubit.Libraries.DiscordRPC.RPC.Payload;

namespace Qubit.Libraries.DiscordRPC.RPC.Commands
{
    internal interface ICommand
    {
        IPayload PreparePayload(long nonce);
    }
}
