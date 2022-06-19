using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VRMovement : MonoBehaviour
{
    private Vector3 targetVelocity;
    private Vector3 forwardVelocity;

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
    [SerializeField][Tooltip("The upwards force that applies when you flap your wings")] 
    private float flapForce;

    [SerializeField][Tooltip("The forwards force that applies when you flap your wings")] 
    private float moveForce;

    [SerializeField][Tooltip("The forwards force that applies when you glide")]         
    private float glideForce;

    [SerializeField][Tooltip("The speed at wich the dragon moddel follows the players view")] 
    private float rotationSpeed;

    private bool leftGlide;
    private bool rightGlide;
    private float glideEffectiveness;
    private bool cooldown = false;
    private bool leftTop, leftBotom, rightTop, rightBotom;
    private bool leftFlap, rightFlap;

    void Start() {
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
        if (leftAnchor.transform.position.y - leftController.transform.position.y < 0.2f && leftAnchor.transform.position.y - leftController.transform.position.y > -0.2f) {
            leftGlide = true;
            glideEffectiveness = Mathf.Abs((rightAnchor.transform.position.y - rightController.transform.position.y) + (leftAnchor.transform.position.y - leftController.transform.position.y));
        }
        else {
            leftGlide = false;
            glideEffectiveness = 0;
        }

        // Check if the right controllers height is between two heights and set the rightGlide variable true or false depending on that.
        if (rightAnchor.transform.position.y - rightController.transform.position.y < 0.2f && rightAnchor.transform.position.y - rightController.transform.position.y > -0.2f) {
            rightGlide = true;
            glideEffectiveness = Mathf.Abs((rightAnchor.transform.position.y - rightController.transform.position.y) + (leftAnchor.transform.position.y - leftController.transform.position.y));
        }
        else {
            rightGlide = false;
            glideEffectiveness = 0;
        }

        if (leftGlide && rightGlide) Glide();

        // If the left controller is above a certain height start the LeftTopFlapping coroutine
        if (leftAnchor.transform.position.y - leftController.transform.position.y < -0.3f)
            StartCoroutine(LeftTopFlapping());

        // If the right controller is above a certain height start the LeftTopFlapping coroutine
        if (rightAnchor.transform.position.y - rightController.transform.position.y < -0.3f)
            StartCoroutine(RightTopFlapping());

        // If the player did not just flap his wings
        if (!cooldown) {
            // Set leftbottom boolean to a true or false depending on if the left controller is below a certain height
            if (leftAnchor.transform.position.y - leftController.transform.position.y > 0.3f) leftBotom = true;
            else leftBotom = false;

            // Set rightBotom boolean to a true or false depending on if the left controller is below a certain height
            if (rightAnchor.transform.position.y - rightController.transform.position.y > 0.3f) rightBotom = true;
            else rightBotom = false;

            // If the player has leftTop and LeftBottom active start the LeftFlapTime Coroutine wich enables and disables leftFlap
            if (leftTop && leftBotom) StartCoroutine(LeftFlapTime());

            // If the player has leftTop and LeftBottom active start the RightFlapTime Coroutine wich enables and disables rightFlap
            if (rightTop && rightBotom) StartCoroutine(RightFlapTime());

            // Succesfully flaped whings if left and right flap are equal to true
            if (leftFlap && rightFlap) Flap();
        }
    }

    void Glide() {
        // Set the y velocity to value between -0.1f and 50f based on how horizontal the controllers are
        rb.velocity = new Vector3(rb.velocity.x, Mathf.Clamp(rb.velocity.y + (-0.4f + glideEffectiveness), -0.1f, 50f), rb.velocity.z);

        // Add extra velocity forward relative to the players view
        rb.velocity += (Cam.transform.rotation * Vector3.forward) * glideForce;
    }

    void Flap() {
        // Add vertical force to the current velocity
        rb.velocity = new Vector3(rb.velocity.x, rb.velocity.y + flapForce, rb.velocity.z);

        // Add extra velocity forward relative to the players view
        rb.velocity += (Cam.transform.rotation * Vector3.forward) * moveForce;

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
