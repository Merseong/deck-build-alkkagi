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
        if (GUILayout.Button("Enter Room"))
        {
            networkManager.EnterGameRoom();
        }
        if (GUILayout.Button("Send Message to Opponent"))
        {
            networkManager.SendMessageToOpponent();
        }
    }
}
