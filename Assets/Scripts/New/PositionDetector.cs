using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider), typeof(Rigidbody))]
public class PositionDetector : MonoBehaviour
{
    public bool isLeft;
    public DetectorPlace place;

    private void OnTriggerEnter(Collider other) {
        if (other.CompareTag("GameController")) VRController.instance.addGesture(new Gesture(Time.time, place, isLeft));
    }
}
