using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System;
using System.IO;

public class ZEDClass : IZedWrapper
{

    // Implement DLL
    [DllImport(@"Assets\Libaries\zed_mapper.dll")]
    static public extern IntPtr CreateZEDMapper([MarshalAs(UnmanagedType.LPStr)] string strPath, int zed_flag);

    [DllImport(@"Assets\Libaries\zed_mapper.dll")]
    static public extern void start(IntPtr pObject);

    [DllImport(@"Assets\Libaries\zed_mapper.dll")]
    static public extern void restart(IntPtr pObject);

    [DllImport(@"Assets\Libaries\zed_mapper.dll")]
    static public extern void DeleteZEDMapper(IntPtr pObject);

    [DllImport(@"Assets\Libaries\zed_mapper.dll", CharSet = CharSet.Ansi)]
    static public extern ulong getFrame(IntPtr pObject, out IntPtr buffer);

    [DllImport(@"Assets\Libaries\zed_mapper.dll")]
    static public extern IntPtr getPose(IntPtr pObject);

    [DllImport(@"Assets\Libaries\zed_mapper.dll")]
    static public extern int isMeshAvailable(IntPtr pObject);

    [DllImport(@"Assets\Libaries\zed_mapper.dll")]
    static public extern IntPtr getMesh(IntPtr pObject, out IntPtr buffer_vertices, out IntPtr buffer_triangles, int mesh_flag);

    [DllImport(@"Assets\Libaries\zed_mapper.dll")]
    static public extern IntPtr getTexturedMesh(IntPtr pObject, out IntPtr buffer_vertices, out IntPtr buffer_triangles);

    [DllImport(@"Assets\Libaries\zed_mapper.dll")]
    static public extern int isMeshUpdateAvailable(IntPtr pObject);

    [DllImport(@"Assets\Libaries\zed_mapper.dll")]
    static public extern IntPtr getMeshUpdate(IntPtr pObject, out IntPtr buffer_chunks, out IntPtr buffer_vertices, out IntPtr buffer_triangles, int mesh_flag);

    [DllImport(@"Assets\Libaries\zed_mapper.dll")]
    static public extern int getHeight(IntPtr pObject);

    [DllImport(@"Assets\Libaries\zed_mapper.dll")]
    static public extern int getWidth(IntPtr pObject);
    
    private IntPtr ZEDMapper;
    private int zed_flag;

    // Texture for live rendering
    private Texture2D tex;
    private Pose new_pose;
    private int mesh_flag;

    protected FrameLoader frameLoader;
    protected RawZEDMeshLoader loader;
    protected UpdateZEDMeshLoader updateLoader;
    protected TexturedZEDMeshLoader texLoader;

    public ZEDClass(string path, ZEDWrapper wrapper, bool svo_real_time = false, bool BaseClass = false)
    {
        if (BaseClass)
            return;

        if (svo_real_time)
            zed_flag = zed_flag | 0x01;

        ZEDMapper = IntPtr.Zero;
        try
        {
            ZEDMapper = CreateZEDMapper(path, zed_flag);
        }
        catch (Exception e)
        {
            throw e;
        }

        //int testh = (Int32)getHeight(ZEDMapper);
        //int testw = (Int32)getWidth(ZEDMapper);
        tex = new Texture2D(1280, 720, TextureFormat.BGRA32, false);//new Texture2D(1280*2, 720, TextureFormat.RGBAFloat,true);
        new_pose = new Pose();

        //loader = new RawZEDMeshLoader();
        //loader.mesh = new Mesh();
        //loader.zed = this;
        mesh_flag = 0;

        frameLoader = new FrameLoader();
        frameLoader.zed = this;
        frameLoader.tex = tex;

        updateLoader = new UpdateZEDMeshLoader();
        updateLoader.meshUpdates = new List<MeshUpdate>(2000);// new MeshUpdate[0];
        updateLoader.zed = this;
        updateLoader.wrapper = wrapper;

        //texLoader = new TexturedZEDMeshLoader();
        //texLoader.mesh = new Mesh();
        //texLoader.zed = this;
    }


public virtual void Start(bool filter_mesh = false)
    {
        if (filter_mesh)
            mesh_flag = mesh_flag | 0x01;
        start(ZEDMapper);
    }

    public virtual void Restart()
    {
        restart(ZEDMapper);
    }

    public virtual void Delete()
    {
        //loader.WaitFor();
        //texLoader.WaitFor();
        updateLoader.WaitFor();
        DeleteZEDMapper(ZEDMapper);
    }

