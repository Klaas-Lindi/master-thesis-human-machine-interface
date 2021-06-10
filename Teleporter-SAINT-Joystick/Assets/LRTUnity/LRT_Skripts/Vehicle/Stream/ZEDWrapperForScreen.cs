using UnityEngine;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;

public class ZEDWrapperForScreen : MonoBehaviour
{
    private ZEDClass zed;

    // Texture for live rendering
    //private Texture2D tex;  

    private int poseLosed = 0;
    private int mesh_count = 0;
    private MeshFilter rec;
    private List<GameObject> rec_parent = new List<GameObject>();


    // position to calculate Pose
    private Vector3 offsetRot = new Vector3(-90, 0, 0); //new Vector3(-90, 0, 0);
    private Vector3 offsetPos = new Vector3(0, 0.98f, 0);
    //private Quaternion orientation = new Quaternion();
    //private RawMeshLoader rawMeshLoader;
    //private bool zed_running = false;

    public Boolean ZedEnabled;
    public Boolean lockToCamera;
    public Boolean enableTexture;
    public Boolean filter_mesh;
    public Boolean svo_real_time;

    public string file_path;

    private Pose estimatedPose;

    public Pose EstimatedPose
    {
        get
        {
            return estimatedPose;
        }

        /*set
        {
            estimated_pose = value;
        }*/
    }

    // Use this for initialization
    void Start()
    {
        zed = new ZEDClass(file_path, null, svo_real_time);

        //"C:\\Users\\Max\\Documents\\ZED\\HD720_SN11267_17-14-08.svo"
        //C:\\Users\\Max\\Documents\\ZED\\HD720_SN11267_17-27-15.svo

        zed.Start(filter_mesh);
        //zed_running = true;

        rec_parent.Add(new GameObject());
        //Add Components
        rec_parent[mesh_count].AddComponent<MeshFilter>();
        rec_parent[mesh_count].AddComponent<MeshRenderer>();
        Mesh new_mesh = new Mesh();
        new_mesh.name = "Mesh" + mesh_count;
        rec = rec_parent[mesh_count].GetComponent<MeshFilter>();
        rec.name = "Mesh" + mesh_count;
        rec.mesh = new_mesh;
        //Material yourMaterial = (Material)Resources.Load("Wireframe Cutout Double-Sided");
        rec_parent[mesh_count].GetComponent<MeshRenderer>().material.shader = Shader.Find("UCLA Game Lab/Wireframe/Double-Sided Cutout");
        rec_parent[mesh_count].GetComponent<MeshRenderer>().material.color = new Color32(0, 244, 255, 255);
        
        // Initialize MeshLoader
        //rawMeshLoader = new RawMeshLoader();
        //rawMeshLoader.ZEDMapper = ZEDMapper;
        //rawMeshLoader.mesh = new Mesh();
    }

    // Update is called once per frame
    void Update()
    {
        if (ZedEnabled)
        {

            // Apply texture to material
            Texture2D tex = zed.getFrame();

            if (tex == null)
            {
                //zed_running = false;
                if (enableTexture)
                {
                    zed.requestTexturedMesh();
                }
            }
            else
            {
                gameObject.GetComponent<Renderer>().material.mainTexture = tex;
            }

            estimatedPose = zed.getPose();

            this.transform.rotation = estimatedPose.rotation;

            // Calculate Sphere position
            Vector3 tmp = this.transform.rotation * Vector3.forward;
            this.transform.position = tmp * 50f;

            // Add offset to rotation 
            this.transform.rotation *= Quaternion.Euler(offsetRot);

            // Add position from player
            this.transform.position += (estimatedPose.position) - offsetPos;

            if (lockToCamera)
            {
                Camera.main.transform.position = estimatedPose.position;
                Camera.main.transform.rotation = estimatedPose.rotation;
            }

            zed.requestMesh();

            if (zed.meshRequestState())
            {
                rec.mesh = zed.getMeshAsync();
                rec.mesh.RecalculateBounds();
                print("Mesh updated");              
            }

            // Print status
            print("Pose_State: " + estimatedPose.state);
            if(estimatedPose.state == 0)
            {
                poseLosed++;
            }
            else
            {
                poseLosed = 0;
            }
            if (rec.mesh.vertices.Length >= 60000 || poseLosed == 30)
            {
                rec.mesh.RecalculateBounds();
                rec.mesh = Instantiate(rec.mesh);
                rec_parent[mesh_count].GetComponent<Renderer>().enabled = false;
                rec_parent.Add(new GameObject());
                mesh_count++;
                //Add Components
                rec_parent[mesh_count].AddComponent<MeshFilter>();
                rec_parent[mesh_count].AddComponent<MeshRenderer>();
                rec_parent[mesh_count].GetComponent<MeshRenderer>().material.shader = Shader.Find("UCLA Game Lab/Wireframe/Double-Sided Cutout");
                rec_parent[mesh_count].GetComponent<MeshRenderer>().material.color = new Color32(0, 244, 255, 255);
                //Mesh new_mesh = new Mesh();

                rec = rec_parent[mesh_count].GetComponent<MeshFilter>();
                //rec.name = "Mesh" + mesh_count;
                rec.sharedMesh = new Mesh();
                rec.mesh.name = "Mesh" + mesh_count;

                if (enableTexture)
                    zed.requestTexturedMesh();

                zed.Restart();
                poseLosed = 0;
            }

            if(zed.texturedMeshRequestState())
            {
                zed.getTexturedMeshAsync();
                print("Textured Mesh updated");
            }

        }
    }

    void OnApplicationQuit()
    {
        zed.Delete();
    }


}
