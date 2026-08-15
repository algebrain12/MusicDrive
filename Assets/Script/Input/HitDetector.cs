using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitDetector : MonoBehaviour
{
    [Header("References")]
    public BeatClock   beatClock;
    public BeatMapData beatMap;

    private int _lastScoredBeatIndex = -1;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            OnSpacebarPressed();
        }
    }

    void OnSpacebarPressed()
    {
        // CRITICAL: use dspTime, not Time.time — dspTime is the audio
        // engine's clock and stays perfectly in sync with the music.
        double pressTime = AudioSettings.dspTime;

        double songTimePressTime = pressTime - beatClock.SongStartDsp;
        int nearestIdx = FindNearestBeatIndex(songTimePressTime);

        if (nearestIdx < 0) return;
        if (nearestIdx == _lastScoredBeatIndex) return;

        double nearestBeatDsp = beatClock.BeatToDspTime(beatMap.beats[nearestIdx]);
        float errorMs = Mathf.Abs((float)(pressTime - nearestBeatDsp)) * 1000f;

        if (errorMs <= beatMap.perfectWindowMs)
        {
            GameEvents.OnHit?.Invoke(HitRes.Perfect, errorMs);
            _lastScoredBeatIndex = nearestIdx;
        }
        else if (errorMs <= beatMap.goodWindowMs)
        {
            GameEvents.OnHit?.Invoke(HitRes.Good, errorMs);
            _lastScoredBeatIndex = nearestIdx;
        }
        else if (errorMs <= beatMap.okWindowMs)
        {
            GameEvents.OnHit?.Invoke(HitRes.Ok, errorMs);
            _lastScoredBeatIndex = nearestIdx;
        }
        else
        {
            GameEvents.OnMiss?.Invoke();
        }
    }

    int FindNearestBeatIndex(double songTime)
    {
        if (beatMap.beats == null || beatMap.beats.Length == 0) return -1;

        int nearestIdx = 0;
        float nearestDist = float.MaxValue;

        for (int i = 0; i < beatMap.beats.Length; i++)
        {
            float dist = Mathf.Abs((float)songTime - beatMap.beats[i]);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearestIdx = i;
            }
            // Beats are sorted — once distance starts growing, we've passed the nearest one
            else if (dist > nearestDist) break;
        }
        return nearestIdx;
    }
}