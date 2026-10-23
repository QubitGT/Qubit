namespace Qubit.Libraries.DiscordRPC
{
    [System.Flags]
    public enum EventType
    {
        None = 0,

        [System.Obsolete("Spectating is no longer supported by Discord.")]
        Spectate = 0x1,

        Join = 0x2,

        JoinRequest = 0x4
    }
}
