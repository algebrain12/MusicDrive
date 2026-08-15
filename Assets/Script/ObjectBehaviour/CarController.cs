using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations;

public class CarController : MonoBehaviour
{
    [Header("Movement Variables")]
    public float EngineForce = 1f;
    public float AirResistence = 10f;
    public float RotationConst = 10f;

    private Rigidbody PlayerBody;
    // Start is called before the first frame update
    void Start()
    {
        PlayerBody = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 moveDir = GameInput.Instance.getMoveDir();
        PlayerBody.AddForce(transform.forward*moveDir.y*EngineForce - PlayerBody.velocity * AirResistence * (2-Vector3.Dot(transform.forward, PlayerBody.velocity.normalized)));
        transform.SetPositionAndRotation(transform.position, Quaternion.Euler(transform.rotation.eulerAngles + new Vector3(0,moveDir.x*RotationConst,0) * Vector3.Dot(transform.forward, PlayerBody.velocity)));
    }
}