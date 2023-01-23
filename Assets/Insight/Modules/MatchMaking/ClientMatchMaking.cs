using System;
using UnityEngine;

namespace Insight
{
    public class ClientMatchMaking : InsightModule
    {
        InsightClient client;

        ClientGameManager gameManager;

        public event Action<MatchMakingResponseType, string> OnMatchMakingChange;

        public override void Initialize(InsightClient client, ModuleManager manager)
        {
            this.client = client;
            gameManager = manager.GetModule<ClientGameManager>();
            RegisterHandlers();
        }

        void RegisterHandlers()
        {
            client.RegisterHandler<MatchMakingResponseMsg>(HandleMatchMakingResponseMsg);
        }

        void HandleMatchMakingResponseMsg(InsightNetworkMessage netMsg)
        {

            MatchMakingResponseMsg message = netMsg.ReadMessage<MatchMakingResponseMsg>();

            string status = "";
            switch ((MatchMakingResponseType)message.ResponseType)
            {
                case MatchMakingResponseType.Search:
                status = "Searching for match...";
                break;
                case MatchMakingResponseType.Wait:
                status = "Waiting for players\n";
                if (message.WaitTime > 0)
                    status += "{0:mm\\:ss} ETA " + TimeSpan.FromSeconds(message.WaitTime).ToString("mm\\:ss");
                break;
                case MatchMakingResponseType.Full:
                status = "Queue is full\nPlease wait {0:mm\\:ss}";
                break;
                case MatchMakingResponseType.Join:
                status = "Joining server...";
                break;
                case MatchMakingResponseType.Timeout:
                status = "No players found!";
                break;
                case MatchMakingResponseType.Failed:
                status = "MatchMaking failed!";
                break;
            }
            Debug.Log("[MatchMakingResponse] " + status);
            OnMatchMakingChange?.Invoke((MatchMakingResponseType)message.ResponseType, status);
        }

        #region Message Senders
        public void SendStartMatchMaking(StartMatchMakingMsg startMatchMakingMsg)
        {
            client.Send(startMatchMakingMsg);
        }

        public void SendStopMatchMaking()
        {
            client.Send(new StopMatchMakingMsg());
        }
        #endregion
    }
}
