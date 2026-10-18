using Qubit.Libraries.DiscordRPC.Entities;
using Qubit.Libraries.DiscordRPC.Exceptions;
using Qubit.Libraries.DiscordRPC.IO;
using Qubit.Libraries.DiscordRPC.Logging;
using Qubit.Libraries.DiscordRPC.Message;
using Qubit.Libraries.DiscordRPC.RPC;
using Qubit.Libraries.DiscordRPC.RPC.Commands;
using System;

namespace Qubit.Libraries.DiscordRPC
{

    


    public sealed class DiscordRpcClient : IDisposable
    {
        #region Properties


        



        public bool HasRegisteredUriScheme { get; private set; }

        


        public string ApplicationID { get; private set; }

        


        public string SteamID { get; private set; }

        


        public int ProcessID { get; private set; }

        


        public int MaxQueueSize { get; private set; }

        


        public bool IsDisposed { get; private set; }

        


        public ILogger Logger
        {
            get { return _logger; }
            set
            {
                _logger = value;
                if (connection != null) connection.Logger = value;
            }
        }
        private ILogger _logger;

        


        public bool AutoEvents { get; private set; }

        


        public bool SkipIdenticalPresence { get; set; }
        #endregion

        



        public int TargetPipe { get; private set; }

        private readonly RpcConnection connection;

        


        public RichPresence CurrentPresence { get; private set; }

        


        public EventType Subscription { get; private set; }

        


        public User CurrentUser { get; private set; }

        


        public Configuration Configuration { get; private set; }

        


        public bool IsInitialized { get; private set; }

        



        public bool ShutdownOnly
        {
            get { return _shutdownOnly; }
            set
            {
                _shutdownOnly = value;
                if (connection != null) connection.ShutdownOnly = value;
            }
        }
        private bool _shutdownOnly = true;
        private readonly object _sync = new object();

        #region Events

        



        public event OnReadyEvent OnReady;

        



        public event OnCloseEvent OnClose;

        



        public event OnErrorEvent OnError;

        



        public event OnPresenceUpdateEvent OnPresenceUpdate;

        



        public event OnSubscribeEvent OnSubscribe;

        



        public event OnUnsubscribeEvent OnUnsubscribe;

        



        public event OnJoinEvent OnJoin;

        



        [Obsolete("Spectating is no longer supported by Discord.")]
        public event OnSpectateEvent OnSpectate;

        



        public event OnJoinRequestedEvent OnJoinRequested;

        



        public event OnConnectionEstablishedEvent OnConnectionEstablished;

        



        public event OnConnectionFailedEvent OnConnectionFailed;

        


        public event OnRpcMessageEvent OnRpcMessage;
        #endregion

        #region Initialization

        



        public DiscordRpcClient(string applicationID) : this(applicationID, -1) { }

        







        public DiscordRpcClient(string applicationID, int pipe = -1, ILogger logger = null, bool autoEvents = true, INamedPipeClient client = null)
        {
            
            if (string.IsNullOrEmpty(applicationID))
                throw new ArgumentNullException("applicationID");

            
            ApplicationID = applicationID.Trim();
            TargetPipe = pipe;
            ProcessID = System.Diagnostics.Process.GetCurrentProcess().Id;
            HasRegisteredUriScheme = false;
            AutoEvents = autoEvents;
            SkipIdenticalPresence = true;

            
            _logger = logger ?? new NullLogger();

            
            connection = new RpcConnection(ApplicationID, ProcessID, TargetPipe, client ?? new ManagedNamedPipeClient(), autoEvents ? 0 : 128U)
            {
                ShutdownOnly = _shutdownOnly,
                Logger = _logger
            };

            
            connection.OnRpcMessage += (sender, msg) =>
            {
                if (OnRpcMessage != null)
                    OnRpcMessage.Invoke(this, msg);

                if (AutoEvents)
                    ProcessMessage(msg);
            };
        }

        #endregion

        #region Message Handling
        




