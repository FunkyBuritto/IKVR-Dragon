using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum DetectorPlace {
    Top,
    Middle,
    Bottom,
}

public class Gesture {
    public float activation;
    public DetectorPlace place;
    public bool isLeft;
    public Gesture(float a, DetectorPlace p, bool l) {
        activation = a;
        place = p;
        isLeft = l;
    }
}
