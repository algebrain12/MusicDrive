using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public class RampGenerator : MonoBehaviour
{
    [Header("References")]
    public SplineContainer splineContainer;
    public GameObject rampPrefab;
    public Transform rampParent;

    [Header("Ramp Settings")]
    public int noOfRamps = 5;
    public float roadWidth = 10f;
    public float offset = 0.45f;


    public void GenerateRamps()
    {
        GenerateRamps(Random.Range(int.MinValue, int.MaxValue));
    }

    public void GenerateRamps(int seed)
    {
        System.Random random = new System.Random(seed);

        for (int i = 0; i < noOfRamps; i++)
        {
            float baseT = i / (float)noOfRamps;

            float t = baseT + Range(random, 0f, 0.5f / noOfRamps);


            Vector3 center =
                (Vector3)splineContainer.EvaluatePosition(t);

            Vector3 tangent =
                (Vector3)splineContainer.EvaluateTangent(t);
            Vector3 splineUp = (Vector3)splineContainer.EvaluateUpVector(t);
            tangent.Normalize();  //mag = 1

            Vector3 right = Vector3.Cross(splineUp, tangent);

            float edgeOffset = roadWidth * offset;

            if (random.NextDouble() < 0.5f)
            {
                edgeOffset = -edgeOffset;
            }

            Vector3 spawnPos = center + (right * edgeOffset);

            Quaternion rot = Quaternion.LookRotation(-tangent,splineUp);

            Instantiate(rampPrefab, spawnPos, rot, rampParent);
        }
    }

    private static float Range(System.Random random, float min, float max)
    {
        return min + (float)random.NextDouble() * (max - min);
    }
}
