using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaypointHandler : MonoBehaviour
{
    public LineRenderer line;

    public Material transparent, nonTransparent;
    public GameObject sphereObject;

    public RobotControlSAINT robotControl;
    public WaypointScreenHandler screenHandler;

    public GameObject XZxSlider;
    public GameObject XZzSlider;

    public RectTransform sphere;

    Vector3[] trajectoryArray, compareArray;

    // general UI elements
    public GameObject HUDSaint;

    // Waypointmarking UI elemnts
    public GameObject startWaypointMarking;
    public GameObject exitWaypointMarking;
    public GameObject sendWaypointMarking;

    // other elements
    string message;

    private Vector3 waypoint;
    public Vector3 Waypoint
    {
        get
        {
            return waypoint;
        }

        set
        {
            waypoint = value;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        Waypoint = sphere.position;
        //line.GetPositions(compareArray); 
        //color = sphereObject.GetComponent<Renderer>().material;
        //nonTransparent = new Color(color.r, color.g, color.b, 1.0f);
        //transparent = new Color(color.r, color.g, color.b, 0.0f);
    }

    // Update is called once per frame
    void Update()
    {
        message = robotControl.Msg;
        switch (message)
        {
            case "Waypoint Marking":
                StartWaypointMarking();
                break;
            case "Exit Waypoint Marking":
                ExitWaypointMarking();
                break;
        }
        //line.GetPositions(trajectoryArray);
        //if (!(trajectoryArray == compareArray))
        //{
        //    line.GetPositions(compareArray);
        //}
        sphere.position = Waypoint;
    }

    public void StartWaypointMarking()
    {
        sphereObject.GetComponent<Renderer>().material = nonTransparent;
        screenHandler.ActivateWaypointMarkingView();
        HUDSaint.gameObject.SetActive(false);
        exitWaypointMarking.gameObject.SetActive(true);
        sendWaypointMarking.gameObject.SetActive(true);
    }

    public void ExitWaypointMarking()
    {
        sphereObject.GetComponent<Renderer>().material = transparent;
        HUDSaint.gameObject.SetActive(true);
        exitWaypointMarking.gameObject.SetActive(false);
        sendWaypointMarking.gameObject.SetActive(false);
        screenHandler.DeactivateWaypointMarkingView();
    }

    public void SendWaypoint()
    {
        robotControl.WaypointMarking = Waypoint;
    }
}
