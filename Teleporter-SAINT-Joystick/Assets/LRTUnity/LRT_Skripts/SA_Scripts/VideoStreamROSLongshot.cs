using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VideoStreamROSLongshot : MonoBehaviour
{
    public bool enableStream = true;
    public Texture2D targetTexture2D;
    public RawImage targetRawImage;

    private byte[] image_raw_data;
    private Texture2D image_texture;
    private bool image_received = false;
    // Use this for initialization
    void Start()
    {
        image_texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
    }

    // Update is called once per frame
    void Update()
    {
        if (enableStream && image_received)
        {
            image_texture.LoadImage(image_raw_data);
            image_texture.Apply();

            if (targetTexture2D != null)
                targetTexture2D = image_texture;
            if (targetRawImage != null)
                targetRawImage.texture = (Texture)image_texture;

            image_received = false;
        }
    }

    public void SetImage(byte[] rawData)
    {
        if (enableStream)
        {
            image_raw_data = rawData;
            image_received = true;
        }
    }
}