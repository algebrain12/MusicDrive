using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeatClock : MonoBehaviour
{
    [Header("References")]
    public BeatMapData beatMap;       
    public AudioSource audioSource;   
    [Range(0.5f, 3f)] public float beatApproachTime = 1.5f;

    // ── Private state ────────────────────────────────────────
    private int    _nextBeatIndex = 0;  
    private int    _nextApproachIndex = 0;
    private double _songStartDsp;        
    private bool   _isPlaying    = false;

    private float currDelay = 0f;

    

  
    public double SongTime => AudioSettings.dspTime - _songStartDsp;
    public double SongStartDsp => _songStartDsp;

    // ── Lifecycle ────────────────────────────────────────────
    IEnumerator Start()
    {
        if (beatMap != null && beatMap.song != null &&
            (beatMap.beats == null || beatMap.beats.Length == 0))
        {
            yield return GenerateBeatMap();
        }

        //StartSong();
    }

    IEnumerator GenerateBeatMap()
    {
        AudioClip clip = beatMap.song;
        if (clip.loadState == AudioDataLoadState.Unloaded)
        {
            clip.LoadAudioData();
        }

        while (clip.loadState == AudioDataLoadState.Loading)
        {
            yield return null;
        }

        if (clip.loadState != AudioDataLoadState.Loaded)
        {
            Debug.LogError($"Could not load audio data for beat detection: {clip.name}", this);
            yield break;
        }

        int channels = clip.channels;
        float[] samples = new float[clip.samples * channels];
        if (!clip.GetData(samples, 0))
        {
            Debug.LogError($"Could not read audio samples for beat detection: {clip.name}", this);
            yield break;
        }

        const int windowSize = 1024;
        const int historySize = 24;
        int windowCount = samples.Length / (windowSize * channels);
        int fftSize = windowSize; 
        float[] windowSamples = new float[fftSize];
        System.Numerics.Complex[] complexSamples = new System.Numerics.Complex[fftSize];
        float[] energy = new float[windowCount];

        for (int window = 0; window < windowCount; window++)
        {
            int start = window * windowSize * channels;
            float sum = 0f;
            for (int sample = 0; sample < windowSize; sample++)
            {
                float mono = 0f;
                for (int channel = 0; channel < channels; channel++)
                {
                    mono += samples[start + sample * channels + channel];
                }

                mono /= channels;
                sum += mono * mono;
            }

            energy[window] = sum / windowSize;
        }

        List<float> detectedBeats = new List<float>();
        float minimumSpacing = Mathf.Max(0.1f, beatMap.minimumBeatSpacing);
        float lastBeatTime = -minimumSpacing;
        int[] prevTopBins = new int[5];

        for (int window = historySize; window < windowCount - 1; window++)
        {
            float average = 0f;
            for (int history = window - historySize; history < window; history++)
            {
                average += energy[history];
            }
            average /= historySize;

            bool isPeak = energy[window] > energy[window - 1] &&
                          energy[window] >= energy[window + 1];
            bool isOnset = energy[window] > average * beatMap.onsetSensitivity;
            bool isNewFreq = false;
        
            int startSample = window * windowSize * channels;
            for (int i = 0; i < fftSize; i++)
            {
                float mono = 0f;
                for (int c = 0; c < channels; c++)
                {
                    mono += samples[startSample + i * channels + c];
                }
                windowSamples[i] = mono / channels;
            }

            FFT(windowSamples, complexSamples);

            var currentTopBins = GetTopFiveBins(complexSamples);

            int matchingBins = 0;
            foreach (int currentBin in currentTopBins)
            {
                foreach (int prevBin in prevTopBins)
                {
                    if (currentBin == prevBin) matchingBins++;
                }
            }
            
            if (matchingBins < 4) 
            {
                isNewFreq = true;
            }

            prevTopBins = currentTopBins;
            float beatTime = window * windowSize / (float)clip.frequency;

            if (isPeak && isOnset && isNewFreq && beatTime - lastBeatTime >= minimumSpacing)
            {
                detectedBeats.Add(beatTime);
                lastBeatTime = beatTime;
            }
        }

        if (detectedBeats.Count < 4)
        {
            float secondsPerBeat = 60f / Mathf.Max(1f, beatMap.bpm);
            for (float beatTime = 0f; beatTime < clip.length; beatTime += secondsPerBeat)
            {
                detectedBeats.Add(beatTime);
            }

            Debug.LogWarning(
                $"Waveform detection found too few beats. Using a {beatMap.bpm:0.#} BPM grid.",
                this
            );
        }

        beatMap.beats = detectedBeats.ToArray();
        Debug.Log($"Generated {beatMap.beats.Length} beats for {clip.name}.", this);
    }
    private int[] GetTopFiveBins(System.Numerics.Complex[] fftData)
    {
        int halfSize = fftData.Length / 2;
        System.Collections.Generic.List<System.Tuple<int, double>> magnitudes = new System.Collections.Generic.List<System.Tuple<int, double>>();

        for (int i = 0; i < halfSize; i++)
        {
            magnitudes.Add(new System.Tuple<int, double>(i, fftData[i].Magnitude));
        }

        magnitudes.Sort((a, b) => b.Item2.CompareTo(a.Item2));

        int[] topBins = new int[5];
        for (int i = 0; i < 5 && i < magnitudes.Count; i++)
        {
            topBins[i] = magnitudes[i].Item1;
        }
        return topBins;
    }

    private void FFT(float[] input, System.Numerics.Complex[] output)
    {
        int n = input.Length;
        for (int i = 0; i < n; i++)
        {
            output[i] = new System.Numerics.Complex(input[i], 0);
        }

        for (int i = 1, j = 0; i < n; i++)
        {
            int bit = n >> 1;
            while ((j & bit) != 0) { j ^= bit; bit >>= 1; }
            j ^= bit;
            if (i < j) { var temp = output[i]; output[i] = output[j]; output[j] = temp; }
        }

        for (int len = 2; len <= n; len <<= 1)
        {
            double angle = -2 * System.Math.PI / len;
            System.Numerics.Complex wlen = new System.Numerics.Complex(System.Math.Cos(angle), System.Math.Sin(angle));
            for (int i = 0; i < n; i += len)
            {
                System.Numerics.Complex w = 1;
                for (int j = 0; j < len / 2; j++)
                {
                    System.Numerics.Complex u = output[i + j];
                    System.Numerics.Complex v = output[i + j + len / 2] * w;
                    output[i + j] = u + v;
                    output[i + j + len / 2] = u - v;
                    w *= wlen;
                }
            }
        }
    }

    public void StartSong()
    {
        if (beatMap == null || audioSource == null)
        {
            Debug.LogError("BeatClock requires a BeatMapData asset and an AudioSource.", this);
            enabled = false;
            return;
        }

        // Capture the exact dspTime of song start
        // Schedule playback 0.1s in future for precision
        double startDelay    = currDelay;
        _songStartDsp        = AudioSettings.dspTime + startDelay;
        _nextBeatIndex       = 0;
        _nextApproachIndex   = 0;
        _isPlaying           = true;

        audioSource.clip = beatMap.song;
        if (beatMap.song != null)
        {
            audioSource.PlayScheduled(_songStartDsp);   // precision scheduled start
        }
    }

    // ── Update: fire beat events at the right moment ─────────
    void Update()
    {
        if(!_isPlaying)currDelay+=Time.deltaTime;
        if (!_isPlaying) return;
        if (beatMap == null || beatMap.beats == null || beatMap.beats.Length == 0) return;
        // Current position in the song (in seconds)
        double currentSongTime = SongTime;

        // Check if we've passed the next scheduled beat
        // TEACHING POINT: we fire SLIGHTLY BEFORE the beat
        // so visual feedback (beat ring flash) appears on time
        // The player sees it and THEN hits spacebar
        float lookAheadMs = 0f;  // set to 50-100ms for visual cue lead

        while (_nextApproachIndex < beatMap.beats.Length &&
               currentSongTime >= beatMap.beats[_nextApproachIndex] - beatApproachTime)
        {
            GameEvents.OnBeatApproaching?.Invoke(beatMap.beats[_nextApproachIndex]);
            _nextApproachIndex++;
        }

        while (_nextBeatIndex < beatMap.beats.Length &&
               currentSongTime >= beatMap.beats[_nextBeatIndex] - (lookAheadMs / 1000.0))
        {
            // Fire the beat event — HitDetector and HUD both listen
            GameEvents.OnBeat?.Invoke(beatMap.beats[_nextBeatIndex]);
            _nextBeatIndex++;
        }
    }

    // ── Helper: absolute dspTime of a beat ───────────────────
    // HitDetector uses this to compare with player's press time
    public double BeatToDspTime(float beatSongTime)
    {
        return _songStartDsp + beatSongTime;
    }
}