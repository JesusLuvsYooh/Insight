using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Insight;

public class InsightNetworkManager : NetworkManager
{
    public override void OnClientConnect()
    {
        // use the StayConnected bool on InsightClient script.
        // ignore this if you require players to stay connected to MasterServer, such as for global chat.
        if (InsightServer.instance == null && InsightClient.instance.StayConnected == false) // so game server does not call these
        { InsightClient.instance.TemporarilyDisconnectFromInsightServer(); }

        base.OnClientConnect();
    }

    public override void OnClientDisconnect()
    {
        // use the StayConnected bool on InsightClient script.
        // ignore this if you require players to stay connected to MasterServer, such as for global chat.
        if (InsightServer.instance == null && InsightClient.instance.AutoReconnect) // so game server does not call these
        { InsightClient.instance.ReconnectToInsightServer(); }
        // you may not need the reconnect, if game resets after disconnecting, which will re-connect to insight MS 

        base.OnClientDisconnect();
    }
}