    public virtual Texture2D getFrame()
    {
        // Get image from ZED
        IntPtr buffer = IntPtr.Zero;
        Int32 size = (Int32)getFrame(ZEDMapper, out buffer);

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

    public virtual Pose getPose()
    {
        // Get Pose
        IntPtr buffer = IntPtr.Zero;
        buffer = getPose(ZEDMapper);
        float[] pose = new float[8];
        Marshal.Copy(buffer, pose, 0, 8);

        // Apply on Object
        new_pose.state = (int)pose[0];
        new_pose.position = new Vector3(pose[1], pose[2], pose[3]);
        new_pose.rotation = new Quaternion(pose[4], pose[5], pose[6], pose[7]);

        return new_pose;
    }

    public virtual Mesh getMesh()
    {
        // Call funcition
        IntPtr buffer = IntPtr.Zero;
        IntPtr buffer_vertices = IntPtr.Zero;
        IntPtr buffer_triangles = IntPtr.Zero;
        buffer = getMesh(ZEDMapper, out buffer_vertices, out buffer_triangles, mesh_flag);

        // Copy sizes
        Int32[] sizes = new Int32[2];
        Marshal.Copy(buffer, sizes, 0, 2);

        // Copy vertices
        Vector3[] vertices = new Vector3[sizes[0]];
        float[] data = new float[sizes[0] * 3];
        Marshal.Copy(buffer_vertices, data, 0, sizes[0] * 3);
        for (int j = 0; j < sizes[0] * 3; j += 3)
        {
            vertices[(int)j / 3].Set(data[j], data[j + 1], data[j + 2]);
        }

        // Copy triangles
        int[] triangles = new int[sizes[1] * 3];
        Marshal.Copy(buffer_triangles, triangles, 0, sizes[1] * 3);

        Mesh mesh = new Mesh();

        //GetComponent<MeshFilter>().mesh = mesh;
        mesh.vertices = vertices;
        mesh.triangles = triangles;

        return mesh;
    }

    /******************************************************************************
    *  Get Frame data handler for async calls to get one frame
    * ***************************************************************************/

    

    public virtual void getFrameForThread(ref byte[] frame)
    {
        // Get image from ZED
        IntPtr buffer = IntPtr.Zero;
        Int32 size = (Int32)getFrame(ZEDMapper, out buffer);

        // Framegrabbing successful?
        //if (size == -1)
        //    return null;

        //byte[] image = new byte[size];// +(180**1280*4)];
        Marshal.Copy(buffer, frame, 0, size);
    }

    public virtual void requestFrame()
    {
        // Get Frame
        if (frameLoader.IsRunning == false)
        {
            frameLoader.Start();
        }
    }

    public virtual Texture2D getFrameAsync()
    {
        frameLoader.Update();
        return frameLoader.tex;
    }

    public virtual bool frameRequestState()
    {
        return frameLoader.IsDone;
    }

    /*public int isFrameAvailable()
    {
        return isMeshAvailable(ZEDMapper);
    }*/

    /******************************************************************************
    *  Mesh data handler for async calls to get whole mesh
    * ***************************************************************************/

    public virtual void getMeshForThread(ref Vector3[] vertices, ref int[] triangles)
    {
        //List<Vector3> list_vertices = new List<Vector3>();
        // Call funcition
        IntPtr buffer = IntPtr.Zero;
        IntPtr buffer_vertices = IntPtr.Zero;
        IntPtr buffer_triangles = IntPtr.Zero;
        buffer = getMesh(ZEDMapper, out buffer_vertices, out buffer_triangles, mesh_flag);

        // Copy sizes
        Int32[] sizes = new Int32[2];
        Marshal.Copy(buffer, sizes, 0, 2);

        // Copy vertices
        vertices = new Vector3[sizes[0]];
        float[] data = new float[sizes[0] * 3];
        Marshal.Copy(buffer_vertices, data, 0, sizes[0] * 3);
        for (int j = 0; j < sizes[0] * 3; j += 3)
        {
            vertices[(int)j / 3].Set(data[j], data[j + 1], data[j + 2]);
        }

        // Copy triangles
        triangles = new int[sizes[1] * 3];
        Marshal.Copy(buffer_triangles, triangles, 0, sizes[1] * 3);

        /* Skript for updating
        // Copy sizes
        Int32[] sizes = new Int32[2];
        Marshal.Copy(buffer, sizes, 0, 2);

        Vector3[] tmp = (Vector3[])vertices.Clone();

        vertices = new Vector3[sizes[0]+tmp.Length];

        int i;
        for (i = 0; i < tmp.Length; i++)
            vertices[i] = tmp[i];

        float[] data = new float[sizes[0] * 3];
        Marshal.Copy(buffer_vertices, data, 0, sizes[0] * 3);
        for (int j = 0; j < sizes[0] * 3; j += 3)
        {
            vertices[((int)j / 3)+i].Set(data[j], data[j + 1], data[j + 2]);
        }

        // Copy triangles
        int[] tmp_tr = (int[])triangles.Clone();
        triangles = new int[(sizes[1]*3) + tmp_tr.Length];
        for (i = 0; i < tmp_tr.Length; i++)
            triangles[i] = tmp_tr[i];

        // Update triangles
        Marshal.Copy(buffer_triangles, triangles, i, sizes[1] * 3); */
    }

    public virtual void requestMesh()
    {
        // Get Mesh
        if (isMeshAvailable() == 1 && loader.IsRunning == false)
        {
            loader.Start();
        }
    }

    public virtual Mesh getMeshAsync()
    {
        loader.Update();
        return loader.mesh;
    }

    public virtual bool meshRequestState()
    {
        return loader.IsDone;
    }

    public virtual int isMeshAvailable()
    {
        return isMeshAvailable(ZEDMapper);
    }

    /******************************************************************************
    *  Mesh data handler for async calls to get mesh chunks
    * ***************************************************************************/

    public virtual void getUpdateMeshForThread(ref Int32[] chunks, ref Vector3[] vertices, ref int[] triangles)
    {
        // List<Vector3> list_vertices = new List<Vector3>();
        // Call funcition
        IntPtr buffer = IntPtr.Zero;
        IntPtr buffer_chunks = IntPtr.Zero;
        IntPtr buffer_vertices = IntPtr.Zero;
        IntPtr buffer_triangles = IntPtr.Zero;
        buffer = getMeshUpdate(ZEDMapper, out buffer_chunks, out buffer_vertices, out buffer_triangles, mesh_flag);

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

    public virtual void requestUpdatedMesh()
    {
        // Get Mesh
        if (isMeshUpdateAvailable() == 1 && updateLoader.IsRunning == false)
        {
            updateLoader.Start();
        }
    }

    public virtual List<MeshUpdate> getUpdateMeshAsync() 
    {
        updateLoader.Update();
        try { updateLoader.Update(); }
        catch (Exception e) { throw e; }
        return updateLoader.meshUpdates;
    }

    public virtual bool updateMeshRequestState()
    {
        return updateLoader.IsDone;
    }

    public virtual int isMeshUpdateAvailable()
    {
        return isMeshUpdateAvailable(ZEDMapper);
    }

    /******************************************************************************
    *  Mesh data handler for getting whole textured mesh - Not Working - TBD
    * ***************************************************************************/

    public void getTexturedMeshForThread()
    {
        IntPtr buffer_vertices = IntPtr.Zero;
        IntPtr buffer_triangles = IntPtr.Zero;

        getTexturedMesh(ZEDMapper, out buffer_vertices, out buffer_triangles);
    }

    public void requestTexturedMesh()
    {
        // Get Mesh
        if (texLoader.IsRunning == false)
        {
            texLoader.Start();
        }
    }

    public Mesh getTexturedMeshAsync()
    {
        texLoader.Update();
        return texLoader.mesh;
    }

    public bool texturedMeshRequestState()
    {
        return texLoader.IsDone;
    }
}

public class ThreadedJobing
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

public class FrameLoader : ThreadedJobing
{
    public ZEDClass zed;  // arbitary job data
    public Texture2D tex;
    public byte[] frame = new byte[3686400];

    protected override void ThreadFunction()
    {
        // Call funcition
        zed.getFrameForThread(ref frame);
    }
    protected override void OnFinished()
    {
        // Load image in texture
        tex.LoadRawTextureData(frame);
        tex.Apply();
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

public class RawZEDMeshLoader : ThreadedJobing
{
    public ZEDClass zed;  // arbitary job data
    public Mesh mesh; // arbitary job data
    Vector3[] vertices = new Vector3[0];
    int[] triangles = new int[0];

    protected override void ThreadFunction()
    {
        // Call funcition
        zed.getMeshForThread(ref vertices, ref triangles);
    }
    protected override void OnFinished()
    {
        Vector3[] tmp = mesh.vertices;
        try
        {
            mesh.Clear();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            IsQueueEmpty = true;
        }
        catch(Exception e)
        {
            throw new Exception(e.Message, e);
            //string s = "Size_ver: " + vertices.Length + " Size_tri: " + triangles.Length + " " + e.Message;
        }
    }
}

public class UpdateZEDMeshLoader : ThreadedJobing
{
    public ZEDClass zed;  // arbitary job data
    public ZEDWrapper wrapper;
    public List<MeshUpdate> meshUpdates;// = new MeshUpdate[1000]; // arbitary job data
    List<MeshUpdate> meshUpdatesSwap = new List<MeshUpdate>(20000); // arbitary job data
    Int32[] chunks = new Int32[0];
    Vector3[] vertices = new Vector3[0];
    int[] triangles = new int[0];


    List<Vector3> tmpVer = new List<Vector3>(65000);//[65000];
    List<int> tmpTri = new List<int>(65000);
    List<Vector3[]> list_ver = new List<Vector3[]>(2000);
    List<int[]> list_tri = new List<int[]>(2000);


    protected override void ThreadFunction()
    {
        int count_vertices = 0;
        int count_triangles = 0;

        // Call funcition
        zed.getUpdateMeshForThread(ref chunks, ref vertices, ref triangles);

        //tmpVer.Clear();
        //tmpTri.Clear();

        //System.GC.Collect();
        //System.GC.WaitForPendingFinalizers();
        //tmpVer.Coip
        tmpVer.InsertRange(0,vertices);
        tmpTri.InsertRange(0,triangles);
        
        for (int i = 0; i < chunks[1]; i++)
        {
            list_ver.Insert(i, tmpVer.GetRange(count_vertices, chunks[(i * 3) + 3]).ToArray());
            count_vertices += chunks[(i * 3) + 3];
            list_tri.Insert(i,tmpTri.GetRange(count_triangles, chunks[(i * 3) + 4] * 3).ToArray());
            count_triangles += (chunks[(i * 3) + 4] * 3);
        }
    }
    protected override void OnFinished()
    {
        //Vector3[] tmp = mesh.vertices;
        try
        {
            //wrapper.StartCoroutine("CopyMesh");
            //int count_vertices = 0;
            //int count_triangles = 0;
            
            for (int i = 0; i < chunks[1]; i++)
            {
                if(meshUpdatesSwap.Count <= chunks[(i * 3) + 2])
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
                //meshUpdatesSwap[i].Mesh.vertices = tmpVer.GetRange(count_vertices, chunks[(i * 3) + 3]).ToArray();
                meshUpdatesSwap[i].Mesh.vertices = list_ver[i];
                //meshUpdatesSwap[i].Mesh.colors = new Color[chunks[(i * 3) + 3]];
                //count_vertices += chunks[(i * 3) + 3];
                //int[] TmpTri = new int[chunks[(i * 3) + 4] * 3];
                //Array.Copy(triangles, count_triangles, TmpTri, 0, chunks[(i * 3) + 4] * 3);
                //meshUpdatesSwap[i].Mesh.triangles = tmpTri.GetRange(count_triangles, chunks[(i * 3) + 4] * 3).ToArray();
                meshUpdatesSwap[i].Mesh.triangles = list_tri[i];
                //count_triangles += (chunks[(i * 3) + 4] * 3);
                
            }

            meshUpdates = meshUpdatesSwap;//.ToArray();
            IsQueueEmpty = true;

            /*
            int queueLen = 100;
            if(indices.Count <= queueLen)
            {
                queueLen = indices.Count;
                IsQueueEmpty = true;
            }
            meshUpdates = new MeshUpdate[queueLen];
            for (int i = 0; i < queueLen; i++)
            {
                meshUpdates[i] = new MeshUpdate();
                meshUpdates[i].MeshId = indices.Dequeue();//[i];
                meshUpdates[i].Mesh.vertices = verToMesh.Dequeue();//[i];
                meshUpdates[i].Mesh.triangles = triToMes.Dequeue();//[i];
            }*/
        }
        catch (Exception e)
        {

            string s = "Size_ver: " + vertices.Length + " Size_tri: " + triangles.Length + " " + e.Message;
            IsQueueEmpty = true;
            throw new Exception(s);
        }
    }

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
    }
}

public class TexturedZEDMeshLoader : ThreadedJobing
{
    public ZEDClass zed;  // arbitary job data
    public Mesh mesh; // arbitary job data
    //Vector3[] vertices;
    //int[] triangles;

    protected override void ThreadFunction()
    {
        // Call funcition
        zed.getTexturedMeshForThread();
    }
    protected override void OnFinished()
    {
       mesh.Clear();
        IsQueueEmpty = true;
        //mesh.vertices = vertices;
        //mesh.triangles = triangles;
    }
}

public class MeshUpdate
{
    int meshId;
    Mesh mesh;
    //List<Color> colors = new List<Color>(65000);

    public int MeshId
    {
        get
        {
            return meshId;
        }

        set
        {
            meshId = value;
        }
    }

    public Mesh Mesh
    {
        get
        {
            return mesh;
        }

        set
        {
            mesh = value;
        }
    }

    public MeshUpdate()
    {
        meshId = new int();
        mesh = new Mesh();
    }
}
