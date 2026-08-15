using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComboSystem : MonoBehaviour
{
    [Header("Combo Settings")]
    public float comboLifeMs = 65000000f;

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

    void Start()
    {
        OnEnable();
    }

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
        _lifeTimer = comboLifeMs / 1000f;
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