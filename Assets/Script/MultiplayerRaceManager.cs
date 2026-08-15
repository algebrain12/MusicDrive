using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class MultiplayerRaceManager : MonoBehaviour
{
    public RandomTrackGenerator trackGenerator;
    public RoadMeshGenerator roadMeshGenerator;
    public GameObject playerPrefab;
    public bool startAutomatically = true;

    private bool hasStarted;

    private void Start()
    {
        if (startAutomatically && PhotonNetwork.InRoom)
        {
            StartRaceFromLobby();
        }
    }

    public void StartRaceFromLobby()
    {
        if (hasStarted || !PhotonNetwork.InRoom)
        {
            return;
        }

        hasStarted = true;

        int seed = GetMapSeed();
        if (trackGenerator != null)
        {
            trackGenerator.gameObject.SetActive(true);
            trackGenerator.generateOnStart = false;
            trackGenerator.GenerateTrack(seed);
        }

        SpawnLocalPlayer();
    }

    private void SpawnLocalPlayer()
    {
        if (playerPrefab == null || roadMeshGenerator == null)
        {
            Debug.LogError("MultiplayerRaceManager is missing playerPrefab or roadMeshGenerator.");
            return;
        }

        int playerIndex = GetLocalPlayerIndex();
        roadMeshGenerator.GetSpawnPose(playerIndex, out Vector3 spawnPosition, out Quaternion spawnRotation);

        GameObject player = PhotonNetwork.Instantiate(playerPrefab.name, spawnPosition, spawnRotation);
        PlayerSetup setup = player.GetComponent<PlayerSetup>();
        if (setup != null)
        {
            setup.IsLocalPlayer();
        }
    }

    private int GetMapSeed()
    {
        Hashtable roomProperties = PhotonNetwork.CurrentRoom.CustomProperties;
        if (roomProperties.TryGetValue(NetworkLobbyManager.MapSeedProperty, out object seedValue) && seedValue is int seed)
        {
            return seed;
        }

        return 0;
    }

    private int GetLocalPlayerIndex()
    {
        Player[] players = PhotonNetwork.PlayerList;
        System.Array.Sort(players, (a, b) => a.ActorNumber.CompareTo(b.ActorNumber));

        for (int i = 0; i < players.Length; i++)
        {
            if (players[i].ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
            {
                return i;
            }
        }

        return 0;
    }
}
