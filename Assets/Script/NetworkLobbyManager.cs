using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NetworkLobbyManager : MonoBehaviourPunCallbacks
{
    public const string ReadyProperty = "Ready";
    public const string MapSeedProperty = "MapSeed";
    public const string RaceStartedProperty = "RaceStarted";

    [Header("Panels")]
    public GameObject nicknamePanel;
    public GameObject menuPanel;
    public GameObject createLobbyPanel;
    public GameObject joinLobbyPanel;
    public GameObject waitingRoomPanel;
    public GameObject connectingPanel;

    [Header("Nickname")]
    public TMP_InputField nicknameInput;

    [Header("Create Lobby")]
    public TMP_InputField lobbyNameInput;

    [Header("Join Lobby")]
    public Transform roomListContent;
    public LobbyRoomListItem roomItemPrefab;
    public TMP_Text emptyRoomsText;
    public float roomRefreshLabelInterval = 3f;

    [Header("Waiting Room")]
    public TMP_Text lobbyCodeText;
    public TMP_Text playerCountText;
    public TMP_Text readyCountText;
    public TMP_Text hostText;
    public TMP_Text statusText;
    public TMP_Text readyButtonText;
    public Button readyButton;
    public Transform playerListContent;
    public LobbyPlayerListItem playerItemPrefab;

    [Header("Race")]
    public string raceSceneName = "SampleScene";
    public bool loadRaceSceneWhenReady = true;

    private readonly Dictionary<string, RoomInfo> cachedRooms = new Dictionary<string, RoomInfo>();
    private readonly List<GameObject> spawnedRoomItems = new List<GameObject>();
    private readonly List<GameObject> spawnedPlayerItems = new List<GameObject>();

    private float refreshLabelTimer;
    private bool hasRequestedStart;

    private void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        ShowOnly(nicknamePanel);
    }

    private void Update()
    {
        if (joinLobbyPanel != null && joinLobbyPanel.activeSelf)
        {
            refreshLabelTimer -= Time.deltaTime;
            if (refreshLabelTimer <= 0f)
            {
                refreshLabelTimer = roomRefreshLabelInterval;
                RebuildRoomList();
            }
        }
    }

    public void ContinueFromNickname()
    {
        string nickname = nicknameInput != null ? nicknameInput.text.Trim() : string.Empty;
        if (string.IsNullOrWhiteSpace(nickname))
        {
            nickname = "Player" + Random.Range(1000, 9999);
        }

        PhotonNetwork.NickName = nickname;
        SetStatus("Connecting...");
        ShowOnly(connectingPanel);

        // FIX: Lock AppVersion so Editor and Standalone Builds share the same lobby
        PhotonNetwork.PhotonServerSettings.AppSettings.AppVersion = "1.0.0";

        if (PhotonNetwork.IsConnectedAndReady)
        {
            PhotonNetwork.JoinLobby();
        }
        else
        {
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public void OpenCreateLobbyPanel()
    {
        ShowOnly(createLobbyPanel);
    }

   public void OpenJoinLobbyPanel()
    {
        ShowOnly(joinLobbyPanel);

        // CRITICAL FIX: If we aren't currently in the lobby, join it now!
        // Joining the lobby forces Photon to immediately send the full room list via OnRoomListUpdate.
        if (!PhotonNetwork.InLobby)
        {
            SetStatus("Fetching rooms...");
            PhotonNetwork.JoinLobby(TypedLobby.Default);
        }
        else
        {
            RebuildRoomList();
        }
    }

    public void BackToMenu()
    {
        ShowOnly(menuPanel);
    }

    public void CreateLobby()
    {
        string lobbyName = lobbyNameInput != null ? lobbyNameInput.text.Trim() : string.Empty;
        if (string.IsNullOrWhiteSpace(lobbyName))
        {
            lobbyName = PhotonNetwork.NickName + "'s Lobby";
        }

        string roomCode = lobbyName + "-" + Random.Range(1000, 9999);
        int mapSeed = Random.Range(int.MinValue, int.MaxValue);

        // FIX 1: Create room properties hashtable explicitly
        Hashtable customRoomProps = new Hashtable
        {
            { MapSeedProperty, mapSeed },
            { RaceStartedProperty, false }
        };

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = 8,
            IsOpen = true,
            IsVisible = true,
            CustomRoomProperties = customRoomProps,
            // FIX 2: Ensure BOTH properties are exposed to the lobby server!
            CustomRoomPropertiesForLobby = new[] { MapSeedProperty, RaceStartedProperty }
        };

        SetStatus("Creating lobby...");
        // FIX 3: Pass TypedLobby.Default explicitly
        PhotonNetwork.CreateRoom(roomCode, options, TypedLobby.Default);
    }

    public void JoinRoom(string roomName)
    {
        if (string.IsNullOrWhiteSpace(roomName))
        {
            return;
        }

        SetStatus("Joining " + roomName + "...");
        PhotonNetwork.JoinRoom(roomName);
    }

    public void LeaveCurrentRoom()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
    }

    public void ToggleReady()
    {
        if (!PhotonNetwork.InRoom)
        {
            return;
        }

        bool isReady = IsPlayerReady(PhotonNetwork.LocalPlayer);
        SetLocalReady(!isReady);
    }

    public override void OnConnectedToMaster()
    {
        // Always join the lobby as soon as connected to master
        if (!PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby(TypedLobby.Default);
        }
        ShowOnly(menuPanel);
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("[Photon Debug] Successfully Joined Lobby! InLobby = " + PhotonNetwork.InLobby);
        SetStatus(string.Empty);
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        
        foreach (RoomInfo room in roomList)
        {
            Debug.Log(room.Name);
            if (room.RemovedFromList || !room.IsVisible || !room.IsOpen)
            {
                cachedRooms.Remove(room.Name);
            }
            else
            {
                cachedRooms[room.Name] = room;
            }
        }

        RebuildRoomList();
    }

    public override void OnCreatedRoom()
    {
        SetStatus("Lobby created.");
    }

    public override void OnJoinedRoom()
    {
        hasRequestedStart = false;
        SetLocalReady(false);
        ShowOnly(waitingRoomPanel);
        RebuildWaitingRoom();
    }

    public override void OnLeftRoom()
    {
        hasRequestedStart = false;
        ShowOnly(menuPanel);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        RebuildWaitingRoom();
        CheckReadyAndStart();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        RebuildWaitingRoom();
        CheckReadyAndStart();
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        RebuildWaitingRoom();
        CheckReadyAndStart();
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        SetStatus("Create failed: " + message);
        ShowOnly(createLobbyPanel);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        SetStatus("Join failed: " + message);
        ShowOnly(joinLobbyPanel);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        SetStatus("Disconnected: " + cause);
        ShowOnly(nicknamePanel);
    }

    private void SetLocalReady(bool ready)
    {
        PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable
        {
            { ReadyProperty, ready }
        });
    }

    private void CheckReadyAndStart()
    {
        if (!PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom || hasRequestedStart)
        {
            return;
        }

        int totalPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
        if (totalPlayers <= 0 || CountReadyPlayers() != totalPlayers)
        {
            return;
        }

        hasRequestedStart = true;
        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.CurrentRoom.IsVisible = false;
        PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable
        {
            { RaceStartedProperty, true }
        });

        if (loadRaceSceneWhenReady && !string.IsNullOrWhiteSpace(raceSceneName))
        {
            PhotonNetwork.LoadLevel(raceSceneName);
        }
        else
        {
            MultiplayerRaceManager raceManager = FindObjectOfType<MultiplayerRaceManager>();
            if (raceManager != null)
            {
                raceManager.StartRaceFromLobby();
            }
        }
    }

    private void RebuildRoomList()
    {
        Clear(spawnedRoomItems);

        foreach (RoomInfo room in cachedRooms.Values)
        {
            if (room.PlayerCount >= room.MaxPlayers)
            {
                continue;
            }

            if (room.CustomProperties.TryGetValue(RaceStartedProperty, out object started) && started is bool raceStarted && raceStarted)
            {
                continue;
            }

            LobbyRoomListItem item = Instantiate(roomItemPrefab, roomListContent);
            item.Setup(room, this);
            spawnedRoomItems.Add(item.gameObject);
        }

        if (emptyRoomsText != null)
        {
            emptyRoomsText.gameObject.SetActive(spawnedRoomItems.Count == 0);
            emptyRoomsText.text = spawnedRoomItems.Count == 0 ? "No lobbies found" : string.Empty;
        }
    }

    private void RebuildWaitingRoom()
    {
        if (!PhotonNetwork.InRoom)
        {
            return;
        }

        Clear(spawnedPlayerItems);

        Player[] players = PhotonNetwork.PlayerList;
        foreach (Player player in players)
        {
            LobbyPlayerListItem item = Instantiate(playerItemPrefab, playerListContent);
            item.Setup(player);
            spawnedPlayerItems.Add(item.gameObject);
        }

        int readyPlayers = CountReadyPlayers();

        if (lobbyCodeText != null)
        {
            lobbyCodeText.text = PhotonNetwork.CurrentRoom.Name;
        }

        if (playerCountText != null)
        {
            playerCountText.text = players.Length + "/8";
        }

        if (readyCountText != null)
        {
            readyCountText.text = readyPlayers + "/" + players.Length;
        }

        if (hostText != null)
        {
            hostText.text = PhotonNetwork.IsMasterClient ? "Host" : "Client";
        }

        bool localReady = IsPlayerReady(PhotonNetwork.LocalPlayer);
        if (readyButtonText != null)
        {
            readyButtonText.text = localReady ? "Unready" : "Ready";
        }

        if (readyButton != null)
        {
            readyButton.interactable = !hasRequestedStart;
        }
    }

    private int CountReadyPlayers()
    {
        int readyPlayers = 0;
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (IsPlayerReady(player))
            {
                readyPlayers++;
            }
        }

        return readyPlayers;
    }

    private static bool IsPlayerReady(Player player)
    {
        return player.CustomProperties.TryGetValue(ReadyProperty, out object value) && value is bool ready && ready;
    }

    private void ShowOnly(GameObject panel)
    {
        SetActive(nicknamePanel, panel == nicknamePanel);
        SetActive(menuPanel, panel == menuPanel);
        SetActive(createLobbyPanel, panel == createLobbyPanel);
        SetActive(joinLobbyPanel, panel == joinLobbyPanel);
        SetActive(waitingRoomPanel, panel == waitingRoomPanel);
        SetActive(connectingPanel, panel == connectingPanel);
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private static void Clear(List<GameObject> items)
    {
        foreach (GameObject item in items)
        {
            if (item != null)
            {
                Destroy(item);
            }
        }

        items.Clear();
    }
}
