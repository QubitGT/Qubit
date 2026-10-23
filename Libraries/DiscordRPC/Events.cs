using Qubit.Libraries.DiscordRPC.Message;

namespace Qubit.Libraries.DiscordRPC
{
    public delegate void OnReadyEvent(object sender, ReadyMessage args);

    public delegate void OnCloseEvent(object sender, CloseMessage args);

    public delegate void OnErrorEvent(object sender, ErrorMessage args);

    public delegate void OnPresenceUpdateEvent(object sender, PresenceMessage args);

    public delegate void OnSubscribeEvent(object sender, SubscribeMessage args);

    public delegate void OnUnsubscribeEvent(object sender, UnsubscribeMessage args);

    public delegate void OnJoinEvent(object sender, JoinMessage args);

    [System.Obsolete("Spectating is no longer supported by Discord.")]
    public delegate void OnSpectateEvent(object sender, SpectateMessage args);

    public delegate void OnJoinRequestedEvent(object sender, JoinRequestMessage args);

    public delegate void OnConnectionEstablishedEvent(object sender, ConnectionEstablishedMessage args);

    public delegate void OnConnectionFailedEvent(object sender, ConnectionFailedMessage args);

    public delegate void OnRpcMessageEvent(object sender, IMessage msg);
}
