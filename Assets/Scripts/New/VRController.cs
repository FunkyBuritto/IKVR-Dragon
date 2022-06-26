using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class VRController : MonoBehaviour
{
    public static VRController instance;
    private Rigidbody rb;
    private List<Gesture> gestures = new List<Gesture>();


    private float targetRotation;
    private float rotationTime = 0;


    private Vector3 addedTargetVelocity;
    private Vector3 currentTargetVelocity;
    private float velocityTime = 0;

    [Header("Constrains")]
    [SerializeField] float diveRotation;
    [SerializeField] float climbRotation;

    [Header("Modifiers")]
    [SerializeField] float rotationSpeed;
    [SerializeField] float rotationChange;
    [SerializeField] float velocitySpeed;
    [SerializeField] float velocityChange;

    private void Start(){
        rb = GetComponent<Rigidbody>();
    }

    public void addGesture(Gesture gesture) {

    }

    void Update() {
        // Update the internal time variable
        velocityTime += velocityChange * Time.deltaTime;

        // Set the target velocity of the previous frame
        Vector3 oldTargetVelocity = Vector3.Lerp(rb.velocity, currentTargetVelocity, velocityTime);

        #region VrInputDetection

        #endregion


        // Set the target velocity of the Updated frame
        Vector3 newTargetVelocity = Vector3.Lerp(currentTargetVelocity, addedTargetVelocity, velocityTime);

        // Set the velocity to the average of the old and the new
        rb.velocity = Vector3.Lerp(oldTargetVelocity, newTargetVelocity, 0.5f);
    }
}
