using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour {
    [SerializeField, Range(0.1f, 100f)] private float moveSmoothness;
    [SerializeField, Range(0.1f, 100f)] private float rotSmoothness;

    [SerializeField] private Vector3 moveOffset;
    [SerializeField] private Vector3 rotOffset;

    [SerializeField] private Transform carTarget;

    [Header("Combo FOV Effect")]
    [Tooltip("Leave empty to auto-grab a Camera component on this same GameObject.")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float baseFov = 60f;
    [SerializeField] private float maxFov = 80f;
    [Tooltip("Combo value at which FOV reaches maxFov. Higher combos are clamped to maxFov.")]
    [SerializeField] private int comboForMaxFov = 10;
    [SerializeField, Range(0.1f, 20f)] private float fovSmoothness = 4f;

    private int currentCombo;

    private void Awake() {
        if (targetCamera == null) {
            targetCamera = GetComponent<Camera>();
        }

        // Start from whatever FOV is set in the Camera component/inspector,
        // so baseFov doesn't have to be kept in sync by hand.
        if (targetCamera != null) {
            baseFov = targetCamera.fieldOfView;
        }
    }

    private void OnEnable() {
        // Only the local player's camera is active at a time (PlayerSetup
        // disables remote cameras), so this only ever reacts to the local
        // player's own combo — no ownership check needed here.
        GameEvents.OnComboChanged += HandleComboChanged;
    }

    private void OnDisable() {
        GameEvents.OnComboChanged -= HandleComboChanged;
    }

    private void HandleComboChanged(int newCombo) {
        currentCombo = newCombo;
    }

    private void FixedUpdate() {
        FollowTarget();
    }

    private void Update() {
        UpdateFov();
    }

    private void FollowTarget() {
        HandleMovement();
        HandleRotation();
    }

    private void HandleMovement() {
        Vector3 targetPos = carTarget.TransformPoint(moveOffset);

        transform.position = Vector3.Lerp(transform.position, targetPos, moveSmoothness * Time.deltaTime);
    }

    private void HandleRotation() {
        Vector3 direction = carTarget.position - transform.position;
        Quaternion rotation = Quaternion.LookRotation(direction + rotOffset, Vector3.up);

        transform.rotation = Quaternion.Lerp(transform.rotation, rotation, rotSmoothness * Time.deltaTime);
    }

    // CHANGED: new — smoothly widens the FOV as combo climbs, and eases it
    // back to baseFov as combo drops (e.g. resets to 0 on a miss/timeout).
    private void UpdateFov() {
        if (targetCamera == null) {
            return;
        }

        float t = comboForMaxFov > 0 ? Mathf.Clamp01((float)currentCombo / comboForMaxFov) : 0f;
        float target = Mathf.Lerp(baseFov, maxFov, t);

        targetCamera.fieldOfView = Mathf.Lerp(targetCamera.fieldOfView, target, fovSmoothness * Time.deltaTime);
    }
}