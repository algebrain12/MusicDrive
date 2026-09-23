using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComboSystem : MonoBehaviour
{
    [Header("Combo Settings")]
    public float comboLifeMs = 3f;

    private int   _combo = 0;
    private float _lifeTimer = 0f;
    private bool  _comboIsActive = false;

    public int Combo => _combo;

    void OnEnable()
    {
        GameEvents.OnHit  += HandleHit;
        GameEvents.OnMiss += HandleMiss;
    }
    void OnDisable()
    {
        GameEvents.OnHit  -= HandleHit;
        GameEvents.OnMiss -= HandleMiss;
    }

    // CHANGED (fix): removed the manual OnEnable() call that used to live
    // here. Unity already invokes OnEnable() once automatically before
    // Start() runs, so calling it again subscribed HandleHit/HandleMiss
    // twice — every hit was bumping the combo by 2 instead of 1, which
    // cascaded into NitroSystem picking the wrong layer and everything
    // downstream (torque, speed cap, FOV) reacting incorrectly.

    void Update()
    {
        if (!_comboIsActive) return;

        _lifeTimer -= Time.deltaTime;

        if (_lifeTimer <= 0f)
        {
            ResetCombo();
        }
    }

    void HandleHit(HitRes result, float errorMs)
    {
        _combo++;
        _lifeTimer = comboLifeMs;
        _comboIsActive = true;

        GameEvents.OnComboChanged?.Invoke(_combo);
    }

    void HandleMiss()
    {
        ResetCombo();
    }

    void ResetCombo()
    {
        _combo = 0;
        _lifeTimer = 0f;
        _comboIsActive = false;

        GameEvents.OnComboChanged?.Invoke(0);
    }
}