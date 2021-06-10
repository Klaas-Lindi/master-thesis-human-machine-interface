using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticlePathViewer : MonoBehaviour {

    public float speed = 100.0f;

    private List<Vector3> waypoints = new List<Vector3>(100);

    public List<Vector3> Waypoints
    {
        get
        {
            return waypoints;
        }

        set
        {
            waypoints = value;
            if (waypoint_index > waypoints.Count)
                waypoint_index = 0;
            if (this.GetComponent<LineRenderer>() != null)
            {
                this.GetComponent<LineRenderer>().positionCount = waypoints.Count;
                this.GetComponent<LineRenderer>().SetPositions(value.ToArray());
            }
        }
    }

    private int waypoint_index;
    

    // Use this for initialization
    void Start () {
        Waypoints.Add(new Vector3(0, 0, 0));

        this.transform.position = Waypoints[0];
        waypoint_index = 0;
    }
	
	// Update is called once per frame
	void Update () {
        /*
        if (Vector3.Distance(Waypoints[waypoint_index],this.transform.position) < (speed / 100))
        {
            if (waypoint_index < Waypoints.Count-1)
            {
                waypoint_index++;
                this.transform.position = Waypoints[waypoint_index - 1];
            }
            else
            {
                waypoint_index = 0;
                this.transform.position = Waypoints[0];
            }
        }
        this.transform.position += Vector3.Normalize(Waypoints[waypoint_index] - this.transform.position) * (speed / 100);
        */
        if (Waypoints.Count != 0)
        {
            this.transform.position = Waypoints[waypoint_index];
            if (waypoint_index < Waypoints.Count - 1)
            {
                waypoint_index++;
                if(Vector3.Distance(Waypoints[waypoint_index], Waypoints[Waypoints.Count - 1]) < 0.5)
                {
                    waypoint_index = 0;
                    this.transform.position = Waypoints[0];
                }
                //this.transform.position = Waypoints[waypoint_index - 1];
            }
            else
            {
                waypoint_index = 0;
                this.transform.position = Waypoints[0];
            }
        }

    }

    public void RefreshPath()
    {
        waypoint_index = 0;
        this.transform.position = Waypoints[0];
    }

    public void ClearPath()
    {
        Waypoints.Clear();
        waypoint_index = 0;
        Waypoints.Add(new Vector3(0, 0, 0));
        this.transform.position = Waypoints[0];

        this.GetComponent<LineRenderer>().positionCount = 0;
        this.GetComponent<LineRenderer>().SetPositions(Waypoints.ToArray());
    }
}
