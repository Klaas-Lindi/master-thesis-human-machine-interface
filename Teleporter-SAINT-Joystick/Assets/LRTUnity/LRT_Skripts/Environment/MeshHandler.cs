using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The MeshHandler handels all dynmaically produced meshes, receive it post processed it etc.
/// </summary>
public class MeshHandler : MonoBehaviour {

    private MeshFilter currentMeshFilter;

    // List of all meshes in the Environment
    private List<GameObject> meshList = new List<GameObject>();
    // List of all colors of mesh in the Environment
    List<List<Color>> colorList = new List<List<Color>>();

    private int currentMeshNumber = 0;
    //  private List<Color> colors = new List<Color>(65000);
    private Color32 meshColor = new Color32(0, 244, 255, 255);
    private Shader meshShader;

    //private Queue<Pose> vehiclePose = new Queue<Pose>();
    //private Queue<Texture2D> frameTexture = new Queue<Texture2D>();

    private Pose uavPose = new Pose();
    private Texture2D frameTexture;// = new Texture2D();

    public bool wireFrame; 


    void Awake()
    {
        if (wireFrame)
        {
            meshShader = Shader.Find("UCLA Game Lab/Wireframe/Double-Sided Cutout");
        }
        else
        {
            meshShader = Shader.Find("Unlit/Test1");
        }
            //meshShader = Shader.Find("Custom/StandardVertex");
            //meshShader = Shader.Find("UCLA Game Lab/Wireframe/Double-Sided Cutout");
        }
    

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        /*foreach(GameObject go in meshList)
        {
            Mesh m = go.GetComponent<MeshFilter>().mesh;
            Color[] colors = new Color[m.vertices.Length];
            for (int i = 0; i < m.vertices.Length; i++)
            {
                Vector3 point = m.vertices[i];
                RaycastHit hit;

                if(Physics.Raycast(point, (point-VehiclePose.position).normalized, out hit))
                {
                    Vector3 pixelUV = hit.textureCoord;
                    pixelUV.x *= -FrameTexture.width;
                    pixelUV.y *= FrameTexture.height;
                    colors[i] = (Color)FrameTexture.GetPixel((int)pixelUV.x, (int)pixelUV.y);
                    //print(colors[i]);
                }
                else
                {
                    colors[i] = Color.grey;
                }
            }
            m.colors = colors;
        }*/
	}

    /// <summary>
    /// This function initilize the first mesh for the submesh given with subMeshName
    /// </summary>
    /// <param name="subMeshName">Name or identifire of the submeshes</param>
    internal void InitMesh(string subMeshName)
    {
        // Add new submesh as GameObject
        meshList.Add(new GameObject());
        colorList.Add(new List<Color>(65000));

        meshList[currentMeshNumber].AddComponent<MeshFilter>();
        meshList[currentMeshNumber].AddComponent<MeshRenderer>();

        // Create new mesh
        Mesh new_mesh = new Mesh();
        new_mesh.name = subMeshName + currentMeshNumber;

        // Add new mesh to filter
        currentMeshFilter = meshList[currentMeshNumber].GetComponent<MeshFilter>();
        currentMeshFilter.name = subMeshName + currentMeshNumber;
        currentMeshFilter.mesh = new_mesh;

        // Set Material, Parent and Color
        meshList[currentMeshNumber].GetComponent<MeshRenderer>().material.shader = meshShader;
        meshList[currentMeshNumber].GetComponent<MeshRenderer>().material.color = meshColor;
        meshList[currentMeshNumber].transform.parent = this.transform;
       
    }

    /// <summary>
    /// Sets the current mesh which is empty after initializing the submesh or adding an new one
    /// </summary>
    /// <param name="mesh">The mesh which should be setted</param>
    internal void setCurrentMesh(Mesh mesh)
    {
        currentMeshFilter.mesh = mesh;
        currentMeshFilter.mesh.RecalculateBounds();
    }

    /// <summary>
    /// Get the current mesh size
    /// </summary>
    public int CurrentMeshSize
    {
        get
        {
            return currentMeshFilter.mesh.vertices.Length;
        }
    }

    public int MeshCount
    {
        get
        {
            return currentMeshNumber;
        }
    }

    public Pose UavPose
    {
        set
        {
            //vehiclePose.Enqueue(value);
            uavPose = value;
        }

    }

    public Texture2D FrameTexture
    {
        set
        {
            //frameTexture.Enqueue(value);
            frameTexture = value;
        }
    }

    /// <summary>
    /// Sets the mesh with given id in meshupdate
    /// </summary>
    /// <param name="update">A MeshUpdate to update one mesh</param>
    internal void setMesh(MeshUpdate update)
    {
        /*if (meshList.Count <= update.MeshId)
        {
            int i = 0;
        }*/
        //colors.Clear();
        //meshList[update.MeshId].layer = LayerMask.NameToLayer("Test1");
        if (!wireFrame)
        {
            colorList[update.MeshId].Clear();
            //Color[] colors = new Color[update.Mesh.vertices.Length];
            //List<Color> colors = new List<Color>(update.Mesh.vertices.Length);
            //update.Mesh.colors = new Color[update.Mesh.vertices.Length];
            for (int i = 0; i < update.Mesh.vertices.Length; i++)
            {
                Vector3 point = update.Mesh.vertices[i];
                RaycastHit hit;

                if (Physics.Raycast(point, (point - uavPose.position).normalized, out hit, 60.0f))
                {
                    Vector3 pixelUV = hit.textureCoord;
                    pixelUV.x *= -frameTexture.width;
                    pixelUV.y *= frameTexture.height;
                    //update.Mesh.colors[i] = 
                    try
                    {
                        colorList[update.MeshId].Insert(i, ((Color)frameTexture.GetPixel((int)pixelUV.x, (int)pixelUV.y)));
                    }
                    catch (ArgumentOutOfRangeException e)
                    {
                        print(e.Message);
                        colorList[update.MeshId].Insert(i, Color.clear);
                    }
                    //colors.Add((Color)frameTexture.GetPixel((int)pixelUV.x, (int)pixelUV.y));
                    //print(colors[i]);
                }
                else
                {
                    if (i < meshList[update.MeshId].GetComponent<MeshFilter>().mesh.colors.Length)
                        colorList[update.MeshId].Insert(i, meshList[update.MeshId].GetComponent<MeshFilter>().mesh.colors[i]);
                    else
                        colorList[update.MeshId].Insert(i, Color.clear);

                }
            }
            try
            {
                update.Mesh.colors = colorList[update.MeshId].GetRange(0, update.Mesh.vertices.Length).ToArray();
            }
            catch (ArgumentOutOfRangeException e)
            {
                print(e.Message);
                //print(update.MeshId);
            }
            //update.Mesh.colors = colors;//.ToArray();//.Clone();

            meshList[update.MeshId].GetComponent<MeshFilter>().mesh = update.Mesh;
            meshList[update.MeshId].GetComponent<MeshFilter>().mesh.RecalculateBounds();
        }
        else
        {
            meshList[update.MeshId].GetComponent<MeshFilter>().mesh = update.Mesh;
            meshList[update.MeshId].GetComponent<MeshFilter>().mesh.RecalculateBounds();
        }
    }

    /// <summary>
    /// Get the current mesh size
    /// </summary>
    public int MeshSize(int id)
    {
        return meshList[id].GetComponent<MeshFilter>().mesh.vertices.Length;
    }

    /// <summary>
    /// Creates a new mesh in the given submesh
    /// </summary>
    /// <param name="subMeshName">The name or identifier of the submesh</param>
    internal void CreateNewMesh(string subMeshName, bool enableLastMesh = false)
    {
        // Finish the exisiting mesh
        currentMeshFilter.mesh.RecalculateBounds();
        currentMeshFilter.mesh = Instantiate(currentMeshFilter.mesh);
        meshList[currentMeshNumber].GetComponent<Renderer>().enabled = enableLastMesh;

        // Add new mesh to the list as GameObject
        meshList.Add(new GameObject());
        colorList.Add(new List<Color>(65000));
        currentMeshNumber++;
        meshList[currentMeshNumber].AddComponent<MeshFilter>();
        meshList[currentMeshNumber].AddComponent<MeshRenderer>();

        // Set Material, Parent and Color
        meshList[currentMeshNumber].GetComponent<MeshRenderer>().material.shader = meshShader;
        meshList[currentMeshNumber].GetComponent<MeshRenderer>().material.color = meshColor;
        meshList[currentMeshNumber].transform.parent = this.transform;

        // Get Meshfilter and add new mesh to it
        currentMeshFilter = meshList[currentMeshNumber].GetComponent<MeshFilter>();
        currentMeshFilter.sharedMesh = new Mesh();
        currentMeshFilter.mesh.name = subMeshName + currentMeshNumber;
        currentMeshFilter.name = subMeshName + currentMeshNumber;
    }

    /// <summary>
    /// Set the position and rotation of the current mesh
    /// </summary>
    /// <param name="position">Postition of the mesh in x,y,z - coordinates</param>
    /// <param name="rotation">Rotation of the mesh in quaternionen</param>
    internal void SetCurrentMeshPose(Vector3 position, Quaternion rotation)
    {
        Transform transform = meshList[currentMeshNumber].GetComponent<Transform>();
        transform.position = position;
        transform.rotation = rotation;
    }
}


