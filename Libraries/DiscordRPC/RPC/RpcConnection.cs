using Qubit.Libraries.DiscordRPC.Entities;
using Qubit.Libraries.DiscordRPC.Helper;
using Qubit.Libraries.DiscordRPC.IO;
using Qubit.Libraries.DiscordRPC.Logging;
using Qubit.Libraries.DiscordRPC.Message;
using Qubit.Libraries.DiscordRPC.RPC.Commands;
using Qubit.Libraries.DiscordRPC.RPC.Payload;
using System;
using System.Collections.Generic;
using System.Threading;
using Valve.Newtonsoft.Json;

namespace Qubit.Libraries.DiscordRPC.RPC
{
    


    internal class RpcConnection : IDisposable
    {
        


        public static readonly int VERSION = 1;

        


        public static readonly int POLL_RATE = 1000;

        


        private static readonly bool CLEAR_ON_SHUTDOWN = true;

        


        private static readonly bool LOCK_STEP = false;

        


        public ILogger Logger
        {
            get { return _logger; }
            set
            {
                _logger = value;
                if (namedPipe != null)
                    namedPipe.Logger = value;
            }
        }
        private ILogger _logger;

        


        public event OnRpcMessageEvent OnRpcMessage;

        #region States

        


        public RpcState State
        {
            get
            {
                lock (l_states)
                    return _state;
            }
        }
        private RpcState _state;
        private readonly object l_states = new object();

        


        public Configuration Configuration { get { Configuration tmp = null; lock (l_config) tmp = _configuration; return tmp; } }
        private Configuration _configuration = null;
        private readonly object l_config = new object();

        private volatile bool aborting = false;
        private volatile bool shutdown = false;

        


        public bool IsRunning { get { return thread != null; } }

        



        public bool ShutdownOnly { get; set; }

        #endregion

        #region Privates

        private readonly string applicationID;                   
        private readonly int processID;                          

        private long nonce;                             

        private Thread thread;                          
        private readonly INamedPipeClient namedPipe;

        private readonly int targetPipe;                             

        private readonly object l_rtqueue = new object();   
        private readonly uint _maxRtQueueSize;
        private readonly Queue<ICommand> _rtqueue;                   

        private readonly object l_rxqueue = new object();   
        private readonly uint _maxRxQueueSize;              
        private readonly Queue<IMessage> _rxqueue;                   

        private readonly AutoResetEvent queueUpdatedEvent = new AutoResetEvent(false);
        private readonly BackoffDelay delay;                     
        #endregion

        








        public RpcConnection(string applicationID, int processID, int targetPipe, INamedPipeClient client, uint maxRxQueueSize = 128, uint maxRtQueueSize = 512)
        {
            this.applicationID = applicationID;
            this.processID = processID;
            this.targetPipe = targetPipe;
            namedPipe = client;
            ShutdownOnly = true;

            
            Logger = new ConsoleLogger();

            delay = new BackoffDelay(500, 60 * 1000);
            _maxRtQueueSize = maxRtQueueSize;
            _rtqueue = new Queue<ICommand>((int)_maxRtQueueSize + 1);

            _maxRxQueueSize = maxRxQueueSize;
            _rxqueue = new Queue<IMessage>((int)_maxRxQueueSize + 1);

            nonce = 0;
        }


        private long GetNextNonce()
        {
            nonce += 1;
            return nonce;
        }

        #region Queues
        



        internal void EnqueueCommand(ICommand command)
        {
            Logger.Trace("Enqueue Command: {0}", command.GetType().FullName);

            
            if (aborting || shutdown) return;

            
            lock (l_rtqueue)
            {
                
                if (_rtqueue.Count == _maxRtQueueSize)
                {
                    Logger.Error("Too many enqueued commands, dropping oldest one. Maybe you are pushing new presences to fast?");
                    _rtqueue.Dequeue();
                }

                
                _rtqueue.Enqueue(command);
            }
        }

        



