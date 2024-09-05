using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkClient : IDisposable
{
    private NetworkManager networkManager;

    private const string MENUSCENENAME = "MENU";
    
    public NetworkClient(NetworkManager _networkManager)
    {
        networkManager = _networkManager;

        networkManager.OnClientDisconnectCallback += OnClientDisconnect;
    }

    private void OnClientDisconnect(ulong clientId)
    {
        if (clientId != 0 && clientId != networkManager.LocalClientId) { return; }

        Disconnect();
    }
    public void Disconnect()
    {
        if (SceneManager.GetActiveScene().name != MENUSCENENAME)
        {
            SceneManager.LoadScene(MENUSCENENAME);
        }

        if (networkManager.IsConnectedClient)
        {
            networkManager.Shutdown();
        }
    }
    public void Dispose()
    {
        if (networkManager == null) { return; }
        networkManager.OnClientDisconnectCallback -= OnClientDisconnect;
    }
}
