using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System;
using System.IO;

public class ZEDStreamingClass : IZedWrapper
{

    // Implement DLL
    [DllImport(@"Assets\Libaries\zed_mapper_streamer.dll")]
    private static extern IntPtr CreateZEDMapperStreamer();

    [DllImport(@"Assets\Libaries\zed_mapper_streamer.dll")]
    static public extern void StartStreamer(IntPtr pObject);

    [DllImport(@"Assets\Libaries\zed_mapper_streamer.dll")]
    static public extern void RestartStreamer(IntPtr pObject);

    [DllImport(@"Assets\Libaries\zed_mapper_streamer.dll")]
    static public extern void DeleteZEDMapperStreamer(IntPtr pObject);

    [DllImport(@"Assets\Libaries\zed_mapper_streamer.dll", CharSet = CharSet.Ansi)]
    static public extern ulong GetFrameStreamer(IntPtr pObject, out IntPtr buffer);

    [DllImport(@"Assets\Libaries\zed_mapper_streamer.dll")]
    static public extern IntPtr GetPoseStreamer(IntPtr pObject);

    [DllImport(@"Assets\Libaries\zed_mapper_streamer.dll")]
    static public extern int IsMeshUpdateAvailableStreamer(IntPtr pObject);

    [DllImport(@"Assets\Libaries\zed_mapper_streamer.dll")]
    static public extern IntPtr GetMeshUpdateStreamer(IntPtr pObject, out IntPtr buffer_chunks, out IntPtr buffer_vertices, out IntPtr buffer_triangles);

    [DllImport("kernel32")]
    private static extern bool AllocConsole();

    [DllImport("kernel32")]
    private static extern bool FreeConsole();

    // dll handler variables
    private IntPtr ZEDMapper;
    private IntPtr bufferFrame;
    private int zed_flag;

    // Loader with own thread
    StreamingFrameLoader frameLoader;
    StreamingUpdateZEDMeshLoader updateLoader;

    // zed properties
    private Texture2D tex;
    private Pose new_pose;

    public ZEDStreamingClass(ZEDWrapper wrapper) 
    {
        // Get ZEDStreamer
        ZEDMapper = IntPtr.Zero;
        bufferFrame = IntPtr.Zero;
        try
        {
                ZEDMapper = CreateZEDMapperStreamer();
        }
        catch (Exception e)
        {
            throw e;
        }

        // Initilize texture
        tex = new Texture2D(1280, 720, TextureFormat.RGBA32, false);
        new_pose = new Pose();

        // Initialize thread loader for frame
        frameLoader = new StreamingFrameLoader();
        frameLoader.zed = this;
        frameLoader.tex = tex;

        // Initialize thread loder for mesh
        updateLoader = new StreamingUpdateZEDMeshLoader();
        updateLoader.meshUpdates = new List<MeshUpdate>(2000);// new MeshUpdate[0];
        updateLoader.zed = this;
        updateLoader.wrapper = wrapper;        
    }

    // Start streaming
    public void Start(bool filter_mesh = false)
    {
        StartStreamer(ZEDMapper);
    }

    // Restart streaming
    public void Restart()
    {
        RestartStreamer(ZEDMapper);
    }

