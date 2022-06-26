using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

public class MenuController : MonoBehaviour
{
    public static MenuController instance;

    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject calibrateMenu;

    [SerializeField] private GameObject controllerR;
    private LineRenderer line;

    private string previousName;
    private float progress;

    // Start is called before the first frame update
    void Start() {
        instance = this;
        line = controllerR.GetComponent<LineRenderer>();
    }

    // Update is called once per frame
    void Update() {
        // Shoot ray forward to the camera
        RaycastHit hit;
        if(Physics.Raycast(controllerR.transform.position, controllerR.transform.forward, out hit, 15f)) {
            if (previousName != hit.transform.name)
                progress = 0;

            previousName = hit.transform.name;
            progress += Time.deltaTime;
            if(progress >= 1) {
                switch (hit.transform.name) {
                    case "lqscene":
                        SceneManager.LoadScene("LowQualityIsland");
                        break;
                    case "hqscene":
                        SceneManager.LoadScene("Island");
                        break;
                    case "call":
                        toggleMenu();
                        break;
                    case "cancel":
                        toggleMenu();
                        break;
                }
                progress = 0;
            }
        }

        // Set the correct values in the line renderer
        line.SetPosition(0, controllerR.transform.position);
        line.SetPosition(1, controllerR.transform.position + controllerR.transform.forward * 10f);
    }

    public void toggleMenu() {
        if (mainMenu.activeInHierarchy) {
            mainMenu.SetActive(false);
            calibrateMenu.SetActive(true);
        } 
        else {
            mainMenu.SetActive(true);
            calibrateMenu.SetActive(false);
        }
    }
}
