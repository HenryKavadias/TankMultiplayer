using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies.Models;
using Unity.Services.Lobbies;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    // prevents dedicated server support (for server billing reasons)
    [SerializeField] private bool disableMatchmaking = false;

    [SerializeField] private TMP_Text queueStatusText;
    [SerializeField] private TMP_Text queueTimerText;
    [SerializeField] private TMP_Text findMatchButtonText;
    [SerializeField] private TMP_InputField joinCodeField;

    private bool isMatchmaking;
    private bool isCancelling;
    private bool isBusy;
    private float timeInQueue = 0.0f;

    private void Start()
    {
        if (ClientSingleton.Instance == null) { return; }

        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

        queueStatusText.text = string.Empty;
        queueTimerText.text = string.Empty;

        if (disableMatchmaking) 
        {
            findMatchButtonText.text = "(disabled)";
        }
    }

    private void Update()
    {
        if (isMatchmaking)
        {
            timeInQueue += Time.deltaTime;
            TimeSpan ts = TimeSpan.FromSeconds(timeInQueue);
            queueTimerText.text = string.Format("{0:00}:{1:00}", ts.Minutes, ts.Seconds);
        }
    }

    public async void FindMatchPressed()
    {
        if (isCancelling || disableMatchmaking) { return; }

        if (isMatchmaking) 
        {
            queueStatusText.text = "Cancelling...";
            isCancelling = true;
            // Cancel matchmaking
            await ClientSingleton.Instance.GameManager.CancelMatchmaking();
            isCancelling = false;
            isMatchmaking = false;
            isBusy = false;
            findMatchButtonText.text = "Find Match";
            queueStatusText.text = string.Empty;
            queueTimerText.text = string.Empty;
            return; 
        }

        if (isBusy) { return; }

        // Start queue
        ClientSingleton.Instance.GameManager.MatchmakeAsync(OnMatchMade);
        findMatchButtonText.text = "Cancel";
        queueStatusText.text = "Searching...";
        timeInQueue = 0f;
        isMatchmaking = true;
    }

    private void OnMatchMade(MatchmakerPollingResult result)
    {
        switch (result)
        {
            case MatchmakerPollingResult.Success:
                queueStatusText.text = "Connecting...";
                break;
            case MatchmakerPollingResult.TicketCreationError:
                queueStatusText.text = "TicketCreationError";
                break;
            case MatchmakerPollingResult.TicketCancellationError:
                queueStatusText.text = "TicketCancellationError";
                break;
            case MatchmakerPollingResult.TicketRetrievalError:
                queueStatusText.text = "TicketRetrievalError";
                break;
            case MatchmakerPollingResult.MatchAssignmentError:
                queueStatusText.text = "MatchAssignmentError";
                break;
            default:
                queueStatusText.text = "UnknownError";
                break;
        }
    }

    public async void StartHost()
    {
        if (isBusy) { return; }
        isBusy = true;
        await HostSingleton.Instance.GameManager.StartHostAsync();
        isBusy = false;
    }
    public async void StartClient()
    {
        if (isBusy) { return; }
        isBusy = true;
        await ClientSingleton.Instance.GameManager.StartClientAsync(joinCodeField.text);
        isBusy = false;
    }
    public async void JoinAsync(Lobby lobby)
    {
        if (isBusy) { return; }
        isBusy = true;

        try
        {
            Lobby joiningLobby = await Lobbies.Instance.JoinLobbyByIdAsync(lobby.Id);
            string joinCode = joiningLobby.Data["JoinCode"].Value;

            await ClientSingleton.Instance.GameManager.StartClientAsync(joinCode);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }

        isBusy = false;
    }
}
