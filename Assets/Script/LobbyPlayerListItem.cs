using Photon.Realtime;
using TMPro;
using UnityEngine;

public class LobbyPlayerListItem : MonoBehaviour
{
    public TMP_Text playerNameText;
    public TMP_Text readyText;
    public TMP_Text hostText;

    public void Setup(Player player)
    {
        if (playerNameText != null)
        {
            playerNameText.text = string.IsNullOrWhiteSpace(player.NickName) ? "Unnamed" : player.NickName;
        }

        bool ready = player.CustomProperties.TryGetValue(NetworkLobbyManager.ReadyProperty, out object value)
            && value is bool isReady
            && isReady;

        if (readyText != null)
        {
            readyText.text = ready ? "Ready" : "Not Ready";
        }

        if (hostText != null)
        {
            hostText.text = player.IsMasterClient ? "Host" : string.Empty;
        }
    }
}
