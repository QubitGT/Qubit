using Qubit.Managers.DiscordRPC.RPC.Payload;

namespace Qubit.Managers.DiscordRPC.RPC.Commands
{
    internal interface ICommand
    {
        IPayload PreparePayload(long nonce);
    }
}