        public IMessage[] Invoke()
        {
            if (AutoEvents)
            {
                Logger.Error("Cannot Invoke client when AutomaticallyInvokeEvents has been set.");
                return new IMessage[0];
                
            }

            
            IMessage[] messages = connection.DequeueMessages();
            for (int i = 0; i < messages.Length; i++)
            {
                
                var message = messages[i];
                ProcessMessage(message);
            }

            
            return messages;
        }

        



		private void ProcessMessage(IMessage message)
        {
            if (message == null) return;
            switch (message.Type)
            {
                
                case MessageType.PresenceUpdate:
                    lock (_sync)
                    {
                        var pm = message as PresenceMessage;
                        if (pm != null)
                        {
                            
                            if (pm.Presence == null)
                            {
                                CurrentPresence = null;
                            }
                            else if (CurrentPresence == null)
                            {
                                CurrentPresence = (new RichPresence()).Merge(pm.Presence);
                            }
                            else
                            {
                                CurrentPresence.Merge(pm.Presence);
                            }

                            
                            pm.Presence = CurrentPresence;
                        }
                    }

                    if (OnPresenceUpdate != null)
                        OnPresenceUpdate.Invoke(this, message as PresenceMessage);

                    break;

                
                case MessageType.Ready:
                    var rm = message as ReadyMessage;
                    if (rm != null)
                    {
                        lock (_sync)
                        {
                            Configuration = rm.Configuration;
                            CurrentUser = rm.User;
                        }

                        
                        SynchronizeState();
                    }

                    if (OnReady != null)
                        OnReady.Invoke(this, message as ReadyMessage);

                    break;

                case MessageType.Close:
                    if (OnClose != null)
                        OnClose.Invoke(this, message as CloseMessage);
                    break;

                case MessageType.Error:
                    if (OnError != null)
                        OnError.Invoke(this, message as ErrorMessage);
                    break;

                
                case MessageType.JoinRequest:
                    if (Configuration != null)
                    {
                        
                        var jrm = message as JoinRequestMessage;
                        if (jrm != null) jrm.User.SetConfiguration(Configuration);
                    }
                    if (OnJoinRequested != null)
                        OnJoinRequested.Invoke(this, message as JoinRequestMessage);
                    break;

                case MessageType.Subscribe:
                    lock (_sync)
                    {
                        var sub = message as SubscribeMessage;
                        Subscription |= sub.Event;
                    }

                    if (OnSubscribe != null)
                        OnSubscribe.Invoke(this, message as SubscribeMessage);

                    break;

                case MessageType.Unsubscribe:
                    lock (_sync)
                    {
                        var unsub = message as UnsubscribeMessage;
                        Subscription &= ~unsub.Event;
                    }

                    if (OnUnsubscribe != null)
                        OnUnsubscribe.Invoke(this, message as UnsubscribeMessage);

                    break;

                case MessageType.Join:
                    if (OnJoin != null)
                        OnJoin.Invoke(this, message as JoinMessage);
                    break;

                case MessageType.Spectate:
                    if (OnSpectate != null)
                        OnSpectate.Invoke(this, message as SpectateMessage);
                    break;

                case MessageType.ConnectionEstablished:
                    if (OnConnectionEstablished != null)
                        OnConnectionEstablished.Invoke(this, message as ConnectionEstablishedMessage);
                    break;

                case MessageType.ConnectionFailed:
                    if (OnConnectionFailed != null)
                        OnConnectionFailed.Invoke(this, message as ConnectionFailedMessage);
                    break;

                
                default:
                    Logger.Error("Message was queued with no appropriate handle! {0}", message.Type);
                    break;
            }
        }
        #endregion

        





        public void Respond(JoinRequestMessage request, bool acceptRequest)
            => Respond(request.User.ID, acceptRequest);

        





        public void Respond(User user, bool acceptRequest)
            => Respond(user.ID, acceptRequest);

        





        public void Respond(ulong userID, bool acceptRequest)
        {
            if (IsDisposed)
                throw new ObjectDisposedException("Discord IPC Client");

            if (connection == null)
                throw new ObjectDisposedException("Connection", "Cannot initialize as the connection has been deinitialized");

            if (!IsInitialized)
                throw new UninitializedException();

            connection.EnqueueCommand(new RespondCommand()
            {
                Accept = acceptRequest,
                UserID = userID.ToString()
            });
        }

        



