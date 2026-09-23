using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameInput : MonoBehaviour {
    public static GameInput Instance { get; private set; }
    public bool IsPaused = false;

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
        if(IsPaused)return Vector2.zero;
        return playerInputActions.PlayerActions.Movement.ReadValue<Vector2>();
    }
}