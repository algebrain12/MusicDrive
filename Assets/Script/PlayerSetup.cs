using UnityEngine;
using Photon.Pun;

// CHANGED: now extends MonoBehaviourPun so we get the `photonView` shortcut.
public class PlayerSetup : MonoBehaviourPun
{
    public RealCarController carController;

    public NitroSystem nitroSystem;

    public new GameObject camera;

    public Behaviour[] localOnlyBehaviours;

    // FIX: locality used to be decided only by whoever remembered to call
    // IsLocalPlayer() (MultiplayerRaceManager.SpawnLocalPlayer, right after
    // PhotonNetwork.Instantiate). If that call ever ran late, got skipped,
    // or this object got re-used/re-spawned, the enable/disable state could
    // end up on the wrong instance — which is exactly the symptom of "my
    // input drives someone else's car". photonView.IsMine is Photon's own,
    // always-correct ownership flag, so we use that directly instead.
    private void Start()
    {
        SetLocalState(photonView.IsMine);
    }

    // Kept for backwards compatibility / explicit calls elsewhere; it's now
    // just an alias for "yes, this is mine", and is safe to leave in or
    // remove — Start() above already sets the correct state on its own.
    public void IsLocalPlayer()
    {
        SetLocalState(true);
    }

    private void SetLocalState(bool isLocal)
    {
        if (carController != null)
        {
            carController.enabled = isLocal;
        }

        if (nitroSystem != null)
        {
            nitroSystem.enabled = isLocal;
        }

        if (camera != null)
        {
            camera.SetActive(isLocal);
        }

        if (localOnlyBehaviours == null)
        {
            return;
        }

        foreach (Behaviour behaviour in localOnlyBehaviours)
        {
            if (behaviour != null)
            {
                behaviour.enabled = isLocal;
            }
        }
    }
}