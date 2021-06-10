using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeleObject : MonoBehaviour {

    protected Pose current_pose;
    protected Pose predict_pose;
    protected GameObject current_teleobject;
    protected GameObject predict_teleobject;

    public virtual void Init()
    {
        // Initialize object for further use
    }

    public virtual void CreateTeleObjects(string name)
    {
        // Create current TeleObject with textures
        current_teleobject = new GameObject(name + "_current");
        MeshRenderer current_mesh = current_teleobject.AddComponent<MeshRenderer>();
        current_mesh.material = Resources.Load("LRT_Materials/TeleObject_Current_Material.mat", typeof(Material)) as Material;
        current_teleobject.SetActive(true);

        // Create predict TeleObject with textures
        predict_teleobject = new GameObject(name + "_predict");
        MeshRenderer predict_mesh = current_teleobject.AddComponent<MeshRenderer>();
        predict_mesh.material = Resources.Load("LRT_Materials/TeleObject_Predict_Material.mat", typeof(Material)) as Material;
        predict_teleobject.SetActive(false);
    }


    


    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
