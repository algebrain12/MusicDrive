using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameInput : MonoBehaviour {
    public static GameInput Instance { get; private set; }

    private MovementControls playerInputActions;

    private void Awake() {
        Instance = this;

        playerInputActions = new MovementControls();
        playerInputActions.PlayerActions.Enable();
    }

    private void OnDestroy() {
        playerInputActions.Dispose();
    }

    public Vector2 getMoveDir()
    {
        return playerInputActions.PlayerActions.Movement.ReadValue<Vector2>();
    }
}