        private void EnqueueMessage(IMessage message)
        {
            
            try
            {
                if (OnRpcMessage != null)
                    OnRpcMessage.Invoke(this, message);
            }
            catch (Exception e)
            {
                Logger.Error("Unhandled Exception while processing event: {0}", e.GetType().FullName);
                Logger.Error(e.Message);
                Logger.Error(e.StackTrace);
            }

            
            if (_maxRxQueueSize <= 0)
            {
                Logger.Trace("Enqueued Message, but queue size is 0.");
                return;
            }

            
            Logger.Trace("Enqueue Message: {0}", message.Type);
            lock (l_rxqueue)
            {
                
                if (_rxqueue.Count == _maxRxQueueSize)
                {
                    Logger.Warning("Too many enqueued messages, dropping oldest one.");
                    _rxqueue.Dequeue();
                }

                
                _rxqueue.Enqueue(message);
            }
        }

        



        internal IMessage DequeueMessage()
        {
            
            lock (l_rxqueue)
            {
                
                if (_rxqueue.Count == 0) return null;

                
                return _rxqueue.Dequeue();
            }
        }

        



        internal IMessage[] DequeueMessages()
        {
            
            lock (l_rxqueue)
            {
                
                IMessage[] messages = _rxqueue.ToArray();

                
                _rxqueue.Clear();

                
                return messages;
            }
        }
        #endregion

        


