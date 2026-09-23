using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

public class RotScript : MonoBehaviour
{
    public float RotSpeed = 10f;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(new UnityEngine.Vector3(0,RotSpeed,0)*Time.deltaTime);
    }
}
