using UnityEngine;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using System;

public class LiveVideoRender : MonoBehaviour {

    // Implement DLL
    [DllImport(@"Assets\Libaries\tvr-gstreamer.dll")]
    static public extern IntPtr CreateTVRComGstManager(ushort port);
    
    [DllImport(@"Assets\Libaries\tvr-gstreamer.dll")]
    static public extern void DeleteTVRComGstManager(IntPtr pObject);
    
    [DllImport(@"Assets\Libaries\tvr-gstreamer.dll", CharSet = CharSet.Ansi)]
    static public extern ulong getFrame(IntPtr pObject, out IntPtr buffer);
    
    private IntPtr TVRComGstManager;
    private Texture2D tex;

    public Boolean LiveVideoEnabled;
    // Use this for initialization
    void Start () {
        TVRComGstManager = CreateTVRComGstManager(5000);
        tex = new Texture2D(2, 2);
    }
	
	// Update is called once per frame
	void Update () {
        if (LiveVideoEnabled)
        {
            IntPtr buffer = IntPtr.Zero;
            ulong size = getFrame(TVRComGstManager, out buffer);
            byte[] image = new byte[size];
            Marshal.Copy(buffer, image, 0, (Int32)size);
            tex.LoadImage(image);
            gameObject.GetComponent<Renderer>().material.mainTexture = tex; // LoadPNG("C:/testtmp/frame2.png"); // LoadPNG(Application.dataPath + "/Images/test.jpg");
        }
    }

    public static Texture2D LoadPNG(string filePath)
    {

        Texture2D tex = null;
        byte[] fileData;


        Debug.Log(filePath);
        if (File.Exists(filePath))
        {
            fileData = File.ReadAllBytes(filePath);
            tex = new Texture2D(2, 2);
            tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
        }
        return tex;
    }

    void OnApplicationQuit()
    {
        DeleteTVRComGstManager(TVRComGstManager);
    }
}
