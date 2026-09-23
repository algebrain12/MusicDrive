using System.Collections.Generic;
using UnityEngine;

public class MapScatterer : MonoBehaviour
{
    [Header("Generation Triggers")]
    [Tooltip("If checked, objects will scatter automatically when the game begins.")]
    public bool generateOnStart = true;
    
    [Tooltip("If checked, clears any existing child objects in containerParent before generating on Start.")]
    public bool clearOnStart = true;

    [Header("Randomization Seed")]
    [Tooltip("Use fixed seed for reproducible layouts, or 0 for dynamic random placement every run.")]
    public int randomSeed = 0;

    [Header("Prefabs & Parent")]
    [Tooltip("Assign your 4 models here.")]
    public GameObject[] prefabsToScatter = new GameObject[4];
    
    [Tooltip("Parent transform to keep your Hierarchy clean.")]
    public Transform containerParent;

    [Header("3D Scatter Area Bounds (Outer)")]
    public Vector3 mapBoundsMin = new Vector3(-50f, -50f, -50f);
    public Vector3 mapBoundsMax = new Vector3(50f, 50f, 50f);

    [Header("Exclusion Zone Bounds (Inner)")]
    [Tooltip("No objects will spawn inside this inner cuboid. Must be fully contained within the outer bounds above.")]
    public Vector3 innerBoundsMin = new Vector3(-10f, -10f, -10f);
    [Tooltip("No objects will spawn inside this inner cuboid. Must be fully contained within the outer bounds above.")]
    public Vector3 innerBoundsMax = new Vector3(10f, 10f, 10f);
    
    [Header("Spacing & Placement")]
    [Tooltip("Minimum 3D distance between placed objects.")]
    public float minDistance = 5f;
    
    [Tooltip("How many attempts to place a point before moving on. Higher = denser packing.")]
    public int rejectionSamples = 30;

    [Header("Transform Variations")]
    public bool randomRotation = true;
    public Vector3 minScale = Vector3.one;
    public Vector3 maxScale = Vector3.one;

    private void Start()
    {
        if (generateOnStart)
        {
            if (clearOnStart)
            {
                ClearExisting();
            }

            if (randomSeed != 0)
            {
                Random.InitState(randomSeed);
            }

            ScatterObjects();
        }
    }

    [ContextMenu("Generate 3D Scatter")]
    public void ScatterObjects()
    {
        if (prefabsToScatter == null || prefabsToScatter.Length == 0)
        {
            Debug.LogWarning("No prefabs assigned to scatter!");
            return;
        }

        if (!ValidateInnerBounds())
        {
            Debug.LogWarning("Inner exclusion bounds are not fully contained within the outer map bounds. Clamping automatically.");
        }

        // 1. Generate 3D Poisson points in space (X, Y, Z), skipping the inner exclusion cuboid
        List<Vector3> points = Generate3DPoissonPoints();

        // 2. Instantiate prefabs at generated positions
        int spawnedCount = 0;
        foreach (Vector3 point in points)
        {
            // Select a random prefab from the array
            GameObject selectedPrefab = prefabsToScatter[Random.Range(0, prefabsToScatter.Length)];
            
            // Random full 3D rotation or identity
            Quaternion rotation = randomRotation 
                ? Quaternion.Euler(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 360f)) 
                : Quaternion.identity;

            GameObject newObj = Instantiate(
                selectedPrefab,
                point,
                rotation,
                transform
            );

            // Random Scale
            Vector3 randomScale = new Vector3(
                Random.Range(minScale.x, maxScale.x),
                Random.Range(minScale.y, maxScale.y),
                Random.Range(minScale.z, maxScale.z)
            );
            newObj.transform.localScale = randomScale;

            // Parent organization
            /*if (containerParent != null)
            {
                newObj.transform.SetParent(containerParent);
            }*/

            spawnedCount++;
        }

