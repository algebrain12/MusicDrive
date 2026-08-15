using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameHUD : MonoBehaviour
{
    [Header("Beat Ring (optional visual feedback)")]
    public Image beatRing;
    public float ringFlashDuration = 0.1f;
    public Color ringBaseColor = new Color(1, 1, 1, 0.2f);
    public Color ringFlashColor = Color.white;

    [Header("Combo")]
    public TextMeshProUGUI comboText;

    [Header("Hit Result")]
    public TextMeshProUGUI hitResultText;
    public float hitResultFadeDuration = 0.6f;

    [Header("Driving")]
    public RealCarController carController;
    public TextMeshProUGUI speedText;

    private float _ringTimer;
    private float _hitResultTimer;

    void OnEnable()
    {
        GameEvents.OnBeat += HandleBeat;
        GameEvents.OnHit += HandleHit;
        GameEvents.OnMiss += HandleMiss;
        GameEvents.OnComboChanged += HandleComboChanged;
    }

    void OnDisable()
    {
        GameEvents.OnBeat -= HandleBeat;
        GameEvents.OnHit -= HandleHit;
        GameEvents.OnMiss -= HandleMiss;
        GameEvents.OnComboChanged -= HandleComboChanged;
    }

    void Update()
    {
        if (carController == null)
            carController = FindObjectOfType<RealCarController>();

        if (speedText != null && carController != null)
        {
            speedText.text = $"{carController.CurrentSpeedKph:000} km/h";
        }

        if (_ringTimer > 0f)
        {
            _ringTimer -= Time.deltaTime;
            float t = _ringTimer / ringFlashDuration;

            if (beatRing != null)
            {
                beatRing.color = Color.Lerp(ringBaseColor, ringFlashColor, t);
            }
        }

        if (_hitResultTimer > 0f)
        {
            _hitResultTimer -= Time.deltaTime;
            float alpha = _hitResultTimer / hitResultFadeDuration;

            if (hitResultText != null)
            {
                Color c = hitResultText.color;
                hitResultText.color = new Color(c.r, c.g, c.b, alpha);
            }
        }
    }

    void HandleBeat(float beatTime)
    {
        _ringTimer = ringFlashDuration;

        if (beatRing != null)
        {
            beatRing.color = ringFlashColor;
        }
    }

    void HandleHit(HitRes result, float errorMs)
    {
        (string text, Color color) = result switch
        {
            HitRes.Perfect => ("PERFECT!", Color.yellow),
            HitRes.Good    => ("GOOD", Color.green),
            HitRes.Ok      => ("OK", Color.white),
            _                 => ("", Color.clear)
        };

        if (hitResultText != null)
        {
            hitResultText.text = text;
            hitResultText.color = color;
        }

        _hitResultTimer = hitResultFadeDuration;
    }

    void HandleMiss()
    {
        if (hitResultText != null)
        {
            hitResultText.text = "MISS";
            hitResultText.color = Color.red;
        }

        _hitResultTimer = hitResultFadeDuration;
    }

    void HandleComboChanged(int combo)
    {
        if (comboText != null)
        {
            comboText.text = combo > 1 ? $"x{combo} COMBO" : "";
        }
    }
}