    // Cancel all threads and delte streaming instance
    public void Delete()
    {
        try
        {
            frameLoader.WaitFor();
            updateLoader.WaitFor();
            DeleteZEDMapperStreamer(ZEDMapper);
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
        Int32 size = (Int32)GetFrameStreamer(ZEDMapper, out buffer);

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

    // Routine to get pose from stream
    public Pose getPose()
    {
        // Get Pose
        IntPtr buffer = IntPtr.Zero;
        buffer = GetPoseStreamer(ZEDMapper);
        float[] pose = new float[8];
        Marshal.Copy(buffer, pose, 0, 8);

        // Apply on Object
        new_pose.state = (int)pose[0];
        new_pose.position = new Vector3(pose[1], pose[2], pose[3]);
        new_pose.rotation = new Quaternion(pose[4], pose[5], pose[6], pose[7]);

        return new_pose;
    }

    /******************************************************************************
    *  Get Frame data handler for async calls to get one frame
    * ***************************************************************************/

    public int getFrameForThread(ref byte[] frame)
    {
        // Get image from ZED
        IntPtr buffer = IntPtr.Zero;
        Int32 size = (Int32)GetFrameStreamer(ZEDMapper, out buffer);
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

    /******************************************************************************
    *  Mesh data handler for async calls to get mesh chunks
    * ***************************************************************************/

    public void getUpdateMeshForThread(ref Int32[] chunks, ref Vector3[] vertices, ref int[] triangles)
    {
        // List<Vector3> list_vertices = new List<Vector3>();
        // Call funcition
        IntPtr buffer = IntPtr.Zero;
        IntPtr buffer_chunks = IntPtr.Zero;
        IntPtr buffer_vertices = IntPtr.Zero;
        IntPtr buffer_triangles = IntPtr.Zero;
        buffer = GetMeshUpdateStreamer(ZEDMapper, out buffer_chunks, out buffer_vertices, out buffer_triangles);

        // Copy sizes
        Int32[] sizes = new Int32[3];
        Marshal.Copy(buffer, sizes, 0, 3);

        // Copy chunks
        chunks = new Int32[sizes[0]];
        Marshal.Copy(buffer_chunks, chunks, 0, sizes[0]);

        // Copy vertices
        vertices = new Vector3[sizes[1]];
        float[] data = new float[sizes[1] * 3];
        Marshal.Copy(buffer_vertices, data, 0, sizes[1] * 3);
        for (int j = 0; j < sizes[1] * 3; j += 3)
        {
            vertices[(int)j / 3].Set(data[j], data[j + 1], data[j + 2]);
        }

        // Copy triangles
        triangles = new int[sizes[2] * 3];
        Marshal.Copy(buffer_triangles, triangles, 0, sizes[2] * 3);
    }

    public void requestUpdatedMesh()
    {
        // Get Mesh
        if (isMeshUpdateAvailable() == 1 && updateLoader.IsRunning == false)
        {
            updateLoader.Start();
        }
    }

    public List<MeshUpdate> getUpdateMeshAsync() 
    {
        //updateLoader.Update();
        try { updateLoader.Update(); }
        catch (Exception e) { throw e; }
        return updateLoader.meshUpdates;
    }

    public bool updateMeshRequestState()
    {
        return updateLoader.IsDone;
    }

    public int isMeshUpdateAvailable()
    {
        return IsMeshUpdateAvailableStreamer(ZEDMapper);
    }
}

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
}

public class StreamingFrameLoader : StreamingThreadedJobing
{
    public ZEDStreamingClass zed;  // arbitary job data
    public Texture2D tex;
    private int result; 
    public byte[] frame = new byte[1280*720*4];

    protected override void ThreadFunction()
    {
        // Call funcition
        result = zed.getFrameForThread(ref frame);
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

public class StreamingUpdateZEDMeshLoader : StreamingThreadedJobing
{
    public ZEDStreamingClass zed;  // arbitary job data
    public ZEDWrapper wrapper;
    public List<MeshUpdate> meshUpdates;// = new MeshUpdate[1000]; // arbitary job data
    List<MeshUpdate> meshUpdatesSwap = new List<MeshUpdate>(20000); // arbitary job data
    Int32[] chunks = new Int32[0];
    Vector3[] vertices = new Vector3[0];
    int[] triangles = new int[0];

    List<Vector3> tmpVer = new List<Vector3>(65000);//[65000];
    List<int> tmpTri = new List<int>(150000);
    List<Vector3[]> list_ver = new List<Vector3[]>(2000);
    List<int[]> list_tri = new List<int[]>(2000);

    protected override void ThreadFunction()
    {
        int count_vertices = 0;
        int count_triangles = 0;

        // Call funcition
        zed.getUpdateMeshForThread(ref chunks, ref vertices, ref triangles);

        tmpVer.InsertRange(0,vertices);
        tmpTri.InsertRange(0,triangles);
        list_ver.Clear();
        list_tri.Clear();
        
        for (int i = 0; (i*3)+1 < chunks.Length; i++)
        {
            list_ver.Insert(i, tmpVer.GetRange(count_vertices, chunks[(i * 3) + 2]).ToArray());
            count_vertices += chunks[(i * 3) + 2];
            list_tri.Insert(i,tmpTri.GetRange(count_triangles, chunks[(i * 3) + 3] * 3).ToArray());
            count_triangles += (chunks[(i * 3) + 3] * 3);
        }
    }
    protected override void OnFinished()
    {
        int i=0, j=0, k=0;
        bool meshValid = true;
        try
        {
            // Set Meshes
            for (i = 0; (i * 3) + 1 < chunks.Length; i++)
            {
                if (meshUpdatesSwap.Count <= chunks[(i * 3) + 1])
                {
                    meshUpdatesSwap.Add(new MeshUpdate());
                }

                for (j = 0; j < list_tri.Count; j++)
                {
                    for (k = 0; k < 3; k++)
                    {
                        if (list_tri[j][k] > list_ver.Count)
                        {
                            Debug.Log("ACCESS VIOLATION " + chunks[(i * 3) + 1] + ": " + j + "/" + list_tri.Count + " | Element: " + k + " Index: " + list_tri[j][k] + "/" + list_ver.Count);
                            meshValid = true;
                        }
                    }

                }

                meshUpdatesSwap[i].MeshId = chunks[(i * 3) + 1];
                meshUpdatesSwap[i].Mesh.Clear();
                if (meshValid)
                {
                    meshUpdatesSwap[i].Mesh.vertices = list_ver[i];
                    meshUpdatesSwap[i].Mesh.triangles = list_tri[i];
                }
                else // Workaroung: If mesh was not valid whole update will be canceled
                {
                    i = chunks.Length + 10;
                    meshValid = true;
                }            
            }

            // Swap Pointers with Meshes
            meshUpdates = meshUpdatesSwap;
            IsQueueEmpty = true;
        }
        catch (Exception e)
        {

            string s = "Mesh_ID: " + chunks[(i * 3) + 1] + " Size_ver: " + list_ver[i] + " Size_tri: " + list_tri[i] + " \n" + e.Message;
            IsQueueEmpty = true;
            throw new Exception(s);
        }
    }
    /*
    IEnumerator CopyMesh()
    {
        int count_vertices = 0;
        int count_triangles = 0;

        for (int i = 0; i < chunks[1]; i++)
        {
            if (meshUpdatesSwap.Count <= chunks[(i * 3) + 2])
            {
                meshUpdatesSwap.Add(new MeshUpdate());
            }
            //List<Vector3> tmpVer = new List<Vector3>(vertices);
            //List<int> tmpTri = new List<int>(triangles);

            //meshUpdates[i] = new MeshUpdate();
            meshUpdatesSwap[i].MeshId = chunks[(i * 3) + 2];
            meshUpdatesSwap[i].Mesh.Clear();
            //Vector3[] TmpVer = new Vector3[chunks[(i * 3) + 3]];
            //Array.Copy(vertices, count_vertices, tmpVer, 0, chunks[(i * 3) + 3]);
            //count_vertices += chunks[(i * 3) + 3];
            //meshUpdates[i].Mesh.vertices = (Vector3[])TmpVer.GetValue(0, chunks[(i * 3) + 3]);//[i];
            meshUpdatesSwap[i].Mesh.vertices = tmpVer.GetRange(count_vertices, chunks[(i * 3) + 3]).ToArray();
            //meshUpdatesSwap[i].Mesh.colors = new Color[chunks[(i * 3) + 3]];
            count_vertices += chunks[(i * 3) + 3];
            //int[] TmpTri = new int[chunks[(i * 3) + 4] * 3];
            //Array.Copy(triangles, count_triangles, TmpTri, 0, chunks[(i * 3) + 4] * 3);
            meshUpdatesSwap[i].Mesh.triangles = tmpTri.GetRange(count_triangles, chunks[(i * 3) + 4] * 3).ToArray();
            count_triangles += (chunks[(i * 3) + 4] * 3);

        }
        yield return null;
        meshUpdates = meshUpdatesSwap;//.ToArray();
        IsQueueEmpty = true;
    }*/
}