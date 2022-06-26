using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColliderTracker : MonoBehaviour
{

    public GameObject parrent;

    // Start is called before the first frame update
    void Start()
    {
        //parrent = GetComponentInParent<Transform>();
    }

    // Update is called once per frame
    void LateUpdate()
    {
        transform.position = parrent.transform.position;
    }
}
