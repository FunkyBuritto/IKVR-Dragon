using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VRMovement : MonoBehaviour
{
    [SerializeField] private Vector3 targetVelocity;
    [SerializeField] private Vector3 forwardVelocity;
    private float camAngle;

    private float wingHeight;
    private GameObject leftAnchor;
    private GameObject rightAnchor;
    private GameObject Cam;

    private Rigidbody rb;

    [Header("GameObjects")]
    [SerializeField] private GameObject leftController;
    [SerializeField] private GameObject rightController;
    [SerializeField] private GameObject dragonObject;
    [SerializeField] private GameObject visualiserObject;

    [Header("Movement")]
    [SerializeField] [Tooltip("The speed at which the player changes velocity between directions when gliding")]
    private float glideTransition;

    [SerializeField] [Tooltip("The speed at which the player changes velocity between directions when flapping his wings")]
    private float flapTransition;

    [SerializeField] [Tooltip("The speed at which the player constantly slows down in the forward direction")]
    private float horizontalDrag;

    [SerializeField] [Tooltip("The speed at which the player constantly moves down")]
    private float gravity;

    [SerializeField][Tooltip("The upwards force that applies when you flap your wings")] 
    private float flapForce;

    [SerializeField][Tooltip("The forwards force that applies when you flap your wings")] 
    private float moveForce;

    [SerializeField][Tooltip("The forwards force that applies when you glide")]         
    private float glideForce;

    [SerializeField][Tooltip("The speed at wich the dragon model follows the players view")] 
    private float rotationSpeed;

    private bool leftGlide;
    private bool rightGlide;
    private float glideEffectiveness;
    private bool cooldown = false;
    private bool leftTop, leftBotom, rightTop, rightBotom;
    private bool leftFlap, rightFlap;

    void Start() {
        // Get the calibrated wing height
        wingHeight = PlayerPrefs.GetFloat("height");
        Debug.Log(wingHeight);

        // Find the reference points for the flap/glide heights
        leftAnchor = GameObject.Find("LeftGestures");
        rightAnchor = GameObject.Find("RightGestures");

        // Assign the rigidbody and camera
        rb = gameObject.GetComponent<Rigidbody>();
        Cam = Camera.main.gameObject;
    }

    void Update() {
        // Set the rotation and position of the dragon so they folow the players position and view rotation smoothly.
        dragonObject.transform.rotation = Quaternion.LerpUnclamped(dragonObject.transform.rotation, Cam.transform.localRotation, Time.deltaTime * rotationSpeed);
        dragonObject.transform.position = transform.position + Vector3.up * 1.4f;

        // Set the rotation of the visualiser to always rotate around the players y view axis.
        visualiserObject.transform.rotation = new Quaternion(0, Mathf.Lerp(visualiserObject.transform.rotation.y, Cam.transform.localRotation.y, Time.deltaTime * rotationSpeed), 0, 0);

        // Check if the left controllers height is between two heights and set the leftGlide variable true or false depending on that.
        if (wingHeight - leftController.transform.localPosition.y < 0.2f && wingHeight - leftController.transform.localPosition.y > -0.2f) {
            leftGlide = true;
            glideEffectiveness = Mathf.Abs((wingHeight - rightController.transform.localPosition.y) + (wingHeight - leftController.transform.localPosition.y));
        }
        else {
            leftGlide = false;
            glideEffectiveness = 0;
        }

        // Check if the right controllers height is between two heights and set the rightGlide variable true or false depending on that.
        if (wingHeight - rightController.transform.localPosition.y < 0.2f && wingHeight - rightController.transform.localPosition.y > -0.2f) {
            rightGlide = true;
            glideEffectiveness = Mathf.Abs((wingHeight - rightController.transform.localPosition.y) + (wingHeight - leftController.transform.localPosition.y));
        }
        else {
            rightGlide = false;
            glideEffectiveness = 0;
        }

        if (leftGlide && rightGlide) Glide();


        // If the left controller is above a certain height start the LeftTopFlapping coroutine
        if (wingHeight - leftController.transform.localPosition.y < -0.3f)
            StartCoroutine(LeftTopFlapping());

        // If the right controller is above a certain height start the LeftTopFlapping coroutine
        if (wingHeight - rightController.transform.localPosition.y < -0.3f)
            StartCoroutine(RightTopFlapping());

        // If the player did not just flap his wings
        if (!cooldown) {
            // Set leftbottom boolean to a true or false depending on if the left controller is below a certain height
            if (wingHeight - leftController.transform.localPosition.y > 0.3f) leftBotom = true;
            else leftBotom = false;

            // Set rightBotom boolean to a true or false depending on if the left controller is below a certain height
            if (wingHeight - rightController.transform.localPosition.y > 0.3f) rightBotom = true;
            else rightBotom = false;

            // If the player has leftTop and LeftBottom active start the LeftFlapTime Coroutine wich enables and disables leftFlap
            if (leftTop && leftBotom) StartCoroutine(LeftFlapTime());

            // If the player has leftTop and LeftBottom active start the RightFlapTime Coroutine wich enables and disables rightFlap
            if (rightTop && rightBotom) StartCoroutine(RightFlapTime());

            // Succesfully flaped whings if left and right flap are equal to true
            if (leftFlap && rightFlap) Flap();
        }

        // Converting the camera forward vector to a rotation
        camAngle = Vector3.Angle(Vector3.forward, Cam.transform.forward);
        if (Cam.transform.forward.x < 0.0f)
            camAngle = 360f - camAngle;

        // Apply Drag and Gravity to the target velocity
        targetVelocity = new Vector3(targetVelocity.x - targetVelocity.x < 0 ? -(Time.deltaTime * horizontalDrag) : (Time.deltaTime * horizontalDrag), 
                                     targetVelocity.y - (Time.deltaTime * (targetVelocity.y > 10f ? gravity * 2 : gravity)), 
                                     targetVelocity.z - (Time.deltaTime * horizontalDrag));

        // Rotating the target velocity so it becomes the forward velocity
        forwardVelocity = Quaternion.AngleAxis(camAngle, Vector3.up).normalized * targetVelocity;

        // Apply calculated velocity's
        rb.velocity = Vector3.Lerp(rb.velocity, forwardVelocity, Time.deltaTime * flapTransition);
    }

    void Glide() {
        // Set the vertical velocity to a small downwards force and cancel out the gravity and drag
        targetVelocity = new Vector3(rb.velocity.x, (rb.velocity.y > 0 ? -2 + glideEffectiveness : -0.25f + glideEffectiveness) + (gravity * Time.deltaTime), Mathf.Abs(rb.velocity.z) + glideForce);
    }

    void Flap() {
        targetVelocity = new Vector3(rb.velocity.x, rb.velocity.y + flapForce,  Mathf.Abs(rb.velocity.z) + moveForce);

        // Flapped succesfully so disable left and right flap
        leftFlap = false;
        rightFlap = false;

        // Set a cooldown so you can't spam your flaps
        StartCoroutine(CooldownTimer());
    }

    // Cooldown so the player can't spam
    IEnumerator CooldownTimer() {
        cooldown = true;
        yield return new WaitForSeconds(0.7f);
        cooldown = false;
    }

    // All the bellow add extra time for the player to flap so its easyer to succesfully flap
    IEnumerator LeftTopFlapping() {
        leftTop = true;
        yield return new WaitForSeconds(0.7f);
        leftTop = false;
    }
    IEnumerator RightTopFlapping() {
        rightTop = true;
        yield return new WaitForSeconds(0.7f);
        rightTop = false;
    }
    IEnumerator LeftFlapTime() {
        leftFlap = true;
        yield return new WaitForSeconds(0.5f);
        leftFlap = false;
    }
    IEnumerator RightFlapTime() {
        rightFlap = true;
        yield return new WaitForSeconds(0.5f);
        rightFlap = false;
    }
}
