using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class XZcamHandler : MonoBehaviour
{
    public GameObject XZcam;
    private Quaternion XZcamRotation;
    private Vector3 XZcamPosition;

    // Start is called before the first frame update
    void Start()
    {
        XZcamRotation = XZcam.transform.rotation;
        XZcamPosition = XZcam.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (!(XZcam.transform.rotation == XZcamRotation))
        {
            XZcam.transform.rotation = XZcamRotation;
        }

        if (!(XZcam.transform.position == XZcamPosition))
        {
            XZcam.transform.position = XZcamPosition;
        }
    }
}
