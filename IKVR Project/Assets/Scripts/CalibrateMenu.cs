using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CalibrateMenu : MonoBehaviour
{
    [SerializeField] private Transform leftTransform;
    [SerializeField] private Transform rightTransform;

    private float timer = 0;

    private void Update() {
        // Check if the left and right controller are more then 1 meter away
        if(leftTransform.position.x < -0.5f && rightTransform.position.x > 0.5f) {
            timer += Time.deltaTime;

            // If the player held this for 1 second set the glide height to the avarage height of the controllers
            if(timer >= 1) {
                float l = leftTransform.position.y;
                float r = rightTransform.position.y;
                PlayerPrefs.SetFloat("height", (l * r / 2) + 0.35f);
                MenuController.instance.toggleMenu();
            }
        }
        else {
            timer = 0;
        }
    }
}