        private void MainLoop()
        {
            
            Logger.Info("RPC Connection Started");
            if (Logger.Level <= LogLevel.Trace)
            {
                Logger.Trace("============================");
                Logger.Trace("Assembly:             " + System.Reflection.Assembly.GetAssembly(typeof(RichPresence)).FullName);
                Logger.Trace("Pipe:                 " + namedPipe.GetType().FullName);
                Logger.Trace("Platform:             " + Environment.OSVersion);
                Logger.Trace("DotNet:               " + Environment.Version);
                Logger.Trace("applicationID:        " + applicationID);
                Logger.Trace("targetPipe:           " + targetPipe);
                Logger.Trace("POLL_RATE:            " + POLL_RATE);
                Logger.Trace("_maxRtQueueSize:      " + _maxRtQueueSize);
                Logger.Trace("_maxRxQueueSize:      " + _maxRxQueueSize);
                Logger.Trace("============================");
            }

            
            
            while (!aborting && !shutdown)
            {
                try
                {
                    
                    
                    if (namedPipe == null)
                    {
                        Logger.Error("Something bad has happened with our pipe client!");
                        aborting = true;
                        return;
                    }

                    
                    Logger.Trace("Connecting to the pipe through the {0}", namedPipe.GetType().FullName);
                    if (namedPipe.Connect(targetPipe))
                    {
                        #region Connected
                        
                        Logger.Trace("Connected to the pipe. Attempting to establish handshake...");

#pragma warning disable CS0618 // We know ConnectedPipe is obsolete, but we still need to use it for the ConnectionEstablishedMessage
                        EnqueueMessage(new ConnectionEstablishedMessage() { ConnectedPipe = namedPipe.ConnectedPipe });
#pragma warning restore CS0618

                        
                        EstablishHandshake();
                        Logger.Trace("Connection Established. Starting reading loop...");

                        
                        
                        
                        PipeFrame frame;
                        bool mainloop = true;
                        while (mainloop && !aborting && !shutdown && namedPipe.IsConnected)
                        {
                            #region Read Loop

                            
                            if (namedPipe.ReadFrame(out frame))
                            {
                                #region Read Payload
                                Logger.Trace("Read Payload: {0}", frame.Opcode);

                                
                                switch (frame.Opcode)
                                {
                                    
                                    case Opcode.Close:

                                        ClosePayload close = frame.GetObject<ClosePayload>();
                                        Logger.Warning("We have been told to terminate by discord: ({0}) {1}", close.Code, close.Reason);
                                        EnqueueMessage(new CloseMessage() { Code = close.Code, Reason = close.Reason });
                                        mainloop = false;
                                        break;

                                    
                                    case Opcode.Ping:
                                        Logger.Trace("PING");
                                        frame.Opcode = Opcode.Pong;
                                        namedPipe.WriteFrame(frame);
                                        break;

                                    
                                    case Opcode.Pong:
                                        Logger.Trace("PONG");
                                        break;

                                    
                                    case Opcode.Frame:
                                        if (shutdown)
                                        {
                                            
                                            Logger.Warning("Skipping frame because we are shutting down.");
                                            break;
                                        }

                                        if (frame.Data == null)
                                        {
                                            
                                            Logger.Error("We received no data from the frame so we cannot get the event payload!");
                                            break;
                                        }

                                        
                                        EventPayload response = null;
                                        try { response = frame.GetObject<EventPayload>(); }
                                        catch (Exception e)
                                        {
                                            Logger.Error("Failed to parse event! {0}", e.Message);
                                            Logger.Error("Data: {0}", frame.Message);
                                        }


                                        try { if (response != null) ProcessFrame(response); }
                                        catch (Exception e)
                                        {
                                            Logger.Error("Failed to process event! {0}", e.Message);
                                            Logger.Error("Data: {0}", frame.Message);
                                        }

                                        break;


                                    default:
                                    case Opcode.Handshake:
                                        
                                        Logger.Error("Invalid opcode: {0}", frame.Opcode);
                                        mainloop = false;
                                        break;
                                }

                                #endregion
                            }

                            if (!aborting && namedPipe.IsConnected)
                            {
                                
                                ProcessCommandQueue();

                                
                                queueUpdatedEvent.WaitOne(POLL_RATE);
                            }

                            #endregion
                        }
                        #endregion

                        Logger.Trace("Left main read loop for some reason. Aborting: {0}, Shutting Down: {1}", aborting, shutdown);
                    }
                    else
                    {
                        Logger.Error("Failed to connect for some reason.");
                        EnqueueMessage(new ConnectionFailedMessage() { FailedPipe = targetPipe });
                    }

                    
                    if (!aborting && !shutdown)
                    {
                        
                        
                        long sleep = delay.NextDelay();

                        Logger.Trace("Waiting {0}ms before attempting to connect again", sleep);
                        Thread.Sleep(delay.NextDelay());
                    }
                }
                
                
                
                
                catch (Exception e)
                {
                    Logger.Error("Unhandled Exception: {0}", e.GetType().FullName);
                    Logger.Error(e.Message);
                    Logger.Error(e.StackTrace);
                }
                finally
                {
                    
                    if (namedPipe.IsConnected)
                    {
                        
                        Logger.Trace("Closing the named pipe.");
                        namedPipe.Close();
                    }

                    
                    SetConnectionState(RpcState.Disconnected);
                }
            }

            
            Logger.Trace("Left Main Loop");
            if (namedPipe != null)
                namedPipe.Dispose();

            Logger.Info("Thread Terminated, no longer performing RPC connection.");
        }

        #region Reading

        

