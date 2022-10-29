using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Insight;

public class InsightNetworkManager : NetworkManager
{
    public override void OnClientConnect()
    {
        InsightClient.instance.TemporarilyDisconnectFromInsightServer();
        base.OnClientConnect();
    }

    public override void OnClientDisconnect()
    {
        InsightClient.instance.ReconnectToInsightServer();
        base.OnClientDisconnect();
    }
}
