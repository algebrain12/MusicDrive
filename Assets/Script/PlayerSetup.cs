using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayerSetup : MonoBehaviour
{
    public CarController carController;

    public NitroSystem nitroSystem;

    public new GameObject camera;

    public Behaviour[] localOnlyBehaviours;

    private void Awake()
    {
        SetLocalState(false);
    }
    
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
