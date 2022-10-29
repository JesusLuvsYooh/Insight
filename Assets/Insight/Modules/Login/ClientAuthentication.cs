using Mirror;
using UnityEngine;

//TODO: Remove the example specific code from module

namespace Insight
{
    public class ClientAuthentication : InsightModule
    {
        InsightClient client;

        public string uniqueID;
        private string userName;
        private string userPassword;

        //This is put in the GUI. Just for example purposes
        internal string loginResponse;
        internal bool loginSucessful;

        public override void Initialize(InsightClient client, ModuleManager manager)
        {
            this.client = client;

            RegisterHandlers();

            client.clientAuthentication = this;
        }

        void RegisterHandlers()
        {

        }

        public void SendLoginMsg()
        {
            client.Send(new LoginMsg() { AccountName = userName, AccountPassword = userPassword }, (reader) =>
            {
                LoginResponseMsg msg = reader.ReadMessage<LoginResponseMsg>();

                if (msg.Status == CallbackStatus.Success)
                {
                    uniqueID = msg.UniqueID;
                    loginSucessful = true;
                    loginResponse = "Login Successful!";
                    Debug.Log("[ClientAuthentication] - Login Successful!");
                }else if (msg.Status == CallbackStatus.Error)
                {
                    Debug.LogError("[ClientAuthentication] - Callback Error: Login error");
                }else if (msg.Status == CallbackStatus.Timeout)
                {
                    Debug.LogError("[ClientAuthentication] - Callback Error: Login attempt timed out");
                }
            });
        }

        // we store details for later use, so client does not need UI input for a later reconnection to MasterServer
        public void SetClientLoginDetails(string username, string password)
        {
            userName = username;
            userPassword = password;
        }
    }
}