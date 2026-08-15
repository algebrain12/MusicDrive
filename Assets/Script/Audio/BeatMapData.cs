using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "NewBeatMap", menuName = "MusicDrive/Beat Map")]
public class BeatMapData : ScriptableObject
{
    [Header("Song")]
    public AudioClip song;
    public string songName;
    public float bpm;

    public float minimumBeatSpacing;
    public float onsetSensitivity;

    [Header("Beat Timestamps (seconds)")]
    // Fill this array manually in the Inspector for now.
    // Each number = the time (in seconds) a beat happens in the song.
    public float[] beats;

    [Header("Timing Windows (milliseconds)")]
    public float perfectWindowMs = 50f;
    public float goodWindowMs    = 100f;
    public float okWindowMs      = 150f;
}