        private void ProcessFrame(EventPayload response)
        {
            Logger.Info("Handling Response. Cmd: {0}, Event: {1}", response.Command, response.Event);

            
            if (response.Event.HasValue && response.Event.Value == ServerEvent.Error)
            {
                
                Logger.Error("Error received from the RPC");

                
                ErrorMessage err = response.GetObject<ErrorMessage>();
                Logger.Error("Server responded with an error message: ({0}) {1}", err.Code.ToString(), err.Message);

                
                EnqueueMessage(err);
                return;
            }

            
            if (State == RpcState.Connecting)
            {
                if (response.Command == Command.Dispatch && response.Event.HasValue && response.Event.Value == ServerEvent.Ready)
                {
                    Logger.Info("Connection established with the RPC");
                    SetConnectionState(RpcState.Connected);
                    delay.Reset();

                    
                    ReadyMessage ready = response.GetObject<ReadyMessage>();
                    lock (l_config)
                    {
                        _configuration = ready.Configuration;
                        ready.User.SetConfiguration(_configuration);
                    }

                    
                    EnqueueMessage(ready);
                    return;
                }
            }

            if (State == RpcState.Connected)
            {
                switch (response.Command)
                {
                    
                    case Command.Dispatch:
                        ProcessDispatch(response);
                        break;

                    
                    case Command.SetActivity:
                        if (response.Data == null)
                        {
                            EnqueueMessage(new PresenceMessage());
                        }
                        else
                        {
                            RichPresenceResponse rp = response.GetObject<RichPresenceResponse>();
                            EnqueueMessage(new PresenceMessage(rp));
                        }
                        break;

                    case Command.Unsubscribe:
                    case Command.Subscribe:

                        
                        JsonSerializer serializer = new JsonSerializer();
                        serializer.Converters.Add(new Converters.EnumSnakeCaseConverter());

                        
                        var evt = response.GetObject<EventPayload>().Event.Value;

                        
                        if (response.Command == Command.Subscribe)
                            EnqueueMessage(new SubscribeMessage(evt));
                        else
                            EnqueueMessage(new UnsubscribeMessage(evt));

                        break;


                    case Command.SendActivityJoinInvite:
                        Logger.Trace("Got invite response ack.");
                        break;

                    case Command.CloseActivityJoinRequest:
                        Logger.Trace("Got invite response reject ack.");
                        break;

                    
                    default:
                        Logger.Error("Unknown frame was received! {0}", response.Command);
                        return;
                }
                return;
            }

            Logger.Trace("Received a frame while we are disconnected. Ignoring. Cmd: {0}, Event: {1}", response.Command, response.Event);
        }

        private void ProcessDispatch(EventPayload response)
        {
            if (response.Command != Command.Dispatch) return;
            if (!response.Event.HasValue) return;

            switch (response.Event.Value)
            {
                
                case ServerEvent.ActivitySpectate:
                    var spectate = response.GetObject<SpectateMessage>();
                    EnqueueMessage(spectate);
                    break;

                case ServerEvent.ActivityJoin:
                    var join = response.GetObject<JoinMessage>();
                    EnqueueMessage(join);
                    break;

                case ServerEvent.ActivityJoinRequest:
                    var request = response.GetObject<JoinRequestMessage>();
                    EnqueueMessage(request);
                    break;

                
                default:
                    Logger.Warning("Ignoring {0}", response.Event.Value);
                    break;
            }
        }

        #endregion

        #region Writting

        private void ProcessCommandQueue()
        {
            

            
            if (State != RpcState.Connected)
                return;

            
            if (aborting)
                Logger.Warning("We have been told to write a queue but we have also been aborted.");

            
            bool needsWriting = true;
            ICommand item = null;

            
            while (needsWriting && namedPipe.IsConnected)
            {
                lock (l_rtqueue)
                {
                    
                    
                    needsWriting = _rtqueue.Count > 0;
                    if (!needsWriting) break;

                    
                    item = _rtqueue.Peek();
                }

                
                if (shutdown || (!aborting && LOCK_STEP))
                    needsWriting = false;

                
                IPayload payload = item.PreparePayload(GetNextNonce());
                Logger.Trace("Attempting to send payload: {0}", payload.Command);

                
                PipeFrame frame = new PipeFrame();
                if (item is CloseCommand)
                {
                    
                    
                    SendHandwave();

                    
                    Logger.Trace("Handwave sent, ending queue processing.");
                    lock (l_rtqueue) _rtqueue.Dequeue();

                    
                    return;
                }
                else
                {
                    if (aborting)
                    {
                        
                        Logger.Warning("- skipping frame because of abort.");
                        lock (l_rtqueue) _rtqueue.Dequeue();
                    }
                    else
                    {
                        
                        frame.SetObject(Opcode.Frame, payload);

                        
                        Logger.Trace("Sending payload: {0}", payload.Command);
#if DEBUG
						Logger.Trace("\t- Frame: {0}", frame.Message);
#endif
                        if (namedPipe.WriteFrame(frame))
                        {
                            
                            Logger.Trace("Sent Successfully.");
                            lock (l_rtqueue) _rtqueue.Dequeue();
                        }
                        else
                        {
                            
                            Logger.Warning("Something went wrong during writing!");
                            return;
                        }
                    }
                }
            }
        }

