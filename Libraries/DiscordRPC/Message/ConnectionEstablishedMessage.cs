namespace Qubit.Libraries.DiscordRPC.Message
{
    public class ConnectionEstablishedMessage : IMessage
    {
        public override MessageType Type { get { return MessageType.ConnectionEstablished; } }

        [System.Obsolete("The connected pipe is not neccessary information.")]
        public int ConnectedPipe { get; internal set; }
    }
}
