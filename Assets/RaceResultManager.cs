using ExitGames.Client.Photon;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Drop this on any GameObject in the race scene (e.g. alongside
// MultiplayerRaceManager). Wire up winPanel / winnerNameText /
// returnToLobbyButton in the inspector, and set lobbySceneName to
// whatever your lobby scene is actually called in Build Settings.
public class RaceResultManager : MonoBehaviourPunCallbacks
{
    [Header("Win Panel")]
    public GameObject winPanel;
    public TMP_Text winnerNameText;
    public Button returnToLobbyButton;

    [Header("Scene")]
    public string lobbySceneName = "MainMenu";

    private bool isReturningToLobby;

    private void Awake()
    {
        if (winPanel != null)
        {
            winPanel.SetActive(false);
        }

        if (returnToLobbyButton != null)
        {
            returnToLobbyButton.onClick.AddListener(ReturnToLobby);
        }
    }

    private void Start()
    {
        // Covers a late joiner / reconnect landing in the scene after the
        // race has already finished — the property is already set, so no
        // OnRoomPropertiesUpdate callback will fire for us to catch it.
        if (PhotonNetwork.InRoom &&
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(LapProgressTracker.WinnerNameProperty, out object existingName))
        {
            ShowWinner(existingName as string);
        }
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.TryGetValue(LapProgressTracker.WinnerNameProperty, out object winnerName))
        {
            ShowWinner(winnerName as string);
        }
    }

    private void ShowWinner(string winnerNickname)
    {
        if (winPanel != null)
        {
            winPanel.SetActive(true);
        }

        if (winnerNameText != null)
        {
            winnerNameText.text = string.IsNullOrWhiteSpace(winnerNickname)
                ? "A player wins!"
                : winnerNickname + " wins!";
        }
    }

    public void ReturnToLobby()
    {
        if (isReturningToLobby)
        {
            return;
        }

        isReturningToLobby = true;

        if (returnToLobbyButton != null)
        {
            returnToLobbyButton.interactable = false;
        }

        if (PhotonNetwork.InRoom)
        {
            // OnLeftRoom below loads the lobby scene once this completes.
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            SceneManager.LoadScene(lobbySceneName);
        }
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene(lobbySceneName);
    }
}