using UnityEngine;
using UnityEngine.Splines;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class RoadMeshGenerator : MonoBehaviour
{
    [Header("References")]
    public SplineContainer splineContainer;
    public SpawnCar carSpawner;

    [Header("Road Settings")]
    public float roadWidth = 10f;
    public int samples = 500;

    [Header("SpawnPoint")]
    public int samplepoint = 0;
    public float startGridSpacing = 8f;
    public float startGridLaneOffset = 3f;
    public Vector3 SpawnPoint { get; private set; }
    public Vector3 SpawnForward { get; private set; }
    public Vector3 SpawnUp { get; private set; }

    [Header("Start / Finish Line")]
    [Tooltip("Prefab for the start/finish line marker. Should be a flat plane with no collider.")]
    public GameObject startFinishLinePrefab;

    [Tooltip("If true, the spawned line's local X scale is set so it spans the full road width.")]
    public bool scaleLineToRoadWidth = true;

    [Tooltip("How many world units wide the prefab is when its local scale.x = 1 (Unity's default Plane primitive is 10). Used to auto-fit it to roadWidth.")]
    public float prefabBaseWidth = 10f;

    [Tooltip("Small lift above the road surface so the line doesn't z-fight with the road mesh.")]
    public float startLineHeightOffset = 0.02f;

    [Tooltip("Extra rotation on top of the track-aligned rotation, in case your prefab's default axes don't already match forward/up.")]
    public Vector3 startLineRotationOffset = Vector3.zero;

    private GameObject spawnedStartFinishLine;

    public void GenerateRoad()
    {
        Mesh mesh = new Mesh();
        mesh.name = "Generated Road";

        Vector3[] vertices = new Vector3[samples * 2]; 
        Vector2[] uvs = new Vector2[samples * 2];   
        int[] triangles = new int[samples * 6];

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)samples; 

            Vector3 center =
                (Vector3)splineContainer.EvaluatePosition(t);

            Vector3 tangent =
                (Vector3)splineContainer.EvaluateTangent(t);  

            tangent.Normalize();  //mag = 1
            Vector3 splineUp = (Vector3)splineContainer.EvaluateUpVector(t);
            splineUp.Normalize();

            if (i == samplepoint % samples)
            {
                SpawnPoint = center;
                SpawnForward = tangent;
                SpawnUp = splineUp;
            }
            Vector3 right =
                Vector3.Cross(splineUp, tangent).normalized;  

            vertices[i * 2] =
                center - right * roadWidth * 0.5f;  //left

            vertices[i * 2 + 1] =
                center + right * roadWidth * 0.5f;  //right

            uvs[i * 2] =
                new Vector2(0, t * 20);

            uvs[i * 2 + 1] =
                new Vector2(1, t * 20);
        }

        int tri = 0;

        for (int i = 0; i < samples; i++)
        {
            int next = (i + 1) % samples;

            int a = i * 2;
            int b = a + 1;

            int c = next * 2;
            int d = c + 1;

            triangles[tri++] = a;
            triangles[tri++] = c;
            triangles[tri++] = b;

            triangles[tri++] = b;
            triangles[tri++] = c;
            triangles[tri++] = d;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        MeshFilter mf = GetComponent<MeshFilter>();
        mf.mesh = mesh;

        MeshCollider mc = GetComponent<MeshCollider>();

        if (mc == null)
        {
            mc = gameObject.AddComponent<MeshCollider>();
        }

        mc.sharedMesh = null;
        mc.sharedMesh = mesh;

        PlaceStartFinishLine();

        if (carSpawner != null)
        {
            carSpawner.Spawn(SpawnPoint,SpawnForward);
        }
    }

    // Instantiates (or repositions, on regeneration) the start/finish line prefab
    // at the players' spawn point, aligned flat across the road.
    private void PlaceStartFinishLine()
    {
        if (startFinishLinePrefab == null)
        {
            return;
        }

        Vector3 position = SpawnPoint + SpawnUp * startLineHeightOffset;
        Quaternion rotation = Quaternion.LookRotation(SpawnForward, SpawnUp) * Quaternion.Euler(startLineRotationOffset);

        if (spawnedStartFinishLine == null)
        {
            spawnedStartFinishLine = Instantiate(startFinishLinePrefab, position, rotation, transform);

            // Defensive: strip any collider that might have been left on the prefab,
            // since the line should never physically interact with the cars.
            foreach (Collider col in spawnedStartFinishLine.GetComponentsInChildren<Collider>())
            {
                if (Application.isPlaying)
                {
                    Destroy(col);
                }
                else
                {
                    DestroyImmediate(col);
                }
            }
        }
        else
        {
            spawnedStartFinishLine.transform.SetPositionAndRotation(position, rotation);
        }

        if (scaleLineToRoadWidth && prefabBaseWidth > 0f)
        {
            Vector3 scale = spawnedStartFinishLine.transform.localScale;
            scale.x = roadWidth / prefabBaseWidth;
            spawnedStartFinishLine.transform.localScale = scale;
        }
    }

    public void GetSpawnPose(int playerIndex, out Vector3 position, out Quaternion rotation)
    {
        float t = Mathf.Clamp01((samplepoint % samples) / (float)samples);
        Vector3 center = (Vector3)splineContainer.EvaluatePosition(t);
        Vector3 tangent = ((Vector3)splineContainer.EvaluateTangent(t)).normalized;
        Vector3 splineUp = ((Vector3)splineContainer.EvaluateUpVector(t)).normalized;
        Vector3 right = Vector3.Cross(splineUp, tangent).normalized;

        int row = playerIndex / 2;
        int lane = playerIndex % 2 == 0 ? -1 : 1;

        position = center - tangent * row * startGridSpacing + right * lane * startGridLaneOffset + splineUp * 5f;
        rotation = Quaternion.LookRotation(tangent, splineUp);
    }
}