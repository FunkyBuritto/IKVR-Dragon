using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GestureIdentifier : MonoBehaviour
{

    private char[] objectname = new char[2];

    // Start is called before the first frame update
    void Start()
    {
        if (!gameObject.CompareTag("Gestures"))
            Debug.LogWarning(gameObject + " is not a Gesture");
        else
        {
            objectname = gameObject.name.ToCharArray();
            switch (objectname[1])
            {
                case '1':
                    break;
            }
        }


    }


}
