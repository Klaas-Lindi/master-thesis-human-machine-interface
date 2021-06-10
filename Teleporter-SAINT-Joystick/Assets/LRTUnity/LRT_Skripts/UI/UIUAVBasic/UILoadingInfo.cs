using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UILoadingInfo : MonoBehaviour {

    public LoadPointCloud extPointCloud;
    public UavState vehicleState;
    public GameObject loadingInfoText;
    public GameObject loadingInfoImage;

	// Use this for initialization
	void Start () {
		
	}

    // Update is called once per frame
    void Update() {
        string infoText = "";
        if (extPointCloud != null)
        {
            if (extPointCloud.isReloading())
            {
                infoText = "Loading point cloud...";
            }
        }
        if (vehicleState != null)
        {
            if (!vehicleState.IsConnected)
            {
                infoText = "Connect to vehicle...";
            }
        }

        if (infoText.Length > 0)
        {
            loadingInfoText.GetComponent<Text>().text = infoText;
            loadingInfoImage.SetActive(true);
        }
        else
        {
            loadingInfoText.GetComponent<Text>().text = "";
            loadingInfoImage.SetActive(false);
        }
    }
}
