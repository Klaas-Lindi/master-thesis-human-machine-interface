using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class XYcamHandler : MonoBehaviour
{
    public GameObject XYcam;
    private Quaternion XYcamRotation;
    private Vector3 XYcamPosition;

    // Start is called before the first frame update
    void Start()
    {
        XYcamRotation = XYcam.transform.rotation;
        XYcamPosition = XYcam.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (!(XYcam.transform.rotation == XYcamRotation))
        {
            XYcam.transform.rotation = XYcamRotation;
        }

        if (!(XYcam.transform.position == XYcamPosition))
        {
            XYcam.transform.position = XYcamPosition;
        }
    }
}
