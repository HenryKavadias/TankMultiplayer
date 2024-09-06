using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HostGameManager : IDisposable
{
    private NetworkObject playerPrefab;

    private Allocation allocation;
    private string joinCode;
    private string lobbyId;
    public NetworkServer NetworkServer { get; private set; }

    private const int MAXCONNECTIONS = 20;
    private const string GAMESCENENAME = "Game";

    public HostGameManager(NetworkObject _playerPrefab)
    {
        playerPrefab = _playerPrefab;
    }

    public async Task StartHostAsync()
    {
        // get allocations
        try
        {
            allocation = await Relay.Instance.CreateAllocationAsync(MAXCONNECTIONS);
        }
        catch(Exception e) 
        {
            Debug.Log(e);   
            return;
        }

        // get join code
        try
        {
            joinCode = await Relay.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log(joinCode);
        }
        catch (Exception e)
        {
            Debug.Log(e);
            return;
        }

        // setup new scene as host
        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        RelayServerData relayServerData = new RelayServerData(allocation, "dtls");
        transport.SetRelayServerData(relayServerData);

        // Create a lobby
        try
        {
            CreateLobbyOptions lobbyOptions = new CreateLobbyOptions();
            lobbyOptions.IsPrivate = false; // Can be controlled with a UI button
            lobbyOptions.Data = new Dictionary<string, DataObject>()
            {
                {
                    "JoinCode", new DataObject(
                        visibility: DataObject.VisibilityOptions.Member, 
                        value: joinCode)
                }
            };
            // Could prevent making a lobby if the host player doesn't have a name
            string playerName = PlayerPrefs.GetString(NameSelector.PLAYERNAMEKEY, "Unknown");
            Lobby lobby = await Lobbies.Instance.CreateLobbyAsync(
                $"{playerName}'s Lobby", MAXCONNECTIONS, lobbyOptions);

            lobbyId = lobby.Id;

            HostSingleton.Instance.StartCoroutine(HeartbeatLobby(15));
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
            return;
        }

        NetworkServer = new NetworkServer(NetworkManager.Singleton, playerPrefab);

        UserData userData = new UserData()
        {
            userName = PlayerPrefs.GetString(NameSelector.PLAYERNAMEKEY, "Missing Name"),
            userAuthId = AuthenticationService.Instance.PlayerId
        };

        string payload = JsonUtility.ToJson(userData);
        byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);

        NetworkManager.Singleton.NetworkConfig.ConnectionData = payloadBytes;

        NetworkManager.Singleton.StartHost();

        NetworkServer.OnClientLeft += HandleClientLeft;

        NetworkManager.Singleton.SceneManager.LoadScene(GAMESCENENAME, LoadSceneMode.Single);
    }

    private IEnumerator HeartbeatLobby(float seconds)
    {
        WaitForSecondsRealtime delay = new WaitForSecondsRealtime(seconds);
        while (true) 
        {
            Lobbies.Instance.SendHeartbeatPingAsync(lobbyId);
            yield return delay;
        }
    }

    public void Dispose()
    {
        Shutdown();
    }

    public async void Shutdown()
    {
        if (string.IsNullOrEmpty(lobbyId)) { return; }

        HostSingleton.Instance.StopCoroutine(nameof(HeartbeatLobby));
        try
        {
            await Lobbies.Instance.DeleteLobbyAsync(lobbyId);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
        lobbyId = string.Empty;

        NetworkServer.OnClientLeft -= HandleClientLeft;

        NetworkServer?.Dispose();
    }

    private async void HandleClientLeft(string authId)
    {
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(lobbyId, authId);
        }
        catch (LobbyServiceException e) 
        {
            Debug.Log(e);
        }
    }
}
