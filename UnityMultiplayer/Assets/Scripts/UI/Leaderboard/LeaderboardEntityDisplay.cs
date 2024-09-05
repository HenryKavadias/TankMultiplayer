using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class LeaderboardEntityDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text displayText;
    [SerializeField] private Color myColor;

    private FixedString32Bytes playerName;
    public ulong ClientId { get; private set;}
    
    public int Coins { get; private set; }

    public void Initialise(ulong _clientId, FixedString32Bytes _playerName, int _coins)
    {
        ClientId = _clientId;
        playerName = _playerName;

        if (ClientId == NetworkManager.Singleton.LocalClientId)
        {
            displayText.color = myColor;
        }

        UpdateCoins(_coins);
    }

    public void UpdateCoins(int coins)
    {
        Coins = coins;
        UpdateText();
    }

    public void UpdateText()
    {
        displayText.text = $"{transform.GetSiblingIndex() + 1}. {playerName} - {Coins}";
    }
}
