using Qubit.Libraries.DiscordRPC.RPC.Payload;

namespace Qubit.Libraries.DiscordRPC.Message
{



    public class UnsubscribeMessage : IMessage
    {



        public override MessageType Type { get { return MessageType.Unsubscribe; } }




        public EventType Event { get; internal set; }

        internal UnsubscribeMessage(ServerEvent evt)
        {
            switch (evt)
            {
                default:
                case ServerEvent.ActivityJoin:
                    Event = EventType.Join;
                    break;

                case ServerEvent.ActivityJoinRequest:
                    Event = EventType.JoinRequest;
                    break;
            }
        }
    }
}
