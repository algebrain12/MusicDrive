using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum HitRes{Ok, Good, Perfect}
public class GameEvents
{
    public static Action<float> OnBeat;         
    public static Action<float> OnBeatApproaching; 

    public static Action<HitRes, float> OnHit; 

    public static Action OnMiss;                   

    public static Action<int> OnComboChanged; 

    public static Action<float> OnNitroTick;    

    public static Action<int> OnNitroLayerChanged; 
}
