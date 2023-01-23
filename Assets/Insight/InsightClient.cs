using Mirror;
using System;
using UnityEngine;

namespace Insight
{
    public class InsightClient : InsightCommon
    {
        public static InsightClient instance;

       // [Tooltip("-Optional experimental false setting-\nStay connected to Master Server upon joining Game Server, this is required for certain features like cross-server chat.\nFalse will lighten the Master Server load, using fewer resources and allowing more connections.")]
       // public bool StayConnected = true;
        //public bool AutoReconnect = true;
        public bool AuthClientUponConnect = false;
        [Tooltip("Set false to log only warnings and errors, ideal for release build.")]
        public bool NoisyLogs = true;

        protected int clientID = -1; //-1 = never connected, 0 = disconnected, 1 = connected
        protected int connectionID = 0;

        InsightNetworkConnection insightNetworkConnection;
        public ClientAuthentication clientAuthentication;
        public InsightGameSettings gameSettingsModule;

        public float ReconnectDelayInSeconds = 5f;
        float _reconnectTimer;
        bool active;

        public override void Awake()
        {
            base.Awake();
            if (DontDestroy)
            {
                if (instance != null && instance != this)
                {
                    Destroy(gameObject);
                    return;
                }
                instance = this;
                DontDestroyOnLoad(this);
            }
            else
            {
                instance = this;
            }
        }

        public virtual void Start()
        {
            Application.runInBackground = true;

            clientID = 0;
            insightNetworkConnection = new InsightNetworkConnection();
            insightNetworkConnection.Initialize(this, networkAddress, clientID, connectionID);
            insightNetworkConnection.SetHandlers(messageHandlers);

            transport.OnClientConnected=OnConnected;
            transport.OnClientDataReceived=HandleBytes;
            transport.OnClientDisconnected=OnDisconnected;
            transport.OnClientError=OnError;

            if(AutoStart)
            {
                StartInsight();
            }
        }

        public override void NetworkEarlyUpdate()
        {
            transport.ClientEarlyUpdate();
        }

        public override void NetworkLateUpdate()
        {
            CheckCallbackTimeouts();

            transport.ClientLateUpdate();
        }

        public virtual void Update()
        {
            CheckConnection();
        }

        public void StartInsight(string Address)
        {
            if(string.IsNullOrEmpty(Address))
            {
                Debug.LogError("[InsightClient] - Address provided in StartInsight is Null or Empty. Not Starting.");
                return;
            }

            networkAddress = Address;

            StartInsight();
        }

        public override void StartInsight()
        {
            active = true;

            transport.ClientConnect(networkAddress);

            OnStartInsight();

            _reconnectTimer = Time.realtimeSinceStartup + ReconnectDelayInSeconds;
        }

        public void StartInsight(Uri uri)
        {
            active = true;

            transport.ClientConnect(uri);

            OnStartInsight();

            _reconnectTimer = Time.realtimeSinceStartup + ReconnectDelayInSeconds;
        }

        public override void StopInsight()
        {
            active = true;

            transport.ClientDisconnect();

            if (connectState != ConnectState.Disconnected)
            {
                connectState = ConnectState.Disconnected;

                OnStopInsight();
            }
        }

        private void CheckConnection()
        {
            //if (AutoReconnect)
            //{
                if (active && !isConnected && (_reconnectTimer > 0 && _reconnectTimer < Time.time))
                {
                    if(NoisyLogs) Debug.Log("[InsightClient] - Trying to reconnect...");
                    _reconnectTimer = Time.realtimeSinceStartup + ReconnectDelayInSeconds;
                    StartInsight();
                }
           // }
        }

        public void Send(byte[] data)
        {
            transport.ClientSend(new ArraySegment<byte>(data), 0);
        }

        public void Send<T>(T msg) where T : Message
        {
            Send(msg, null);
        }

