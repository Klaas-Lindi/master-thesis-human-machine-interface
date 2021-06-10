
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using Valve.VR;
//using Valve.VR.InteractionSystem;

#if HMI_VR_ACTIVE
using HTC.UnityPlugin.Vive;

public class VRLaserpointer : MonoBehaviour
{
    [Tooltip("Object at the end of the ray to indicate end")]
    public GameObject LaserPointPrefab;
    private GameObject laserPoint;
    [Tooltip("Stretchable object to show the ray until end")]
    public GameObject LaserRayPrefab;
    private GameObject laserRay;
    [Tooltip("Minimum and maximum distance of the laser in [m]")]
    public Vector2 minmaxDistance = new Vector2(2, 25);
    [Tooltip("Thickness of shown objects in [m]")]
    public float thickness;

    // Controller device
    private float distance;
    private bool enableShow;
    private Vector3 point;

    private int controllerIndex;
    private SteamVR_TrackedObject controller;
    private SteamVR_Controller.Device device;

    /*
    public void init()
    {
        // Instatiate Prefabs
        laserPoint = Instantiate(LaserPointPrefab);
        laserRay = Instantiate(LaserRayPrefab);
    }*/

    // Start is called before the first frame update
    void Start()
    {
        laserPoint = Instantiate(LaserPointPrefab);
        laserRay = Instantiate(LaserRayPrefab);

        distance = (minmaxDistance.x + minmaxDistance.y) / 2;
        enableShow = false;

        controller = GetComponent<SteamVR_TrackedObject>();
        controllerIndex = (int)controller.index;
        device = SteamVR_Controller.Input(controllerIndex);

        //hand = this.GetComponent<Hand>();
    }

    // Update is called once per frame
    void Update()
    {
        //Vector2 touchVector = ViveInput.GetPadTouchAxisEx(HandRole.RightHand);
     
        if (enableShow)
        {
            RaycastHit hit;
            Ray raycast = new Ray(transform.position, transform.forward);

            bool bHit = Physics.Raycast(raycast, out hit);

            float length;

            if (bHit)
            {
                if(hit.distance > distance)
                {
                    point = Vector3.Lerp(controller.transform.position, hit.point, (distance/hit.distance));
                    length = distance;
                }
                else
                {
                    point = hit.point - Vector3.Normalize(hit.point - controller.transform.position);
                    length = hit.distance - 1;
                }
            }
            else
            {
                point = controller.transform.position + raycast.direction*distance;
                length = distance;
            }

            laserRay.transform.position = Vector3.Lerp(controller.transform.position, point, .5f);
            laserRay.transform.LookAt(point);
            laserRay.transform.localScale = new Vector3(thickness, thickness, length);

           //laserRay.transform.position = this.transform.position + raycast.direction * distance;
            laserPoint.transform.position = point;
        }
    }

    /// <summary>
    /// Change the distance of the ray
    /// </summary>
    /// <param name="precentage">value between 0-1.0 </param>
    public void changeDistance(float precentage)
    {
        if(precentage <= 1.0 && precentage >= 0.0)
        {
            distance = minmaxDistance.x + (precentage * (minmaxDistance.y - minmaxDistance.x));
        }
    }

    public void showDistance()
    {
        enableShow = true;
        laserPoint.SetActive(enableShow);
        laserRay.SetActive(enableShow);
    }

    public void hideDistance()
    {
        enableShow = false;
        laserPoint.SetActive(enableShow);
        laserRay.SetActive(enableShow);
    }

    public Vector3 getPoint()
    {
        return point;
    }


}
#endif