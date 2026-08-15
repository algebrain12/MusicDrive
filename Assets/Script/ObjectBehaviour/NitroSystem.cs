using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class NitroSystem : MonoBehaviour {

    [Header("Nitro Duration")]
    // How long nitro stays alive after the last hit (seconds)
    // Each new hit refreshes this timer
    [SerializeField] private float nitroDurationSec = 1.5f;

    [Header("Bonus Torque Per Layer")]
    // Added on top of CarController's base motorTorque.
    // Tune these relative to your base motorTorque (currently 300).
    // Layer 0 = no bonus. Layers 1-5 add increasing torque.
    [SerializeField] private float[] nitroForce = new float[6] {
        0f,      // Layer 0 — off
        150f,    // Layer 1 — +50% of base
        300f,    // Layer 2 — +100%
        500f,    // Layer 3
        750f,    // Layer 4
        1100f    // Layer 5 — max
    };

    [Header("Speed Cap Per Layer (km/h)")]
    [SerializeField] private float[] speedCapKph = new float[6] {
        90f,
        115f,
        140f,
        165f,
        190f,
        220f
    };

    // ── State ──────────────────────────────────────────────────
    private int   _activeLayer = 0;
    private float _nitroTimer  = 0f;
    private bool  _nitroActive = false;

    // ── Public read (CarController reads these every LateUpdate) ──
    public float ActiveForce    => _nitroActive ? nitroForce[_activeLayer] : 0f;
    public float ActiveSpeedCapKph => GetSpeedCapKph(_activeLayer);
    public int   ActiveLayer    => _activeLayer;
    public float NitroFraction  => _nitroActive ? (_nitroTimer / nitroDurationSec) : 0f;

    public float GetSpeedCapKph(int layer) {
        if (speedCapKph == null || speedCapKph.Length == 0) return 90f;
        int index = Mathf.Clamp(layer, 0, speedCapKph.Length - 1);
        return Mathf.Max(1f, speedCapKph[index]);
    }

    // ── Event wiring ───────────────────────────────────────────
    private void OnEnable()  { GameEvents.OnComboChanged += HandleComboChanged; }
    private void OnDisable() { GameEvents.OnComboChanged -= HandleComboChanged; }

    // ── Update: tick nitro timer ────────────────────────────────
    private void Update() {
        if (!_nitroActive) return;

        _nitroTimer -= Time.deltaTime;

        // Broadcast for HUD bar fill
        GameEvents.OnNitroTick?.Invoke(NitroFraction);

        if (_nitroTimer <= 0f) {
            DeactivateNitro();
        }
    }

    // ── Combo handler ──────────────────────────────────────────
    private void HandleComboChanged(int newCombo) {
        if (newCombo == 0) {
            DeactivateNitro();
            return;
        }

        // Clamp: combo 6, 7, 8... all stay at layer 5
        int newLayer = Mathf.Clamp(newCombo, 1, 5);

        _activeLayer = newLayer;
        _nitroTimer  = nitroDurationSec;   // refresh timer on every hit
        _nitroActive = true;

        GameEvents.OnNitroLayerChanged?.Invoke(_activeLayer);
    }

    private void DeactivateNitro() {
        _activeLayer = 0;
        _nitroTimer  = 0f;
        _nitroActive = false;

        GameEvents.OnNitroLayerChanged?.Invoke(0);
        GameEvents.OnNitroTick?.Invoke(0f);
    }
}