        public void Send<T>(T msg, CallbackHandler callback) where T : Message
        {
            if (!transport.ClientConnected())
            {
                Debug.LogError("[InsightClient] - Client not connected!");
                return;
            }

            NetworkWriter writer = new NetworkWriter();
            int msgType = GetId(default(T) != null ? typeof(T) : msg.GetType());
            writer.WriteUShort((ushort)msgType);
            int callbackId = 0;
            if (callback != null)
            {
                callbackId = ++callbackIdIndex; // pre-increment to ensure that id 0 is never used.
                callbacks.Add(callbackId, new CallbackData()
                {
                    callback = callback,
                    timeout = Time.realtimeSinceStartup + callbackTimeout
                });
            }
            writer.WriteInt(callbackId);
            Writer<T>.write.Invoke(writer, msg);
            transport.ClientSend(new ArraySegment<byte>(writer.ToArray()), 0);
        }

        void HandleCallbackHandler(CallbackStatus status, NetworkReader reader)
        {
        }

        void OnConnected()
        {
            if (insightNetworkConnection != null)
            {
                if(NoisyLogs) Debug.Log("[InsightClient] - Connected to Insight Server");
                connectState = ConnectState.Connected;

                if (InsightServer.instance == null && AuthClientUponConnect)
                {
                    if (clientAuthentication)
                        clientAuthentication.SendLoginMsg();
                }
            }
            else Debug.LogError("Skipped Connect message handling because m_Connection is null.");
        }

        void OnDisconnected()
        {
            if (connectState != ConnectState.Disconnected)
            {
                connectState = ConnectState.Disconnected;

                OnStopInsight();
            }
        }

        protected void HandleBytes(ArraySegment<byte> data, int i)
        {
            InsightNetworkMessageDelegate msgDelegate;
            NetworkReader reader = new NetworkReader(data);
            if(UnpackMessage(reader, out int msgType))
            {
                int callbackId = reader.ReadInt();
                InsightNetworkMessage msg = new InsightNetworkMessage(insightNetworkConnection, callbackId)
                {
                    msgType = msgType,
                    reader = reader
                };

                if (callbacks.ContainsKey(callbackId))
                {
                    callbacks[callbackId].callback.Invoke(msg);
                    callbacks.Remove(callbackId);
                }
                else if (messageHandlers.TryGetValue(msgType, out msgDelegate))
                {
                    msgDelegate(msg);
                }
            }
            else
            {
                //NOTE: this throws away the rest of the buffer. Need moar error codes
                Debug.LogError("Unknown message ID " + msgType);// + " connId:" + connectionId);
            }
        }

#if MIRROR_71_0_OR_NEWER
        void OnError(int connectionId, TransportError error, string reason)
#else
        void OnError(Exception exception)
#endif
        {
            // TODO Let's discuss how we will handle errors
#if MIRROR_71_0_OR_NEWER
            Debug.LogWarning($"Insight Server Transport Error for connId={connectionId}: {error}: {reason}.");
#else
            Debug.LogException(exception);
#endif
        }

        void OnApplicationQuit()
        {
            if (NoisyLogs)
                Debug.Log("[InsightClient] Stopping Client");
            StopInsight();
        }

        ////------------Virtual Handlers-------------
        public virtual void OnStartInsight()
        {
            if (NoisyLogs)
                Debug.Log("[InsightClient] - Connecting to Insight Server: " + networkAddress);
        }

        public virtual void OnStopInsight()
        {
            if (NoisyLogs)
                Debug.Log("[InsightClient] - Disconnecting from Insight Server");
        }

        /*
        public void TemporarilyDisconnectFromInsightServer()
        {
            if (StayConnected == false)
            {
                if (NoisyLogs)
                    Debug.Log("[InsightClient] - Temporarily disconnecting from Insight Server");
                AutoReconnect = false;
                StopInsight();
            }
        }

        public void ReconnectToInsightServer()
        {
            if (StayConnected == false)
            {
                if (NoisyLogs)
                    Debug.Log("[InsightClient] - Reconnecting to Insight Server");
                AutoReconnect = true;
                StartInsight();
            }
        }
        */
    }
}
