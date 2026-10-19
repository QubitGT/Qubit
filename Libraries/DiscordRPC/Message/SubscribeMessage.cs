using Qubit.Libraries.DiscordRPC.RPC.Payload;

namespace Qubit.Libraries.DiscordRPC.Message
{



    public class SubscribeMessage : IMessage
    {



        public override MessageType Type { get { return MessageType.Subscribe; } }




        public EventType Event { get; internal set; }

        internal SubscribeMessage(ServerEvent evt)
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
