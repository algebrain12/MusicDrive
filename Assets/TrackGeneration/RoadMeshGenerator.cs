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
            if (i == samplepoint % samples)
            {
                SpawnPoint = center;
                SpawnForward = tangent;
            }
            Vector3 splineUp = (Vector3)splineContainer.EvaluateUpVector(t);  
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
        if (carSpawner != null)
        {
            carSpawner.Spawn(SpawnPoint,SpawnForward);
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
