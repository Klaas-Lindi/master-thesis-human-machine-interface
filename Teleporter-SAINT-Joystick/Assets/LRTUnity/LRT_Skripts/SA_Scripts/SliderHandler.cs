using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SliderHandler : MonoBehaviour
{
    public Slider XYxSlider;
    public Slider XYySlider;
    public Slider XZxSlider;
    public Slider XZzSlider;
    public Slider ZYzSlider;
    public Slider ZYySlider;

    public WaypointHandler waypointHandler;

    Vector3 pos;

    void Start()
    {
        pos.x = waypointHandler.Waypoint[0];
        pos.y = waypointHandler.Waypoint[1];
        pos.z = waypointHandler.Waypoint[2];

        XYxSlider.value = waypointHandler.Waypoint[0];
        XYySlider.value = waypointHandler.Waypoint[1];
        XZxSlider.value = waypointHandler.Waypoint[0];
        XZzSlider.value = waypointHandler.Waypoint[2];
        ZYySlider.value = waypointHandler.Waypoint[1];
        ZYzSlider.value = waypointHandler.Waypoint[2];
    }

    public void XZxSliderHandle()
    {
        pos.x = XZxSlider.value;
        waypointHandler.Waypoint = pos;
        XYxSlider.value = waypointHandler.Waypoint[0];
    }

    public void XZzSliderHandle()
    {
        pos.z = XZzSlider.value;
        waypointHandler.Waypoint = pos;
        ZYzSlider.value = waypointHandler.Waypoint[2];
    }
    
    public void XYxSliderHandle()
    {
        pos.x = XYxSlider.value;
        waypointHandler.Waypoint = pos;
        XZxSlider.value = waypointHandler.Waypoint[0];
    }

    public void XYySliderHandle()
    {
        pos.y = XYySlider.value;
        waypointHandler.Waypoint = pos;
        ZYySlider.value = waypointHandler.Waypoint[1];
    }

    public void ZYySliderHandle()
    {
        pos.y = ZYySlider.value;
        waypointHandler.Waypoint = pos;
        XYySlider.value = waypointHandler.Waypoint[1];
    }

    public void ZYzSliderHandle()
    {
        pos.z = ZYzSlider.value;
        waypointHandler.Waypoint = pos;
        XZzSlider.value = waypointHandler.Waypoint[2];
    }
}

