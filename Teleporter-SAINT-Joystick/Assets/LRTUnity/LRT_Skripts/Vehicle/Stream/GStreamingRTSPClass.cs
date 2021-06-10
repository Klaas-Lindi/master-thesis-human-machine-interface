using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System;
using System.IO;

public class GStreamingRTSPClass
{

    // Implement DLL
    [DllImport(@"Assets\Libaries\gstreamer_rtsp.dll")]
    private static extern IntPtr CreateGstreamerRtsp();

    [DllImport(@"Assets\Libaries\gstreamer_rtsp.dll")]
    static public extern void StartGstreamerRtsp(IntPtr pObject);

    [DllImport(@"Assets\Libaries\gstreamer_rtsp.dll")]
    static public extern void RestartGstreamerRtsp(IntPtr pObject);

    [DllImport(@"Assets\Libaries\gstreamer_rtsp.dll")]
    static public extern void DeleteGstreamerRtsp(IntPtr pObject);

    [DllImport(@"Assets\Libaries\gstreamer_rtsp.dll", CharSet = CharSet.Ansi)]
    static public extern ulong GetFrameGstreamerRtsp(IntPtr pObject, out IntPtr buffer);

    [DllImport("kernel32")]
    private static extern bool AllocConsole();

    [DllImport("kernel32")]
    private static extern bool FreeConsole();

    // dll handler variables
    private IntPtr Gstreamer;
    private IntPtr bufferFrame;
    private int gstreamer_flag;

    // Loader with own thread
    GStreamingRTSPFrameLoader frameLoader;

    // zed properties
    private Texture2D tex;
    private Pose new_pose;

    public GStreamingRTSPClass()
    {
        // Get ZEDStreamer
        Gstreamer = IntPtr.Zero;
        bufferFrame = IntPtr.Zero;
        try
        {
            Gstreamer = CreateGstreamerRtsp();
        }
        catch (Exception e)
        {
            throw e;
        }

        // Initilize texture
        tex = new Texture2D(1280, 720, TextureFormat.RGBA32, false);

        // Initialize thread loader for frame
        frameLoader = new GStreamingRTSPFrameLoader();
        frameLoader.gstreamer = this;
        frameLoader.tex = tex;
    }

    // Start streaming
    public void Start(bool filter_mesh = false)
    {
        StartGstreamerRtsp(Gstreamer);
    }

    // Restart streaming
    public void Restart()
    {
        RestartGstreamerRtsp(Gstreamer);
    }

    // Cancel all threads and delte streaming instance
    public void Delete()
    {
        try
        {
            frameLoader.WaitFor();
            DeleteGstreamerRtsp(Gstreamer);
        }
        catch (Exception e)
        {
            throw e;
        }
    }

    // Routine to get frame from stream
    public Texture2D getFrame()
    {
        // Get image from ZED
        IntPtr buffer = IntPtr.Zero;
        Int32 size = (Int32)GetFrameGstreamerRtsp(Gstreamer, out buffer);

        // Framegrabbing successful?
        if (size == -1)
            return null;

        byte[] image = new byte[size];// +(180**1280*4)];
        Marshal.Copy(buffer, image, 0, size);

        // Load image in texture
        tex.LoadRawTextureData(image);
        tex.Apply();

        return tex;
    }

    /******************************************************************************
    *  Get Frame data handler for async calls to get one frame
    * ***************************************************************************/

    public int getFrameForThread(ref byte[] frame)
    {
        // Get image from ZED
        IntPtr buffer = IntPtr.Zero;
        Int32 size = (Int32)GetFrameGstreamerRtsp(Gstreamer, out buffer);
        if ((size > 0) && (buffer != IntPtr.Zero))
        {
            Marshal.Copy(buffer, frame, 0, size);
        }
        /*if (buffer != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(buffer);
        }*/
        return size;
    }

    public void requestFrame()
    {
        // Get Frame
        if (frameLoader.IsRunning == false)
        {
            frameLoader.Start();
        }
    }

    public Texture2D getFrameAsync()
    {
        frameLoader.Update();
        return frameLoader.tex;
    }

    public bool frameRequestState()
    {
        return frameLoader.IsDone;
    }
}

/* 
public class StreamingThreadedJobing
{
    private bool m_IsDone = false;
    private bool m_IsRunning = false;
    private bool m_isQueueEmpty = false;
    private object m_Handle = new object();
    private System.Threading.Thread m_Thread = null;
    public bool IsDone
    {
        get
        {
            bool tmp;
            lock (m_Handle)
            {
                tmp = m_IsDone;
            }
            return tmp;
        }
        set
        {
            lock (m_Handle)
            {
                m_IsDone = value;
            }
        }
    }

    public bool IsRunning
    {
        get
        {
            bool tmp;
            lock (m_Handle)
            {
                tmp = m_IsRunning;
            }
            return tmp;
        }
        set
        {
            lock (m_Handle)
            {
                m_IsRunning = value;
            }
        }
    }

    public bool IsQueueEmpty
    {
        get
        {
            bool tmp;
            lock (m_Handle)
            {
                tmp = m_isQueueEmpty;
            }
            return tmp;
        }
        set
        {
            lock (m_Handle)
            {
                m_isQueueEmpty = value;
            }
        }
    }

    public virtual void Start()
    {
        m_Thread = new System.Threading.Thread(Run);
        m_Thread.Start();
        m_IsRunning = true;
        m_isQueueEmpty = false;

    }
    public virtual void Abort()
    {
        m_Thread.Abort();
    }

    protected virtual void ThreadFunction() { }

    protected virtual void OnFinished() { }

    public virtual bool Update()
    {
        if (IsDone)
        {
            try
            {
                OnFinished();
            }
            catch (Exception e)
            {
                IsRunning = false;
                IsDone = false;
                throw new Exception(e.Message);
            }
            if (IsQueueEmpty)
            {
                IsRunning = false;
                IsDone = false;
            }
            return true;
        }
        return false;
    }
    public IEnumerator WaitFor()
    {
        while (!Update())
        {
            yield return null;
        }
    }
    private void Run()
    {
        ThreadFunction();
        IsDone = true;
    }
}*/

public class GStreamingRTSPFrameLoader : StreamingThreadedJobing
{
    public GStreamingRTSPClass gstreamer;  // arbitary job data
    public Texture2D tex;
    private int result; 
    public byte[] frame = new byte[1280*720*4];

    protected override void ThreadFunction()
    {
        // Call funcition
        result = gstreamer.getFrameForThread(ref frame);
    }
    protected override void OnFinished()
    {
        // Load image in texture
        if (result != -1)
        {
            tex.LoadRawTextureData(frame);
            tex.Apply();
        }
    }

    public override bool Update()
    {
        if (IsDone)
        {
            OnFinished();
            IsRunning = false;
            IsDone = false;
            return true;
        }
        return false;
    }
}