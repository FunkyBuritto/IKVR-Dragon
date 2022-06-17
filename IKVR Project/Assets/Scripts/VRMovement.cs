using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VRMovement : MonoBehaviour
{
    //public static Gesture[] gestures = new Gesture[18];
    //public static GameObject[] leftObject = new GameObject[9];//topMidL, topFrontL, topBackL, midL, frontL, backL, bottomMidL, bottomFrontL, BottomBackL
    //public static GameObject[] rightObject = new GameObject[9];// topMidR, topFrontR, topBackR, midR, frontR, backR, bottomMidR, bottomFrontR, BottomBackR;

    private GameObject leftAnchor;
    private GameObject rightAnchor;
    private GameObject Cam;

    public GameObject leftController;
    public GameObject rightController;
    public GameObject dragonHolder;

    private Rigidbody rb;

    public float flapForce;
    public float moveForce;
    public float glideForce;
    public float turnSpeed;
    private float glideEffectiveness;

    private bool leftGlide;
    private bool rightGlide;
    private bool cooldown = false;
    private bool leftTop, leftBotom, rightTop, rightBotom;
    public bool leftFlap, rightFlap;

    // Start is called before the first frame update
    void Start()
    {
        leftAnchor = GameObject.Find("LeftGestures");
        rightAnchor = GameObject.Find("RightGestures");
        rb = gameObject.GetComponent<Rigidbody>();
        Cam = Camera.main.gameObject;

        /*
        leftObject[8] = GameObject.Find("TopMidL");
        leftObject[2] = GameObject.Find("BottomMidL");
        rightObject[8] = GameObject.Find("TopMidR");
        rightObject[2] = GameObject.Find("BottomMidR");
        LinkGestures(8, true);
        LinkGestures(2, true);
        LinkGestures(8, false);
        LinkGestures(2, false);
        */
    }

    // Update is called once per frame
    void Update()
    {

        if (leftAnchor.transform.position.y - leftController.transform.position.y < 0.2f && leftAnchor.transform.position.y - leftController.transform.position.y > -0.2f)
        {
            leftGlide = true;
            glideEffectiveness = Mathf.Abs((rightAnchor.transform.position.y - rightController.transform.position.y) + (leftAnchor.transform.position.y - leftController.transform.position.y));
        }
        else
        {
            leftGlide = false;
            glideEffectiveness = 0;
        }

        if (rightAnchor.transform.position.y - rightController.transform.position.y < 0.2f && rightAnchor.transform.position.y - rightController.transform.position.y > -0.2f)
        {
            rightGlide = true;
            glideEffectiveness = Mathf.Abs((rightAnchor.transform.position.y - rightController.transform.position.y) + (leftAnchor.transform.position.y - leftController.transform.position.y));
        }
        else
        {
            rightGlide = false;
            glideEffectiveness = 0;
        }

        if (leftGlide && rightGlide)
            Glide();


        if (leftAnchor.transform.position.y - leftController.transform.position.y < -0.3f)
            StartCoroutine(LeftTopFlapping());

        if (rightAnchor.transform.position.y - rightController.transform.position.y < -0.3f)
            StartCoroutine(RightTopFlapping());

        if (!cooldown)
        {
            if (leftAnchor.transform.position.y - leftController.transform.position.y > 0.3f)
                leftBotom = true;
            else
                leftBotom = false;

            if (rightAnchor.transform.position.y - rightController.transform.position.y > 0.3f)
                rightBotom = true;
            else
                rightBotom = false;

            if (leftTop && leftBotom)
                StartCoroutine(LeftFlapTime());

            if (rightTop && rightBotom)
                StartCoroutine(RightFlapTime());

            if (leftFlap && rightFlap)
                Flap();
        }
    }

    void Glide()
    {
        rb.velocity = new Vector3(rb.velocity.x, Mathf.Clamp(rb.velocity.y + (-0.4f + glideEffectiveness), -0.1f, 50), rb.velocity.z);
        rb.velocity += (Cam.transform.rotation * Vector3.forward) * glideForce;
        dragonHolder.transform.rotation = Quaternion.LerpUnclamped(dragonHolder.transform.rotation, Cam.transform.rotation, turnSpeed);
    }

    void Flap()
    {
        rb.velocity = new Vector3(rb.velocity.x, rb.velocity.y + flapForce, rb.velocity.z);
        rb.velocity += (Cam.transform.rotation * Vector3.forward) * moveForce;//Vector3.forward * moveForce;//
        leftFlap = false;
        rightFlap = false;
        StartCoroutine(CooldownTimer());
    }

    IEnumerator CooldownTimer()
    {
        cooldown = true;
        yield return new WaitForSeconds(0.7f);
        cooldown = false;
    }
    IEnumerator LeftTopFlapping()
    {
        leftTop = true;
        yield return new WaitForSeconds(0.7f);
        leftTop = false;
    }
    IEnumerator RightTopFlapping()
    {
        rightTop = true;
        yield return new WaitForSeconds(0.7f);
        rightTop = false;
    }

    IEnumerator LeftFlapTime()
    {
        leftFlap = true;
        yield return new WaitForSeconds(0.5f);
        leftFlap = false;
    }
    IEnumerator RightFlapTime()
    {
        rightFlap = true;
        yield return new WaitForSeconds(0.5f);
        rightFlap = false;
    }
    /*

    void LinkGestures(int objnumber, bool isleft)
    {
        int number = objnumber;
        if (!isleft) 
        {
            number += 8;
            gestures[number].parrent = rightObject[objnumber];
        }
        else
            gestures[number].parrent = leftObject[objnumber];
        gestures[number].id = new char[1];
        gestures[number].id[0] = objnumber.ToString()[0];
        gestures[number].isleft = isleft;

        Debug.Log(gestures[number].parrent);
        Debug.Log(string.Format("Gesture Succsessfully Linked, GestureID: {0} GestureIsLeft: {1}", gestures[number].id, gestures[number].isleft));
    }

    public struct Gesture
    {
        public char[] id;
        public bool isleft;
        public bool isHit;
        public float timeHit;
        public GameObject parrent;
    }
    */
}
