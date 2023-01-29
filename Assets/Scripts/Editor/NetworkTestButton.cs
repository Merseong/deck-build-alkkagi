using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(NetworkManager))]
public class NetworkTestButton : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        NetworkManager networkManager = (NetworkManager)target;
        if (GUILayout.Button("Connect to Server"))
        {
            networkManager.ConnectServer();
        }
        if (GUILayout.Button("Disconnect"))
        {
            networkManager.DisconnectServer();
        }
        if (GUILayout.Button("Send msg to Opponent"))
        {
            networkManager.SendMessageToOpponent();
        }
    }
}
