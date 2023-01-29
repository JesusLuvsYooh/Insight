using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Insight
{
    public class InsightServer : InsightCommon
    {
        public static InsightServer instance;

        [Tooltip("Set false to log only warnings and errors, ideal for release build.")]
        public bool NoisyLogs = true;
        [Tooltip("Auto call login.auth upon clients connecting to MasterServer.")]
        public bool autoAuthClients = false;
        [Tooltip("This is required for certain features like cross-server chat.\nHowever False will lighten the Master Server load, using fewer resources and allowing more connections.")]
        public bool PlayerStayConnectedToMasterServer = true;
        protected int serverHostId = -1; //-1 = never connected, 0 = disconnected, 1 = connected
        public Dictionary<int, InsightNetworkConnection> connections = new Dictionary<int, InsightNetworkConnection>();
        protected List<SendToAllFinishedCallbackData> sendToAllFinishedCallbacks = new List<SendToAllFinishedCallbackData>();
        public ServerAuthentication serverAuthentication;
        public InsightGameSettings gameSettingsModule;

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

            transport.OnServerConnected=HandleConnect;
            transport.OnServerDisconnected=HandleDisconnect;
            transport.OnServerDataReceived=HandleData;
            transport.OnServerError=OnError;

            if (AutoStart)
            {
                StartInsight();
            }
        }

        public override void NetworkEarlyUpdate()
        {
            transport.ServerEarlyUpdate();
        }

        public override void NetworkLateUpdate()
        {
            CheckCallbackTimeouts();

            transport.ServerLateUpdate();
        }

        public override void StartInsight()
        {
            if(NoisyLogs) Debug.Log("[InsightServer] - Start");
            transport.ServerStart();
            serverHostId = 0;

            connectState = ConnectState.Connected;

            OnStartInsight();
        }

        public override void StopInsight()
        {
            connections.Clear();

            // stop the server when you don't need it anymore
            transport.ServerStop();
            serverHostId = -1;

            connectState = ConnectState.Disconnected;

            OnStopInsight();
        }

        

        void HandleConnect(int _connectionId)
        {
                //Debug.LogWarning("connectionId: " + connectionId);
            //if (serverID == 0)
            //{
            //    serverID = connectionId;
            //}
            // cant remember what this was for?
            //if (connectionId != serverID)
            //{
            //    Debug.LogWarning("Ignore Connection!");
            //    return;
            //}

            if (NoisyLogs)
                Debug.Log("[InsightServer] - Client connected connectionID: " + _connectionId, this);

            // get ip address from connection
            string address = GetConnectionInfo(_connectionId);

            // add player info
            InsightNetworkConnection conn = new InsightNetworkConnection();
            conn.Initialize(this, address, serverHostId, _connectionId);
            AddConnection(conn);

            //check ban list for matching address
            //    if true
            //        {
            // HandleDisconnect(connectionId);
            //  }

            // string UniqueId = Guid.NewGuid().ToString();
            //serverAuthentication.registeredUsers.Add(new UserContainer()
            //{
            //    username = "",
            //    uniqueId = UniqueId,
            //    connectionId = connectionId
            //});

            if (autoAuthClients)
            {
                if (connections.Count <= 0 )// == _connectionId)
                {
                    if (NoisyLogs)
                        Debug.Log("[InsightServer] - autoAuthClients, do not add MS as client.");
                    return;
                }

                if (NoisyLogs)
                    Debug.Log("[InsightServer] - autoAuthClients: " + _connectionId);

                if (serverAuthentication.GetUserByConnection(_connectionId) != null)
                {
                    if (NoisyLogs)
                        Debug.Log("[InsightServer] - autoAuthClients exists: " + _connectionId);
                }
                else
                {
                    if (NoisyLogs)
                        Debug.Log("[InsightServer] - autoAuthClients does not exist, adding: " + _connectionId);

                        string UniqueId = Guid.NewGuid().ToString();

                    serverAuthentication.registeredUsers.Add(new UserContainer()
                    {
                        username = "",
                        uniqueId = UniqueId,
                        connectionId = _connectionId
                    });

                    SendToClient(_connectionId, new LoginResponseMsg()
                    {
                        UniqueID = UniqueId,
                        Status = CallbackStatus.Success
                    }); ;
                }
               
            }
            
        }

        void HandleDisconnect(int connectionId)
        {
            if (NoisyLogs)
                Debug.Log("[InsightServer] - Client disconnected connectionID: " + connectionId, this);

            InsightNetworkConnection conn;
            if (connections.TryGetValue(connectionId, out conn))
            {
                conn.Disconnect();
                RemoveConnection(connectionId);
                serverAuthentication.registeredUsers.Remove(serverAuthentication.GetUserByConnection(connectionId));
            }
        }

       // int serverID = 0;

        void HandleData(int connectionId, ArraySegment<byte> data, int i)
        {
            //Debug.LogWarning("connectionId: " + connectionId);
            //if (serverID == 0)
            //{
            //    serverID = connectionId;
            //}
            //if (connectionId != serverID)
            //{
            //    Debug.LogWarning("Ignore Connection!");
            //    return;
            //}

            NetworkReader reader = new NetworkReader(data);
            short msgType = reader.ReadShort();
            int callbackId = reader.ReadInt();
            InsightNetworkConnection insightNetworkConnection;
            if (!connections.TryGetValue(connectionId, out insightNetworkConnection))
            {
                Debug.LogError("HandleData: Unknown connectionId: " + connectionId, this);
                return;
            }

            if (callbacks.ContainsKey(callbackId))
            {
                InsightNetworkMessage msg = new InsightNetworkMessage(insightNetworkConnection, callbackId) { msgType = msgType, reader = reader };
                callbacks[callbackId].callback.Invoke(msg);
                callbacks.Remove(callbackId);

                CheckForFinishedCallback(callbackId);
            }
            else
            {
                insightNetworkConnection.TransportReceive(data);
            }
        }
#if MIRROR_71_0_OR_NEWER
        void OnError(int connectionId, TransportError error, string reason)
#else
        void OnError(int connectionId, Exception exception)
#endif
        {
            // TODO Let's discuss how we will handle errors
#if MIRROR_71_0_OR_NEWER
            Debug.LogWarning($"Insight Server Transport Error for connId={connectionId}: {error}: {reason}.");
#else
            Debug.LogException(exception);
#endif
        }

        public string GetConnectionInfo(int connectionId)
        {
            return transport.ServerGetClientAddress(connectionId);
        }

        /// <summary>
        /// Disconnect client by specified connectionId
        /// </summary>
        /// <param name="connectionId">ConnectionId to be disconnected</param>
        public void Disconnect(int connectionId)
        {
            transport.ServerDisconnect(connectionId);
        }

        bool AddConnection(InsightNetworkConnection conn)
        {
            if (!connections.ContainsKey(conn.connectionId))
            {
                // connection cannot be null here or conn.connectionId
                // would throw NRE
                connections[conn.connectionId] = conn;
                conn.SetHandlers(messageHandlers);
                return true;
            }
            // already a connection with this id
            return false;
        }

        bool RemoveConnection(int connectionId)
        {
            return connections.Remove(connectionId);
        }

        public bool SendToClient<T>(int connectionId, T msg, CallbackHandler callback = null) where T : Message
        {
            if (transport.ServerActive())
            {
                NetworkWriter writer = new NetworkWriter();
                int msgType = GetId(default(Message) != null ? typeof(Message) : msg.GetType());
                writer.WriteUShort((ushort)msgType);
                int callbackId = 0;
                if (callback != null)
                {
                    callbackId = ++callbackIdIndex; // pre-increment to ensure that id 0 is never used.
                    callbacks.Add(callbackId, new CallbackData() { callback = callback, timeout = Time.realtimeSinceStartup + callbackTimeout });
                }
                writer.WriteInt(callbackId);
                Writer<T>.write.Invoke(writer, msg);

                return connections[connectionId].Send(writer.ToArray());
            }
            Debug.LogError("Server.Send: not connected!", this);
            return false;
        }

        public bool SendToClient<T>(int connectionId, T msg) where T : Message
        {
            return SendToClient(connectionId, msg, null);
        }

        public bool SendToClient(int connectionId, byte[] data)
        {
            if (transport.ServerActive())
            {
                transport.ServerSend(connectionId, new ArraySegment<byte>(data), 0);
                return true;
            }
            Debug.LogError("Server.Send: not connected!", this);
            return false;
        }

        public bool SendToAll<T>(T msg, CallbackHandler callback, SendToAllFinishedCallbackHandler finishedCallback) where T : Message
        {
            if (transport.ServerActive())
            {
                SendToAllFinishedCallbackData finishedCallbackData = new SendToAllFinishedCallbackData() { requiredCallbackIds = new HashSet<int>() };

                foreach (KeyValuePair<int, InsightNetworkConnection> conn in connections)
                {
                    SendToClient(conn.Key, msg, callback);
                    finishedCallbackData.requiredCallbackIds.Add(callbackIdIndex);
                }

                // you can't have _just_ the finishedCallback, although you _can_ have just
                // "normal" callback. 
                if (finishedCallback != null && callback != null)
                {
                    finishedCallbackData.callback = finishedCallback;
                    finishedCallbackData.timeout = Time.realtimeSinceStartup + callbackTimeout;
                    sendToAllFinishedCallbacks.Add(finishedCallbackData);
                }
                return true;
            }
            Debug.LogError("Server.Send: not connected!", this);
            return false;
        }

        public bool SendToAll<T>(T msg, CallbackHandler callback) where T : Message
        {
            return SendToAll(msg, callback, null);
        }

        public bool SendToAll<T>(T msg) where T : Message
        {
            return SendToAll(msg, null, null);
        }

        public bool SendToAll(byte[] bytes)
        {
            if (transport.ServerActive())
            {
                foreach (var conn in connections)
                {
                    conn.Value.Send(bytes);
                }
                return true;
            }
            Debug.LogError("Server.Send: not connected!", this);
            return false;
        }

        void OnApplicationQuit()
        {
            if (NoisyLogs)
                Debug.Log("[InsightServer] Stopping Server");
            transport.ServerStop();
        }

        void CheckForFinishedCallback(int callbackId)
        {
            foreach (var item in sendToAllFinishedCallbacks)
            {
                if (item.requiredCallbackIds.Contains(callbackId)) item.callbacks++;
                if (item.callbacks >= item.requiredCallbackIds.Count)
                {
                    item.callback.Invoke(CallbackStatus.Success);
                    sendToAllFinishedCallbacks.Remove(item);
                    return;
                }
            }
        }

        protected override void CheckCallbackTimeouts()
        {
            base.CheckCallbackTimeouts();
            foreach (var item in sendToAllFinishedCallbacks)
            {
                if (item.timeout < Time.realtimeSinceStartup)
                {
                    item.callback.Invoke(CallbackStatus.Timeout);
                    sendToAllFinishedCallbacks.Remove(item);
                    return;
                }
            }
        }

        ////----------virtual handlers--------------//
        public virtual void OnStartInsight()
        {
            if (NoisyLogs)
                Debug.Log("[InsightServer] - Server started listening");
        }

        public virtual void OnStopInsight()
        {
            if (NoisyLogs)
                Debug.Log("[InsightServer] - Server stopping");
        }
    }
}
