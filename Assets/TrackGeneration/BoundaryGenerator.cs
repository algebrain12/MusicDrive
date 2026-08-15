using UnityEngine;
using UnityEngine.Splines;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class BoundaryGenerator : MonoBehaviour
{
    [Header("References")]
    public SplineContainer splineContainer;

    [Header("Boundary Settings")]
    public float roadWidth = 10f;
    public float boundaryOffset = 0.5f;
    public float wallHeight = 2f;
    public int samples = 500;
    public float uvTileY = 20f;

    public void GenerateBoundaries()
    {
        if (splineContainer == null || samples < 2)
        {
            Debug.LogWarning("Missing SplineContainer or sample count is too low!");
            return;
        }

        Mesh mesh = new Mesh { name = "Generated Boundaries" };

        bool isClosed = splineContainer.Spline.Closed;
        int numSegments = isClosed ? samples : samples - 1;

        // 8 vertices per sample point (4 for outer face, 4 for inner face)
        // This stops normal fighting and fixes the dark shading artifacts completely!
        int vertCount = samples * 8;
        Vector3[] vertices = new Vector3[vertCount];
        Vector2[] uvs = new Vector2[vertCount];

        // 2 walls * 2 sides (inner/outer) * 2 tris * 3 indices
        int totalTriangles = numSegments * 2 * 2 * 6;
        int[] triangles = new int[totalTriangles];

        float halfWidthOffset = (roadWidth * 0.5f) + boundaryOffset;

        for (int i = 0; i < samples; i++)
        {
            float t = isClosed ? (float)i / samples : (float)i / (samples - 1);

            Vector3 center = (Vector3)splineContainer.EvaluatePosition(t);
            Vector3 tangent = Vector3.Normalize((Vector3)splineContainer.EvaluateTangent(t));
            Vector3 splineUp = Vector3.Normalize((Vector3)splineContainer.EvaluateUpVector(t));

            Vector3 right = Vector3.Cross(splineUp, tangent).normalized;

            Vector3 leftBase = center - (right * halfWidthOffset);
            Vector3 rightBase = center + (right * halfWidthOffset);
            Vector3 leftTop = leftBase + (splineUp * wallHeight);
            Vector3 rightTop = rightBase + (splineUp * wallHeight);

            int vIndex = i * 8;
            float uvY = t * uvTileY;

            // --- LEFT WALL (Outer Face: 0,1 | Inner Face: 2,3) ---
            vertices[vIndex + 0] = leftBase; 
            vertices[vIndex + 1] = leftTop;
            uvs[vIndex + 0] = new Vector2(0.0f, uvY);
            uvs[vIndex + 1] = new Vector2(0.5f, uvY);

            vertices[vIndex + 2] = leftBase; 
            vertices[vIndex + 3] = leftTop;
            uvs[vIndex + 2] = new Vector2(0.0f, uvY);
            uvs[vIndex + 3] = new Vector2(0.5f, uvY);

            // --- RIGHT WALL (Outer Face: 4,5 | Inner Face: 6,7) ---
            vertices[vIndex + 4] = rightBase; 
            vertices[vIndex + 5] = rightTop;
            uvs[vIndex + 4] = new Vector2(0.5f, uvY);
            uvs[vIndex + 5] = new Vector2(1.0f, uvY);

            vertices[vIndex + 6] = rightBase; 
            vertices[vIndex + 7] = rightTop;
            uvs[vIndex + 6] = new Vector2(0.5f, uvY);
            uvs[vIndex + 7] = new Vector2(1.0f, uvY);
        }

        int tri = 0;

        for (int i = 0; i < numSegments; i++)
        {
            int next = (i + 1) % samples;

            int curr = i * 8;
            int nxt = next * 8;

            // --- LEFT WALL OUTER FACE ---
            triangles[tri++] = curr + 0;
            triangles[tri++] = curr + 1;
            triangles[tri++] = nxt + 0;

            triangles[tri++] = curr + 1;
            triangles[tri++] = nxt + 1;
            triangles[tri++] = nxt + 0;

            // --- LEFT WALL INNER FACE ---
            triangles[tri++] = curr + 2;
            triangles[tri++] = nxt + 2;
            triangles[tri++] = curr + 3;

            triangles[tri++] = curr + 3;
            triangles[tri++] = nxt + 2;
            triangles[tri++] = nxt + 3;

            // --- RIGHT WALL OUTER FACE ---
            triangles[tri++] = curr + 4;
            triangles[tri++] = nxt + 4;
            triangles[tri++] = curr + 5;

            triangles[tri++] = curr + 5;
            triangles[tri++] = nxt + 4;
            triangles[tri++] = nxt + 5;

            // --- RIGHT WALL INNER FACE ---
            triangles[tri++] = curr + 6;
            triangles[tri++] = curr + 7;
            triangles[tri++] = nxt + 6;

            triangles[tri++] = curr + 7;
            triangles[tri++] = nxt + 7;
            triangles[tri++] = nxt + 6;
        }

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        MeshFilter mf = GetComponent<MeshFilter>();
        mf.mesh = mesh;

        if (TryGetComponent<MeshCollider>(out var mc))
        {
            mc.sharedMesh = null;
            mc.sharedMesh = mesh;
        }
    }
}