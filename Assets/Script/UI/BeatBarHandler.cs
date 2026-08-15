using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEngine;

public class BeatBarHandler : MonoBehaviour
{
    public BeatClock mainBeatClock;
    [SerializeField]private GameObject beatIndicator;
    public float BeatTimeRangeSec = 5;
    private List<GameObject> Indicators = new List<GameObject>();
    private List<float> currentBeatTimes = new List<float>();

    private RectTransform thistransform;

    void Start()
    {
        thistransform = GetComponent<RectTransform>();
        beatIndicator.transform.localScale  = new Vector3(1,1,1);
        beatIndicator.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            mainBeatClock.beatMap.okWindowMs/1000*thistransform.rect.width/BeatTimeRangeSec
        );
        foreach(float beattime in mainBeatClock.beatMap.beats)
        {
                if(!currentBeatTimes.Contains(beattime)){
                    currentBeatTimes.Add(beattime);
                    Indicators.Add(Instantiate(beatIndicator, transform));
                }
        }
    }

    void Update()
    {
        double currTime = mainBeatClock.SongTime;
        /*
        foreach(float beattime in mainBeatClock.beatMap.beats)
        {
            Debug.Log(beattime);
            if(Math.Abs(beattime - currTime) <= BeatTimeRangeSec)
            {
                if(!currentBeatTimes.Contains(beattime)){
                    currentBeatTimes.Add(beattime);
                    Indicators.Add(Instantiate(beatIndicator, transform));
                }
            }
        }
        if(currentBeatTimes.Count > 0 && Math.Abs(currentBeatTimes[0] - currTime) > BeatTimeRangeSec)
        {
            Destroy(Indicators[0]);
            Indicators.RemoveAt(0);
            currentBeatTimes.RemoveAt(0);
        }*/
        for(int i = 0; i < currentBeatTimes.Count; i++)
        {
            float diff = (float)(currTime - currentBeatTimes[i]);
            Indicators[i].transform.SetLocalPositionAndRotation(new Vector3(diff/BeatTimeRangeSec*thistransform.rect.width,0,0),
            Indicators[i].transform.rotation);
        }

    } 
}
