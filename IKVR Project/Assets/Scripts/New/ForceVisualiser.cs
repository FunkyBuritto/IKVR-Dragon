using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForceVisualiser : MonoBehaviour
{
    [SerializeField] private GameObject parrentOffset;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private LineRenderer line;
    [SerializeField] private float visualisationScale;

    // Update is called once per frame
    void Update() {
        line.SetPosition(1, rb.velocity / 10 * visualisationScale);
        parrentOffset.transform.rotation = new Quaternion(0, Camera.main.gameObject.transform.rotation.y, 0, 0);
    }
}