        #endregion

        #region Connection

        



        private void EstablishHandshake()
        {
            Logger.Trace("Attempting to establish a handshake...");

            
            

            
            if (State != RpcState.Disconnected)
            {
                Logger.Error("State must be disconnected in order to start a handshake!");
                return;
            }

            
            Logger.Trace("Sending Handshake...");
            if (!namedPipe.WriteFrame(new PipeFrame(Opcode.Handshake, new Handshake() { Version = VERSION, ClientID = applicationID })))
            {
                Logger.Error("Failed to write a handshake.");
                return;
            }

            
            SetConnectionState(RpcState.Connecting);
        }

        


        private void SendHandwave()
        {
            Logger.Info("Attempting to wave goodbye...");

            
            if (State == RpcState.Disconnected)
            {
                Logger.Error("State must NOT be disconnected in order to send a handwave!");
                return;
            }

            
            if (!namedPipe.WriteFrame(new PipeFrame(Opcode.Close, new Handshake() { Version = VERSION, ClientID = applicationID })))
            {
                Logger.Error("failed to write a handwave.");
                return;
            }
        }


        



        public bool AttemptConnection()
        {
            Logger.Info("Attempting a new connection");

            
            if (thread != null)
            {
                Logger.Error("Cannot attempt a new connection as the previous connection thread is not null!");
                return false;
            }

            
            if (State != RpcState.Disconnected)
            {
                Logger.Warning("Cannot attempt a new connection as the previous connection hasn't changed state yet.");
                return false;
            }

            if (aborting)
            {
                Logger.Error("Cannot attempt a new connection while aborting!");
                return false;
            }

            
            thread = new Thread(MainLoop)
            {
                Name = "Discord IPC Thread",
                IsBackground = true
            };
            thread.Start();

            return true;
        }

        



        private void SetConnectionState(RpcState state)
        {
            Logger.Trace("Setting the connection state to {0}", state.ToString().ToSnakeCase().ToUpperInvariant());
            lock (l_states)
            {
                _state = state;
            }
        }

        



        public void Shutdown()
        {
            
            Logger.Trace("Initiated shutdown procedure");
            shutdown = true;

            
            lock (l_rtqueue)
            {
                _rtqueue.Clear();
                if (CLEAR_ON_SHUTDOWN) _rtqueue.Enqueue(new PresenceCommand() { PID = processID, Presence = null });
                _rtqueue.Enqueue(new CloseCommand());
            }

            
            queueUpdatedEvent.Set();
        }

        


        public void Close()
        {
            if (thread == null)
            {
                Logger.Error("Cannot close as it is not available!");
                return;
            }

            if (aborting)
            {
                Logger.Error("Cannot abort as it has already been aborted");
                return;
            }

            
            if (ShutdownOnly)
            {
                Shutdown();
                return;
            }

            
            Logger.Trace("Updating Abort State...");
            aborting = true;
            queueUpdatedEvent.Set();
        }


        


        public void Dispose()
        {
            ShutdownOnly = false;
            Close();
        }
        #endregion

    }

    


    internal enum RpcState
    {
        


        Disconnected,

        


        Connecting,

        


        Connected
    }
}