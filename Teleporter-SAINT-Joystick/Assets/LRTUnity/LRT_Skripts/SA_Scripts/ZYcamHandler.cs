using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZYcamHandler : MonoBehaviour
{
    public GameObject ZYcam;
    private Quaternion ZYcamRotation;
    private Vector3 ZYcamPosition;

    // Start is called before the first frame update
    void Start()
    {
        ZYcamRotation = ZYcam.transform.rotation;
        ZYcamPosition = ZYcam.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if(!(ZYcam.transform.rotation == ZYcamRotation))
        {
            ZYcam.transform.rotation = ZYcamRotation;
        }

        if (!(ZYcam.transform.position == ZYcamPosition))
        {
            ZYcam.transform.position = ZYcamPosition;
        }
    }
}