        public void SetPresence(RichPresence presence)
        {
            if (IsDisposed)
                throw new ObjectDisposedException("Discord IPC Client");

            if (connection == null)
                throw new ObjectDisposedException("Connection", "Cannot initialize as the connection has been deinitialized");

            if (!IsInitialized)
                Logger.Warning("The client is not yet initialized, storing the presence as a state instead.");

            
            if (presence == null)
            {
                
                if (!SkipIdenticalPresence || CurrentPresence != null)
                    connection.EnqueueCommand(new PresenceCommand() { PID = ProcessID, Presence = null });
            }
            else
            {
                
                
                if (presence.HasSecrets() && !HasRegisteredUriScheme)
                    throw new BadPresenceException("Cannot send a presence with secrets as this object has not registered a URI scheme. Please enable the uri scheme registration in the DiscordRpcClient constructor.");

                if (presence.HasParty() && presence.Party.Max < presence.Party.Size)
                    throw new BadPresenceException("Presence maximum party size cannot be smaller than the current size.");

                if (presence.HasSecrets() && !presence.HasParty())
                    Logger.Warning("The presence has set the secrets but no buttons will show as there is no party available.");

                
                if (!SkipIdenticalPresence || !presence.Matches(CurrentPresence))
                    connection.EnqueueCommand(new PresenceCommand() { PID = ProcessID, Presence = presence.Clone() });
            }

            
            lock (_sync)
            {
                CurrentPresence = presence?.Clone();
            }
        }

        #region Updates

        




        public RichPresence Update(Action<RichPresence> func)
        {
            if (!IsInitialized)
                throw new UninitializedException();

            
            RichPresence presence;
            lock (_sync)
            {
                presence = CurrentPresence == null ? new RichPresence() : CurrentPresence.Clone();
            }

            func(presence);
            SetPresence(presence);

            return presence;
        }

        




        public RichPresence UpdateType(ActivityType type) => Update(p => p.Type = type);
        




        public RichPresence UpdateStatusDisplayType(StatusDisplayType type) => Update(p => p.StatusDisplay = type);

        




        public RichPresence UpdateButtons(Button[] buttons = null) => Update(p => p.Buttons = buttons);

        





        public RichPresence SetButton(Button button, int index = 0) => Update(p => p.Buttons[index] = button);

        




        public RichPresence UpdateDetails(string details) => Update(p => p.Details = details);

        




        public RichPresence UpdateState(string state) => Update(p => p.State = state);

        




        public RichPresence UpdateParty(Party party) => Update(p => p.Party = party);

        





        public RichPresence UpdatePartySize(int size) => Update(p =>
        {
            
            if (p.Party == null)
                throw new BadPresenceException("Cannot set the size of the party if the party does not exist");

            
            p.Party.Size = size;
        });

        






        public RichPresence UpdatePartySize(int size, int max) => Update(p =>
        {
            
            if (p.Party == null)
                throw new BadPresenceException("Cannot set the size of the party if the party does not exist");

            p.Party.Size = size;
            p.Party.Max = max;
        });

        





        public RichPresence UpdateLargeAsset(string key = null, string tooltip = null) => Update(p =>
        {
            p.Assets ??= new Entities.Assets();

            p.Assets.LargeImageKey = key ?? p.Assets.LargeImageKey;
            p.Assets.LargeImageText = tooltip ?? p.Assets.LargeImageText;
        });

        





        public RichPresence UpdateSmallAsset(string key = null, string tooltip = null) => Update(p =>
        {
            p.Assets ??= new Entities.Assets();

            p.Assets.SmallImageKey = key ?? p.Assets.SmallImageKey;
            p.Assets.SmallImageText = tooltip ?? p.Assets.SmallImageText;
        });

        




        public RichPresence UpdateSecrets(Secrets secrets) => Update(p => p.Secrets = secrets);

        



        public RichPresence UpdateStartTime() => UpdateStartTime(DateTime.UtcNow);

        




