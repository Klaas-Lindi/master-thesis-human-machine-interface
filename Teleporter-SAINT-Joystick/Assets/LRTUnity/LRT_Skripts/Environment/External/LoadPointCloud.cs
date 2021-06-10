using KDTree;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class LoadPointCloud : MonoBehaviour {

    // File Parameter
    [Header("Source of Point Cloud")]
    [Tooltip("This is the path to file, where the UTM based PCL will be loaded")]
    public string pointCloudPath;
    private StreamReader stream;

    // Parameters to load data
    [Header("Point Cloud Settings")]
    [Tooltip("The origin UTM point where the surrounding will be loaded. x: Easting, y: Northing, z: Altitude.")]
    public Vector3 defaultOrigin;
    private UTM origin;
    [Tooltip("Range around the origin in m which will be loaded. x: Latitude-Range, y: Longitude-Range.")]
    public Vector2 range;

    [Header("Graphical Settings")]
    // Appearance
    [Tooltip("If enabled a wire frame will be displayed. Otherwise the color of the utm file will be used")]
    public bool wireFrame;
    // TBD: Currently the points are transformed in a surface. Maybe we can also display the point cloud itself

    // Mesh Variables
    private Mesh mesh = null;
    private List<Vector3> vertices;
    private List<int> triangles;
    private List<Color> colors;
    private Shader meshShader;
    private Color32 meshColor = new Color32(0, 244, 255, 255);
    private GameObject pointCloud;

    //Coroutine and Update handle
    private bool readyToReload = true;
    private bool reload = true;
    private bool toogleVisiblity;


    // Use this for initialization
    void Start () {     
        // GameObject References
        pointCloud = new GameObject();
        pointCloud.AddComponent<MeshFilter>();
        pointCloud.AddComponent<MeshRenderer>();
        pointCloud.AddComponent<MeshCollider>();
        pointCloud.GetComponent<MeshFilter>().name = "DOM40";//pointCloudPath.Substring(pointCloudPath.LastIndexOf('\\')+1);
                
        // Set rendering shader
        if (wireFrame)
        {
            meshShader = Shader.Find("UCLA Game Lab/Wireframe/Double-Sided Cutout");
        }
        else
        {
            meshShader = Shader.Find("Unlit/Test1");
        }   
        
        // Set shaders
        pointCloud.GetComponent<MeshRenderer>().material.shader = meshShader;
        pointCloud.GetComponent<MeshRenderer>().material.color = meshColor;
        pointCloud.transform.parent = this.transform;

        // Load pointcloud with presettings
        origin = new UTM(defaultOrigin.x, defaultOrigin.y, defaultOrigin.z);
        readyToReload = true;
        reload = true;

        // UiSettings
        toogleVisiblity = true;


    }



    public IEnumerator LoadPCL()
    {
        // Initialize File reader
        try
        {
            stream = new StreamReader(pointCloudPath);
        }
        catch (DirectoryNotFoundException)
        {
            Debug.LogError("Path to external point cloud not found!");
            stream = null;
        }
        string line;

        // Init mesh 
        mesh = new Mesh();
        vertices = new List<Vector3>();
        triangles = new List<int>();
        colors = new List<Color>();

        // Initialize KDTree for faster search
        KDTree<int> tree = new KDTree<int>(2);
        int index = 0;
        int readed_lines = 1;
        // Get data from file
        while ((line = stream.ReadLine()) != null)
        {
            string[] list = line.Split(' ');

            // Test if in Range of parameters than load
            if (inRange(float.Parse(list[0]), float.Parse(list[1])))
            {
                float x = (float)(decimal.Parse(list[0]) - (decimal)origin.E);
                float y = (float)(decimal.Parse(list[2]) - (decimal)origin.A);
                float z = (float)(decimal.Parse(list[1]) - (decimal)origin.N);
                Vector3 newPoint = new Vector3(x, y, z);
                vertices.Add(newPoint);
                colors.Add(new Color((float)ushort.Parse(list[3]) / ushort.MaxValue, float.Parse(list[4]) / (float)ushort.MaxValue, float.Parse(list[5]) / ushort.MaxValue));
                tree.AddPoint(new double[] { newPoint.x, newPoint.z}, index++);
            }

            // Handle when mesh is to big
            if (index >= 65000)
            {
                // Not implemented at the moment to handle big meshes
                print("Mesh full! Range to big!");
                break;
            }
            if((readed_lines % 1000)==0)
                yield return null;
            readed_lines++;
        }
        //Close file
        stream.Close();

        // Make Grid based on vertices
        int max_ind = 0;
        for (int i = 0; i<vertices.Count - 1; i++)
        {
            // Get nearest neighbour
            NearestNeighbour<int> nearest = tree.NearestNeighbors(new double[] { vertices[i].x, vertices[i].z }, 9);

            // Calulate  if point is boundary
            if (Mathf.Sqrt(Mathf.Pow(vertices[i].x - vertices[i + 1].x, 2) + Mathf.Pow(vertices[i].z - vertices[i + 1].z, 2)) < 1)
            {
                // If not boundary build two triangles
                foreach (int ind in nearest)
                {
                    if (ind >= i)
                    {
                        if (ind == i)
                        {
                            continue;
                        }
                        else if (ind == i + 1)
                        {
                            continue;
                        }
                        else if (nearest.CurrentDistance< 0.25)
                        {
                            triangles.Add(i);
                            triangles.Add(i + 1);
                            triangles.Add(ind);
                            max_ind = ind;
                        }
                        else if (ind<max_ind && nearest.CurrentDistance> 0.25 && nearest.CurrentDistance< 0.5)
                        {
                            triangles.Add(i);
                            triangles.Add(max_ind);
                            triangles.Add(ind);
                        }
                    }
                }

                // Handle not completed triangles
                while (triangles.Count % 3 != 0)
                {
                    triangles.RemoveAt(triangles.Count - 1);
                }
            }
            else
            {
                // Build boundary triangles
                foreach (int ind in nearest)
                {
                    if (ind >= i)
                    {
                        if (ind == i)
                        {
                            continue;
                        }
                        else if (nearest.CurrentDistance< 0.25)
                        {
                            max_ind = ind;
                            continue;
                        }
                        else if (ind<max_ind && nearest.CurrentDistance> 0.25 && nearest.CurrentDistance< 0.5)
                        {
                            triangles.Add(i);
                            triangles.Add(max_ind);
                            triangles.Add(ind);
                        }
                    }
                }
            }
            if ((i % 1000) == 0)
                yield return null;
        }

        // Set mesh
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.colors = colors.ToArray();

        // Set Render
        pointCloud.GetComponent<MeshFilter>().mesh = mesh;
        pointCloud.GetComponent<MeshFilter>().mesh.RecalculateBounds();
        pointCloud.GetComponent<MeshCollider>().sharedMesh = mesh;

        readyToReload = true;
    }
	
	// Update is called once per frame
	void Update () {
        if (wireFrame)
        {
            meshShader = Shader.Find("UCLA Game Lab/Wireframe/Double-Sided Cutout");
        }
        else
        {
            meshShader = Shader.Find("Unlit/Test1");
        }

        // Set shaders
        pointCloud.GetComponent<MeshRenderer>().material.shader = meshShader;

        // Handle Updates of mesh
        if(readyToReload && reload)
        {
            StartCoroutine(this.LoadPCL());
            readyToReload = false;
            reload = false;
        }
    }

    public void Reload(GPS gps)
    {
        GPSConverter converter = new GPSConverter("");
        GPSConverter.UTMResult results = converter.convertLatLngToUtm(gps.latitude, gps.longitude);
        origin.E =  results.Easting;
        origin.N = results.Northing;
        origin.A = gps.height;

        // Set for visualization
        defaultOrigin.x = (float)origin.E;
        defaultOrigin.y = (float)origin.N;
        defaultOrigin.z = (float)origin.A;

        reload = true;
    }


    // Check if point is in range of intereset
    bool inRange(float x, float y)
    {
        if (x < (origin.E - range.x))
            return false;

        if (x > (origin.E + range.x))
            return false;

        if (y < (origin.N - range.y))
            return false;

        if (y > (origin.N + range.y))
            return false;


        return true;
    }

    public bool isReloading()
    {
        return !readyToReload;
    }

    public void ToggleVisibity()
    {
        toogleVisiblity = !toogleVisiblity;
        this.pointCloud.GetComponent<MeshRenderer>().enabled = toogleVisiblity;
    }

}

public class UTM
{
    double easting;
    double northing;
    double altitude;

    public double E
    {
        get
        {
            return easting;
        }

        set
        {
            easting = value;
        }
    }

    public double N
    {
        get
        {
            return northing;
        }

        set
        {
            northing = value;
        }
    }

    public double A
    {
        get
        {
            return altitude;
        }

        set
        {
            altitude = value;
        }
    }

    public UTM()
    {
        this.E = 4475450;
        this.N = 5347635;
        this.A = 478;
    }

    public UTM (double easting, double northing, double altitude)
    {
        this.E = easting;
        this.N = northing;
        this.A = altitude;
    }
}