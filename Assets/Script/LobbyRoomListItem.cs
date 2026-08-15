using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyRoomListItem : MonoBehaviour
{
    public TMP_Text roomNameText;
    public TMP_Text playerCountText;
    public Button joinButton;

    private string roomName;
    private NetworkLobbyManager lobbyManager;

    public void Setup(RoomInfo roomInfo, NetworkLobbyManager manager)
    {
        roomName = roomInfo.Name;
        lobbyManager = manager;

        if (roomNameText != null)
        {
            roomNameText.text = roomInfo.Name;
        }

        if (playerCountText != null)
        {
            playerCountText.text = roomInfo.PlayerCount + "/" + roomInfo.MaxPlayers;
        }

        if (joinButton != null)
        {
            joinButton.onClick.RemoveAllListeners();
            joinButton.onClick.AddListener(JoinRoom);
        }
    }

    private void JoinRoom()
    {
        if (lobbyManager != null)
        {
            lobbyManager.JoinRoom(roomName);
        }
    }
}
