using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnvironmentHandling : MonoBehaviour {

    public UavState currentUavState;

    private GPS lastGPS;
    private LoadPointCloud loadPC;
    private GPSConverter gpsConverter;
    private bool fixGPS;

    // Use this for initialization
    void Start () {
        loadPC = this.GetComponentInChildren<LoadPointCloud>();
        gpsConverter = new GPSConverter("");
        lastGPS = currentUavState.Coordinates;
        fixGPS = false;
	}
	
	// Update is called once per frame
	void Update () {
		if((!fixGPS) && (lastGPS != currentUavState.Coordinates))
        {
            this.SetGPSPosition(currentUavState.Coordinates);
        }
	}

    public void SetGPSPosition(GPS position)
    {


    }
}
