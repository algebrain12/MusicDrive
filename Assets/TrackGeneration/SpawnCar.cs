using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public class SpawnCar : MonoBehaviour
{
    
    [SerializeField]
    private Transform car;
    //[SerializeField]
    //private CameraFollow cameraFollow;
    [SerializeField]
    private RoadMeshGenerator roadMeshGenerator;
    [SerializeField]
    private float timeDelay = 0.5f;
    

   
    public void Spawn(Vector3 spawnPos, Vector3 forward)
    {
        StartCoroutine(SpawnRoutine(spawnPos, forward));
    }
    private IEnumerator SpawnRoutine(Vector3 spawnPos, Vector3 forward)
    {
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        car.transform.position = spawnPos + Vector3.up * 5f;
        car.transform.rotation = Quaternion.LookRotation(forward);
        
    }
}