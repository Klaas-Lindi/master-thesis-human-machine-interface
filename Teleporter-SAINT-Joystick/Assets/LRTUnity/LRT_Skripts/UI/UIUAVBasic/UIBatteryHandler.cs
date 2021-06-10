using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIBatteryHandler : MonoBehaviour {

    public UavState uavState;

    public Text duration;
    public Slider slider;
    public Image sliderFill;
    public Image symbol;

    private float lastBattery = -1.0f;
    private float estimatedDuration = 0.0f;
    private float lastRealTime;
    // Use this for initialization
    void Start () {
        lastRealTime = Time.realtimeSinceStartup;
    }
	
	// Update is called once per frame
	void Update () {
        if (duration != null && slider != null && symbol != null)
        {
            if (lastBattery != uavState.Battery)
            {
                estimatedDuration = uavState.Battery * 24;
                lastBattery = uavState.Battery;
                if (uavState.Battery > 40f)
                {
                    Color green = new Color(0, 255/255, 86/255, 255/255);
                    duration.color = green;
                    sliderFill.color = green;
                    symbol.color = green;
                }
                if (uavState.Battery < 40f)
                {
                    duration.color = Color.yellow;
                    sliderFill.color = Color.yellow;
                    symbol.color = Color.yellow;
                }
                if (uavState.Battery < 20f)
                {
                    duration.color = Color.red;
                    sliderFill.color = Color.red;
                    symbol.color = Color.red;
                }
            }
            else
            {
                estimatedDuration -= (Time.realtimeSinceStartup - lastRealTime);
            }

            slider.value = uavState.Battery;
            if (uavState.Battery == 0 || !uavState.IsConnected)
            {
                duration.text = "No Battery";
            }
            else
            {
                duration.text = ((int)estimatedDuration / 60) + " min " + ((int)estimatedDuration % 60) + " s"; // string.Format("## min", time / 60) + string.Format(" ## s", (time % 60));
            }
            lastRealTime = Time.realtimeSinceStartup;
        }

    }
}
