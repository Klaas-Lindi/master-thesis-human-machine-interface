using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VideoStreamReceiver : MonoBehaviour
{
    public bool enableStream = true;
    public Texture2D targetTexture2D;
    public RawImage targetRawImage;

    // Interface to streaming or local zed operation
    private GStreamingClass gstreamer;

    // real time interval
    private float interval;

    // Use this for initialization
    void Start()
    {
        // Initialize zed class depending on settings
        try
        {
            if (enableStream)
            {
                gstreamer = new GStreamingClass();
                gstreamer.Start();
            }
        }
        catch (Exception e)
        {
            print(e.Message);
            return;
        }
        interval = Time.realtimeSinceStartup;
    }

    // Update is called once per frame
    void Update()
    {
        if (enableStream)
        {
            // Get current frame and set it as texture
            gstreamer.requestFrame();

            if (gstreamer.frameRequestState())
            {
                if(targetTexture2D != null)
                    targetTexture2D = gstreamer.getFrameAsync();
                if (targetRawImage != null)
                    targetRawImage.texture = (Texture)gstreamer.getFrameAsync();
            }
        }
    }

    void OnApplicationQuit()
    {
        if (gstreamer != null)
            gstreamer.Delete();
    }
}