        Debug.Log($"Successfully scattered {spawnedCount} objects throughout 3D space (outside the inner exclusion zone)!");
    }

    [ContextMenu("Clear Scattered Objects")]
    public void ClearExisting()
    {
        if (containerParent == null) return;

        for (int i = containerParent.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying)
            {
                Destroy(containerParent.GetChild(i).gameObject);
            }
            else
            {
                DestroyImmediate(containerParent.GetChild(i).gameObject);
            }
        }
    }

    // Checks that the inner bounds sit fully inside the outer bounds; returns false if clamping was needed.
    private bool ValidateInnerBounds()
    {
        Vector3 clampedMin = Vector3.Max(innerBoundsMin, mapBoundsMin);
        Vector3 clampedMax = Vector3.Min(innerBoundsMax, mapBoundsMax);

        bool wasValid = clampedMin == innerBoundsMin && clampedMax == innerBoundsMax
                         && innerBoundsMin.x <= innerBoundsMax.x
                         && innerBoundsMin.y <= innerBoundsMax.y
                         && innerBoundsMin.z <= innerBoundsMax.z;

        // Ensure min <= max after clamping too
        innerBoundsMin = Vector3.Min(clampedMin, clampedMax);
        innerBoundsMax = Vector3.Max(clampedMin, clampedMax);

        return wasValid;
    }

    // Algorithmic Core: 3D Poisson Disc Sampling with an inner exclusion cuboid
    private List<Vector3> Generate3DPoissonPoints()
    {
        Vector3 regionSize = mapBoundsMax - mapBoundsMin;
        float cellSize = minDistance / Mathf.Sqrt(3);

        int gridWidth = Mathf.CeilToInt(regionSize.x / cellSize);
        int gridHeight = Mathf.CeilToInt(regionSize.y / cellSize);
        int gridDepth = Mathf.CeilToInt(regionSize.z / cellSize);

        int[,,] grid = new int[gridWidth, gridHeight, gridDepth];
        List<Vector3> points = new List<Vector3>();
        List<Vector3> spawnPoints = new List<Vector3>();

        // Inner exclusion cuboid, converted into the same local (0..regionSize) space used below
        Vector3 innerMinLocal = innerBoundsMin - mapBoundsMin;
        Vector3 innerMaxLocal = innerBoundsMax - mapBoundsMin;

        // Seed the queue with several valid points so growth can reach every side of the
        // exclusion zone (a single center seed can fail or get stuck if it starts inside
        // the inner cuboid, or if the inner cuboid splits the region into separate pockets).
        foreach (Vector3 seed in GetSeedPoints(regionSize, innerMinLocal, innerMaxLocal))
        {
            spawnPoints.Add(seed);
        }

        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning("Could not find any valid seed point outside the inner exclusion cuboid. No objects will be scattered.");
            return points;
        }

        while (spawnPoints.Count > 0)
        {
            int spawnIndex = Random.Range(0, spawnPoints.Count);
            Vector3 spawnCentre = spawnPoints[spawnIndex];
            bool candidateAccepted = false;

            for (int i = 0; i < rejectionSamples; i++)
            {
                // Generate uniform random point in spherical shell between minDistance and 2*minDistance
                Vector3 dir = Random.onUnitSphere;
                Vector3 candidate = spawnCentre + dir * Random.Range(minDistance, 2f * minDistance);

                if (IsValid(candidate, regionSize, cellSize, minDistance, points, grid, innerMinLocal, innerMaxLocal))
                {
                    points.Add(candidate);
                    spawnPoints.Add(candidate);

                    int cellX = (int)(candidate.x / cellSize);
                    int cellY = (int)(candidate.y / cellSize);
                    int cellZ = (int)(candidate.z / cellSize);
                    grid[cellX, cellY, cellZ] = points.Count;

                    candidateAccepted = true;
                    break;
                }
            }

            if (!candidateAccepted)
            {
                spawnPoints.RemoveAt(spawnIndex);
            }
        }

        // Offset points to fit mapBoundsMin/Max
        for (int i = 0; i < points.Count; i++)
        {
            points[i] += mapBoundsMin;
        }

        return points;
    }

    // Finds a handful of starting points (region center plus outer corners) that lie
    // outside the inner exclusion cuboid, so the Poisson growth has coverage on every
    // side of the exclusion zone.
    private List<Vector3> GetSeedPoints(Vector3 regionSize, Vector3 innerMin, Vector3 innerMax)
    {
        List<Vector3> seeds = new List<Vector3>();
        float epsilon = 0.001f;

        Vector3 center = regionSize / 2f;
        if (!IsInsideCuboid(center, innerMin, innerMax))
        {
            seeds.Add(center);
        }

        // The 8 corners of the outer region (slightly inset so they stay < regionSize).
        float[] xs = { epsilon, regionSize.x - epsilon };
        float[] ys = { epsilon, regionSize.y - epsilon };
        float[] zs = { epsilon, regionSize.z - epsilon };

        foreach (float x in xs)
        {
            foreach (float y in ys)
            {
                foreach (float z in zs)
                {
                    Vector3 corner = new Vector3(x, y, z);
                    if (!IsInsideCuboid(corner, innerMin, innerMax))
                    {
                        seeds.Add(corner);
                    }
                }
            }
        }

        // Fallback: if every corner and the center were somehow inside the inner cuboid
        // (e.g. inner bounds misconfigured to match outer bounds), try random sampling.
        if (seeds.Count == 0)
        {
            for (int i = 0; i < 100; i++)
            {
                Vector3 candidate = new Vector3(
                    Random.Range(0f, regionSize.x),
                    Random.Range(0f, regionSize.y),
                    Random.Range(0f, regionSize.z)
                );

                if (!IsInsideCuboid(candidate, innerMin, innerMax))
                {
                    seeds.Add(candidate);
                    break;
                }
            }
        }

        return seeds;
    }

    private bool IsValid(Vector3 candidate, Vector3 regionSize, float cellSize, float radius, List<Vector3> points, int[,,] grid, Vector3 innerMin, Vector3 innerMax)
    {
        if (candidate.x >= 0 && candidate.x < regionSize.x &&
            candidate.y >= 0 && candidate.y < regionSize.y &&
            candidate.z >= 0 && candidate.z < regionSize.z)
        {
            // Reject anything that falls inside the inner exclusion cuboid
            if (IsInsideCuboid(candidate, innerMin, innerMax))
            {
                return false;
            }

            int cellX = (int)(candidate.x / cellSize);
            int cellY = (int)(candidate.y / cellSize);
            int cellZ = (int)(candidate.z / cellSize);

            int startX = Mathf.Max(0, cellX - 2);
            int endX = Mathf.Min(cellX + 2, grid.GetLength(0) - 1);
            int startY = Mathf.Max(0, cellY - 2);
            int endY = Mathf.Min(cellY + 2, grid.GetLength(1) - 1);
            int startZ = Mathf.Max(0, cellZ - 2);
            int endZ = Mathf.Min(cellZ + 2, grid.GetLength(2) - 1);

            for (int x = startX; x <= endX; x++)
            {
                for (int y = startY; y <= endY; y++)
                {
                    for (int z = startZ; z <= endZ; z++)
                    {
                        int pointIndex = grid[x, y, z] - 1;
                        if (pointIndex != -1)
                        {
                            float sqrDst = (candidate - points[pointIndex]).sqrMagnitude;
                            if (sqrDst < radius * radius)
                            {
                                return false;
                            }
                        }
                    }
                }
            }
            return true;
        }
        return false;
    }

    private bool IsInsideCuboid(Vector3 point, Vector3 boundsMin, Vector3 boundsMax)
    {
        return point.x >= boundsMin.x && point.x <= boundsMax.x &&
               point.y >= boundsMin.y && point.y <= boundsMax.y &&
               point.z >= boundsMin.z && point.z <= boundsMax.z;
    }

    private void OnDrawGizmosSelected()
    {
        // Draw the outer bounding box in scene view
        Gizmos.color = Color.cyan;
        Vector3 outerCenter = (mapBoundsMin + mapBoundsMax) / 2f;
        Vector3 outerSize = mapBoundsMax - mapBoundsMin;
        Gizmos.DrawWireCube(outerCenter, outerSize);

        // Draw the inner exclusion cuboid in a different color
        Gizmos.color = Color.red;
        Vector3 innerCenter = (innerBoundsMin + innerBoundsMax) / 2f;
        Vector3 innerSize = innerBoundsMax - innerBoundsMin;
        Gizmos.DrawWireCube(innerCenter, innerSize);
    }
}