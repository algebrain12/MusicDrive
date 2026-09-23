using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshTrail : MonoBehaviour {

    [SerializeField] private float activeTime = 0.2f;
    [SerializeField] private float meshRefreshRate = 0.05f;
    [SerializeField] private float meshDestroyDelay = 0.5f;
    [SerializeField] private Transform positionToSpawn;
    [SerializeField] private Mesh bodyMesh;
    [SerializeField] private Material trailMat;

    private bool isTrailActive;
    private Mesh combinedMesh;

    private void Start() {

        // First we will combine all submeshes inside the body mesh to make one unified mesh
        combinedMesh = new Mesh();
        List<CombineInstance> instances = new List<CombineInstance>();
        for (int i = 0; i < bodyMesh.subMeshCount; i++) {
            CombineInstance ci = new CombineInstance();
            ci.mesh = bodyMesh;
            ci.subMeshIndex = i;
            ci.transform = Matrix4x4.identity;
            instances.Add(ci);
        }
        combinedMesh.CombineMeshes(instances.ToArray(), true);
    }


    private void Update() {
        //Change the inside of this if to control when the trail is activated
        if (Input.GetKeyDown(KeyCode.Space) && !isTrailActive) {
            isTrailActive = true;
            StartCoroutine(ActivateTrail(activeTime));
        }
    }

    // This IEnumerator controls the loop of the trail
    IEnumerator ActivateTrail(float timeActive) {
        while (timeActive > 0) {
            timeActive -= meshRefreshRate;


            // create a new gameobject to be spawned
            GameObject gobj = new GameObject();
            gobj.transform.SetPositionAndRotation(positionToSpawn.position, positionToSpawn.rotation);
            gobj.transform.localScale = Vector3.one * 100;

            // add mesh components to the spawned object
            MeshRenderer mr = gobj.AddComponent<MeshRenderer>();
            MeshFilter mf = gobj.AddComponent<MeshFilter>();

            // assign the mesh and material to the object
            mf.mesh = combinedMesh;
            mr.material = trailMat;

            // destroy the object after some time
            Destroy(gobj, meshDestroyDelay);

            yield return new WaitForSeconds(meshRefreshRate);
        }

        isTrailActive = false;
    }

}