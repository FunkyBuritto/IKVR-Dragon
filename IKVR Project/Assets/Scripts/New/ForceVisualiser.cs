using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForceVisualiser : MonoBehaviour
{
    [SerializeField] private Rigidbody rb;
    [SerializeField] private LineRenderer line;
    [SerializeField] private float visualisationScale;

    // Update is called once per frame
    void Update() {
        line.SetPosition(1, rb.velocity / 10 * visualisationScale);
    }
}
