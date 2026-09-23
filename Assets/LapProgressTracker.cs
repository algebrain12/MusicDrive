using ExitGames.Client.Photon;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Splines;

// Put this on the player car root (same GameObject as PhotonView / RealCarController).
// Reports a win using the SAME room properties RaceFinishLine used
// (WinnerActorProperty / WinnerNameProperty), so RaceResultManager needs
// no changes — this script replaces RaceFinishLine as the win trigger.
[RequireComponent(typeof(PhotonView))]
public class LapProgressTracker : MonoBehaviour
{
    public const string WinnerActorProperty = "WinnerActor";
    public const string WinnerNameProperty = "WinnerName";

    [Header("References")]
    [Tooltip("Optional. The generator that built this track. Leave empty if the " +
             "prefab can't reference a scene object — it will be found automatically " +
             "at runtime via FindObjectOfType.")]
    public RoadMeshGenerator roadMeshGenerator;

    [Header("Lap Settings")]
    [Tooltip("Laps required to win the race.")]
    public int lapsToWin = 1;

    [Tooltip("Fraction of the lap (0-1) the car must travel forward past the start line before a crossing counts as a completed lap. Prevents tracking noise from counting a lap early.")]
    [Range(0.05f, 0.95f)]
    public float minForwardProgressForLap = 0.5f;

    [Tooltip("Minimum real seconds that must pass before a lap (including the first) can be counted. A last-resort guard against any single-frame tracking glitch.")]
    public float minSecondsBeforeLap = 3f;

    [Header("Tracking")]
    [Tooltip("How far, as a 0-1 fraction of the whole lap, to search around the car's last known spline position each frame. Keep this small so the search can never jump across the start/finish seam by mistake — but large enough to cover how far the car can travel between two frames at top speed.")]
    [Range(0.005f, 0.2f)]
    public float trackSearchWindow = 0.05f;

    [Tooltip("Points sampled inside the search window each frame. Higher = smoother tracking, more expensive.")]
    public int trackSearchSamples = 12;

    private PhotonView view;
    private SplineContainer spline;
    private float startT;
    private float lastT;
    private float bestForwardProgress;
    private int lapsCompleted;
    private float lastLapTimestamp;
    private bool hasWon;

    private void Awake()
    {
        view = GetComponent<PhotonView>();
    }

    private void Start()
    {
        // The prefab is instantiated at runtime, so it can't hold a
        // reference to a scene object set in the Project asset — find the
        // one live instance in the scene instead.
        if (roadMeshGenerator == null)
        {
            roadMeshGenerator = FindObjectOfType<RoadMeshGenerator>();
        }

        if (roadMeshGenerator == null || roadMeshGenerator.splineContainer == null)
        {
            Debug.LogWarning("LapProgressTracker: no RoadMeshGenerator/spline found in the scene, disabling.", this);
            enabled = false;
            return;
        }

        spline = roadMeshGenerator.splineContainer;
        startT = Mathf.Clamp01((roadMeshGenerator.samplepoint % roadMeshGenerator.samples) / (float)roadMeshGenerator.samples);
        // The car spawns on the start line, so seed tracking there directly
        // rather than doing a first-frame global search (which is exactly
        // where the seam ambiguity bites hardest).
        lastT = startT;
        lastLapTimestamp = Time.time;
    }

    private void Update()
    {
        // Only the owning client needs to track its own car's laps —
        // every other client finds out who won via the room property.
        if (!view.IsMine || hasWon || spline == null)
        {
            return;
        }

        float t = FindNearestT(transform.position, lastT, trackSearchWindow);

        // Progress measured from the start line, always increasing as the
        // car goes forward around the loop (0 at the line, up toward 1
        // just before completing the lap).
        float progressSinceStart = Mathf.Repeat(t - startT, 1f);
        bestForwardProgress = Mathf.Max(bestForwardProgress, progressSinceStart);

        // A lap completes when that forward-progress value wraps back down,
        // i.e. the car just crossed the start line again.
        bool crossedStartLine = Mathf.Repeat(lastT - startT, 1f) > progressSinceStart;
        bool enoughTimePassed = Time.time - lastLapTimestamp >= minSecondsBeforeLap;

        if (crossedStartLine && bestForwardProgress >= minForwardProgressForLap && enoughTimePassed)
        {
            lapsCompleted++;
            bestForwardProgress = 0f;
            lastLapTimestamp = Time.time;

            if (lapsCompleted >= lapsToWin)
            {
                ReportWin();
            }
        }

        lastT = t;
    }

    // Searches only a small window around `searchCenter` instead of the
    // whole spline. This is what prevents the start/finish seam (where
    // t = 0 and t = 1 are the same physical point on a closed loop) from
    // ever being ambiguous: the car can only reach the far side of the
    // window by actually having traveled there.
    private float FindNearestT(Vector3 worldPosition, float searchCenter, float windowHalfWidth)
    {
        float bestT = searchCenter;
        float bestSqrDist = float.MaxValue;

        for (int i = 0; i <= trackSearchSamples; i++)
        {
            float offset = Mathf.Lerp(-windowHalfWidth, windowHalfWidth, i / (float)trackSearchSamples);
            float candidateT = Mathf.Repeat(searchCenter + offset, 1f);
            Vector3 candidatePos = (Vector3)spline.EvaluatePosition(candidateT);
            float sqrDist = (candidatePos - worldPosition).sqrMagnitude;

            if (sqrDist < bestSqrDist)
            {
                bestSqrDist = sqrDist;
                bestT = candidateT;
            }
        }

        return bestT;
    }

    private void ReportWin()
    {
        hasWon = true;

        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
        {
            return;
        }

        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(WinnerActorProperty))
        {
            return;
        }

        Hashtable winnerProps = new Hashtable
        {
            { WinnerActorProperty, PhotonNetwork.LocalPlayer.ActorNumber },
            { WinnerNameProperty, string.IsNullOrWhiteSpace(PhotonNetwork.LocalPlayer.NickName)
                ? "Player"
                : PhotonNetwork.LocalPlayer.NickName }
        };

        // Compare-and-set: only succeeds if nobody has won yet, so two cars
        // finishing within the same frame can't both be declared the winner.
        Hashtable expected = new Hashtable
        {
            { WinnerActorProperty, null }
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(winnerProps, expected);
    }
}