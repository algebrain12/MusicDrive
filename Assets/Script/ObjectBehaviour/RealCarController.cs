using System;
using System.Collections.Generic;
using UnityEngine;

public class RealCarController : MonoBehaviour {

    public enum Axel {
        Front,
        Back,
    }

    [Serializable]
    public struct Wheel {
        public GameObject wheelMesh;
        public WheelCollider wheelCollider;
        public Axel axel;
    }

    [Header("Stats")]
    [SerializeField] private float motorTorque = 300f;
    [SerializeField] private float brakeTorque = 1000f;
    [SerializeField] private float turnSensitivity = 5f;   // Fix 4: raised from 0.6
    [SerializeField] private float maxSteerAngle = 45f;
    [SerializeField] public float maxSpeed = 50000f;

    [SerializeField] private Vector3 centerOfMass;

    [Header("References")]
    [SerializeField] private List<Wheel> wheelsList;

    private float moveInput;
    private float steerInput;
    private Rigidbody carRb;
    private float currentSpeed;
    public float CurrentSpeedKph => carRb != null ? carRb.velocity.magnitude * 3.6f : 0f;

    private void Awake() {
        carRb = GetComponent<Rigidbody>();
    }

    private void Start() {
        carRb.centerOfMass = centerOfMass;
        GameEvents.OnComboChanged += ChangeMaxSpeed;
    }

    private void ChangeMaxSpeed(int combo)
    {
        if(combo > 5)return;
        maxSpeed = (combo+1)*10000f;
    }

    private void Update() {
        GetInput();
    }

    private void FixedUpdate() {     
        Move();
        Steer();
        AnimateWheels();
    }

    private void GetInput() {
        moveInput = GameInput.Instance.getMoveDir().y;
        steerInput = GameInput.Instance.getMoveDir().x;
    }

    private void Move() {
        currentSpeed = 2 * Mathf.PI * wheelsList[0].wheelCollider.radius
                       * wheelsList[0].wheelCollider.rpm * 60f;

        float forwardVelocityDir = Vector3.Dot(
            carRb.velocity.normalized,
            transform.forward.normalized
        );

        foreach (Wheel wheel in wheelsList) {

            if (wheel.axel == Axel.Back) {

                bool isBraking = (moveInput > 0 && forwardVelocityDir < -0.1f)
                              || (moveInput < 0 && forwardVelocityDir > 0.1f);

                if (isBraking) {
                    wheel.wheelCollider.motorTorque = 0;                         
                    wheel.wheelCollider.brakeTorque = Mathf.Abs(moveInput) * brakeTorque;
                }
                else if (Mathf.Abs(currentSpeed) < maxSpeed) {
                    wheel.wheelCollider.brakeTorque = 0;                          
                    wheel.wheelCollider.motorTorque = moveInput * motorTorque;
                }
                else {
                    wheel.wheelCollider.motorTorque = 0;
                    wheel.wheelCollider.brakeTorque = 0;
                }
            }

            
            if (wheel.axel == Axel.Front) {
                wheel.wheelCollider.motorTorque = 0;
                wheel.wheelCollider.brakeTorque = 0;
            }
        }
    }

    private void Steer() {
        foreach (Wheel wheel in wheelsList) {
            if (wheel.axel == Axel.Front) {
                float targetAngle = steerInput * maxSteerAngle;
                wheel.wheelCollider.steerAngle = Mathf.Lerp(
                    wheel.wheelCollider.steerAngle,
                    targetAngle,
                    turnSensitivity * Time.fixedDeltaTime
                );
            }
        }
    }

    private void AnimateWheels() {
        foreach (Wheel wheel in wheelsList) {
            wheel.wheelCollider.GetWorldPose(out Vector3 pos, out Quaternion rot);
            wheel.wheelMesh.transform.position = pos;
            wheel.wheelMesh.transform.rotation = rot;
        }
    }
}