        public RichPresence UpdateStartTime(DateTime time) => Update(p =>
        {
            p.Timestamps ??= new Timestamps();

            p.Timestamps.Start = time;
        });

        



        public RichPresence UpdateEndTime() => UpdateEndTime(DateTime.UtcNow);

        




        public RichPresence UpdateEndTime(DateTime time) => Update(p =>
        {
            p.Timestamps ??= new Timestamps();

            p.Timestamps.End = time;
        });

        



        public RichPresence UpdateClearTime() => Update(p => p.Timestamps = null);

        #endregion

        


        public void ClearPresence()
        {
            if (IsDisposed)
                throw new ObjectDisposedException("Discord IPC Client");

            if (!IsInitialized)
                throw new UninitializedException();

            if (connection == null)
                throw new ObjectDisposedException("Connection", "Cannot initialize as the connection has been deinitialized");

            
            SetPresence(null);
        }

        #region Subscriptions
        




        public void Subscribe(EventType type) { SetSubscription(Subscription | type); }

        




        public void Unsubscribe(EventType type) { SetSubscription(Subscription & ~type); }

        




        public void SetSubscription(EventType type)
        {
            if (IsInitialized)
            {
                
                SubscribeToTypes(Subscription & ~type, true);
                SubscribeToTypes(~Subscription & type, false);
            }
            else
            {
                Logger.Warning("Client has not yet initialized, but events are being subscribed too. Storing them as state instead.");
            }

            lock (_sync)
            {
                Subscription = type;
            }
        }

        




        private void SubscribeToTypes(EventType type, bool isUnsubscribe)
        {
            
            
            if (type == EventType.None) return;

            
            if (IsDisposed)
                throw new ObjectDisposedException("Discord IPC Client");

            if (!IsInitialized)
                throw new UninitializedException();

            if (connection == null)
                throw new ObjectDisposedException("Connection", "Cannot initialize as the connection has been deinitialized");

            
            if (!HasRegisteredUriScheme)
                throw new InvalidConfigurationException("Cannot subscribe/unsubscribe to an event as this application has not registered a URI Scheme. Call RegisterUriScheme().");

            
#pragma warning disable CS0618 // Type or member is obsolete
            if ((type & EventType.Spectate) == EventType.Spectate)
                connection.EnqueueCommand(new SubscribeCommand() { Event = RPC.Payload.ServerEvent.ActivitySpectate, IsUnsubscribe = isUnsubscribe });
#pragma warning restore CS0618 // Type or member is obsolete

            if ((type & EventType.Join) == EventType.Join)
                connection.EnqueueCommand(new SubscribeCommand() { Event = RPC.Payload.ServerEvent.ActivityJoin, IsUnsubscribe = isUnsubscribe });

            if ((type & EventType.JoinRequest) == EventType.JoinRequest)
                connection.EnqueueCommand(new SubscribeCommand() { Event = RPC.Payload.ServerEvent.ActivityJoinRequest, IsUnsubscribe = isUnsubscribe });
        }

        #endregion

        


        public void SynchronizeState()
        {
            
            if (!IsInitialized)
                throw new UninitializedException();

            
            SetPresence(CurrentPresence);
            if (HasRegisteredUriScheme)
                SubscribeToTypes(Subscription, false);
        }

        



        public bool Initialize()
        {
            return IsDisposed
                ? throw new ObjectDisposedException("Discord IPC Client")
                : IsInitialized
                ? throw new UninitializedException("Cannot initialize a client that is already initialized")
                : connection == null
                ? throw new ObjectDisposedException("Connection", "Cannot initialize as the connection has been deinitialized")
                : (IsInitialized = connection.AttemptConnection());
        }

        


        public void Deinitialize()
        {
            if (!IsInitialized)
                throw new UninitializedException("Cannot deinitialize a client that has not been initalized.");

            connection.Close();
            IsInitialized = false;
        }

        


        public void Dispose()
        {
            if (IsDisposed) return;
            if (IsInitialized) Deinitialize();
            IsDisposed = true;
        }

    }
}
