using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class VRMap
{
    public Transform vrTarget;
    public Transform rigTarget;
    public Vector3 trackingPositionOffset;
    public Vector3 trackingRotationOffset;

    public void map(float value)
    {
        rigTarget.position =  new Vector3(vrTarget.TransformPoint(trackingPositionOffset).x + value, vrTarget.TransformPoint(trackingPositionOffset).y, vrTarget.TransformPoint(trackingPositionOffset).z + value);
        rigTarget.rotation = vrTarget.rotation * Quaternion.Euler(trackingRotationOffset);
    }
}
public class VRRig : MonoBehaviour
{

    public float leftValue;
    public float rightValue;

    public VRMap head;
    public VRMap leftWing;
    public VRMap rightWing;

    public Transform headConstraint;
    public Vector3 headBodyOffset;


    // Start is called before the first frame update
    void Start()
    {
        //headBodyOffset = transform.position - headConstraint.position;
    }

    // Update is called once per frame
    void Update()
    {
        //  transform.position = headConstraint.position + headBodyOffset;
        //transform.forward = Vector3.ProjectOnPlane(headConstraint.up, Vector3.up).normalized;
        head.map(1);
        leftWing.map(leftValue);
        rightWing.map(rightValue);
    }
}
