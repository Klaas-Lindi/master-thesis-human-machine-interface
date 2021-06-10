using UnityEngine;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;

public class ZEDWrapper : MonoBehaviour
{
    // Interface to streaming or local zed operation
    private IZedWrapper zed;

    // Pose properties
    private int poseLosed = 0;

    // Public Options and references
    public MeshHandler meshHandler; // Handles all new meshes
    public Boolean ZedEnabled; // Enable or Disable zed camera or streaming 
    public Boolean localOperation; // Set if local operations is wished
    public Boolean useMeshUpdate; // Use Mesh Update. No mesh when disabled
    public Boolean lockToCamera; // Lock Gaming Camera to Zed Camera
    public Boolean filter_mesh; // Filter mesh before retrieving
    public Boolean svo_real_time; // if file is given the video can be played in real time
    public string file_path; // Path to file, if empty connected zed camera will be used

    // real time interval
    private float interval;

    // Animator and visualization class
    private UavState currentUavState;

    // Loggerclass
    private Logger logger;

    // Zed Camera Pose
    private Pose zedCam;

    // Use this for initialization
    void Start()
    {
        // Get visualizer
        currentUavState = this.GetComponent<UavState>();

        // Initialize zed class depending on settings
        try
        {
            if (ZedEnabled)
            {
                if (localOperation)
                {
                    zed = new ZEDClass(file_path, this, svo_real_time);
                    zed.Start(filter_mesh);
                }
                else
                {
                    zed = new ZEDStreamingClass(this);
                    zed.Start();
                }
            }
        }
        catch(Exception e)
        {
            print(e.Message);
            return;
        }

        // set reconstructions camera to zero
        zedCam = new Pose();
        zedCam.rotation = new Quaternion(0, 0, 0, 1);
        zedCam.position = new Vector3();
        
        // Initialize MeshHandler to handle all new meshes
        meshHandler.InitMesh("Mesh");

        //Pre Initialize 1000 meshes
        for (int i = 0; i < 1000; i++)
        {
            meshHandler.CreateNewMesh("Mesh", true);
        }

        interval = Time.realtimeSinceStartup;
    }

    // Update is called once per frame
    void Update()
    {
        if (ZedEnabled)
        {
            // Get current frame and set it as texture
            zed.requestFrame();

            if (zed.frameRequestState())
            {
                currentUavState.CurrentFrame = zed.getFrameAsync();
            }

            meshHandler.FrameTexture = currentUavState.CurrentFrame;
            
            // Get current estunated camera pose from the zed 
            zedCam = zed.getPose();

            // If local operation the zedcam pose will be used
            if (localOperation)
            {
                // Not correctly implementede, because 
                currentUavState.SetCameraPosition(zedCam.position); 
                currentUavState.CameraPose.rotation = zedCam.rotation; 
                currentUavState.CameraPose.state = zedCam.state;
                meshHandler.UavPose = zedCam;
            }

            // Set main camera with current camera
            if (lockToCamera)
            {
                Camera.main.transform.rotation = currentUavState.CameraPose.rotation;
                Camera.main.transform.position = currentUavState.CameraPose.position;   
            }
            
            // Get mesh over updateds
            if (useMeshUpdate)
            {

                // Request mesh every second
                if ((Time.realtimeSinceStartup - interval) > 1)
                {
                    zed.requestUpdatedMesh();
                    interval = Time.realtimeSinceStartup;
                }
                
                // Send mes to meshhander if mesh is avaliable
                if(zed.updateMeshRequestState())
                {
                    try
                    {
                        List<MeshUpdate> updates = zed.getUpdateMeshAsync();
                        //logger.logMesh(updates);
                        foreach (MeshUpdate update in updates)
                        {
                            for (int i = meshHandler.MeshCount; i < update.MeshId; i++)
                            {
                                meshHandler.CreateNewMesh("Mesh", true);
                            }
                            meshHandler.setMesh(update);
                        }
                    }
                    catch( Exception e)
                    {
                        print(e);
                    }
                }
            }

            // Routine to handle long lose of camera
            if(currentUavState.CameraPose.state == 0)
            {
                poseLosed++;
            }
            else
            {
                poseLosed = 0;
            }

            if (meshHandler.CurrentMeshSize >= 60000 || poseLosed == 30)
            {
                meshHandler.CreateNewMesh("Mesh");
               
                if (poseLosed > 30)
                {
                    poseLosed = 0;
                }
                //print("3D reconstruction will be restart");
                //zed.Restart();
            }
        }
    }

    public Pose GetRekonstruktionCameraPose()
    {
        return zedCam;
    }

    void OnApplicationQuit()
    {
        if(zed != null)
            zed.Delete();
    }
}

interface IZedWrapper
{
    void Start(bool filter_mesh = false);
    void Restart();
    void Delete();

    Pose getPose();
    void requestFrame();
    Texture2D getFrameAsync();
    bool frameRequestState();

    void requestUpdatedMesh();
    List<MeshUpdate> getUpdateMeshAsync();
    bool updateMeshRequestState();
    int isMeshUpdateAvailable();
}
    
