using UnityEngine;
using UnityEngine.Splines;

public class RandomTrackGenerator : MonoBehaviour
{
    [Header("References")]
    public SplineContainer splineContainer;
    public RoadMeshGenerator roadGenerator;
    public BoundaryGenerator boundaryGenerator;
    public RampGenerator rampGenerator;

    [Header("Track Shape")]
    public int pointCount = 12;

    public float minRadius = 50f;
    public float maxRadius = 100f;

    [Tooltip("Maximum change in radius between adjacent points")]
    public float radiusStep = 10f;  

    [Tooltip("Random angle offset in radians")]
    public float angleVariation = 0.2f;

    [Header("Random Track Generation")]
    public bool generateOnStart = true;
    public float noiseScale = 0.05f;
    public float elevationScale = 10f;
    private float randomSeed;

    public float sharpTurnProb = 0.3f;

    private void Start()
    {
        if (generateOnStart)
        {
            GenerateTrack();
        }
    }

    [ContextMenu("Generate Track")]
    public void GenerateTrack()
    {
        GenerateTrack(Random.Range(int.MinValue, int.MaxValue));
    }

    public void GenerateTrack(int seed)
    {
        Spline spline = splineContainer.Spline;
        System.Random random = new System.Random(seed);

        spline.Clear();

        float currentRadius =
            Range(random, minRadius, maxRadius);

        randomSeed = Range(random, 0f, 10000f);

        for (int i = 0; i < pointCount; i++)
        {
            float baseAngle =
                i * Mathf.PI * 2f / pointCount;

            float angle =
                baseAngle +
                Range(random,
                    -angleVariation,
                    angleVariation
                );

            currentRadius +=
                Range(random,
                    -radiusStep,
                    radiusStep
                );

            currentRadius =
                Mathf.Clamp(
                    currentRadius,
                    minRadius,
                    maxRadius
                );

            float x = Mathf.Cos(angle) * currentRadius;
            float z = Mathf.Sin(angle) * currentRadius;

            float noise = Mathf.PerlinNoise(x * noiseScale * randomSeed, z* noiseScale * randomSeed);  // 0 to 1
            //Controlled Randomness 

            float y = noise * elevationScale;
            Vector3 pos = new Vector3(x,y,z);

            spline.Add(new BezierKnot(pos));
        }

        for (int i = 0; i < spline.Count; i++)
        {
            
            if (!(i == 0 || i == spline.Count - 1) && random.NextDouble() <= sharpTurnProb)
            {
                spline.SetTangentMode(i, TangentMode.Broken);
            }
            else
            {
                spline.SetTangentMode(i,TangentMode.AutoSmooth);
            }
        }

        spline.Closed = true;

        if (roadGenerator != null)
        {
            roadGenerator.GenerateRoad();
            if (boundaryGenerator != null)
            {
                boundaryGenerator.GenerateBoundaries();
            }

            if (rampGenerator != null)
            {
                rampGenerator.GenerateRamps(seed);
            }
        }
    }

    private static float Range(System.Random random, float min, float max)
    {
        return min + (float)random.NextDouble() * (max - min);
    }

    private void OnDrawGizmos()
    {
        if (splineContainer == null)
            return;

        var spline = splineContainer.Spline;

        Gizmos.color = Color.red;

        for (int i = 0; i < spline.Count; i++)
        {
            Vector3 p = spline[i].Position;

            Gizmos.DrawSphere(
                transform.TransformPoint(p),
                2f
            );
        }
    }
}
