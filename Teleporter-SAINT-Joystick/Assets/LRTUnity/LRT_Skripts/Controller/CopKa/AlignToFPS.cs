using UnityEngine;
using System.Collections;

public class AlignToFPS : MonoBehaviour {

    public GameObject FPCamera;
    private Vector3 offset = new Vector3(0, 0.98f, 0);

	// Use this for initialization
	void Start () {
        //offset = FPCamera.gameObject.transform.position - this.transform.position;
    }
	
	// Update is called once per frame
	void Update () {
        this.transform.position = FPCamera.gameObject.transform.position - offset;
	}
}
