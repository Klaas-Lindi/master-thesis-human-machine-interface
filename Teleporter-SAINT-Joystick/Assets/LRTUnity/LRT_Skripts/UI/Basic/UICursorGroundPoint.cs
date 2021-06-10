using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UICursorGroundPoint : MonoBehaviour
{
    LineRenderer lineRenderer;
    public Transform cursorPosition;

    private Vector3[] list;
    // Start is called before the first frame update
    void Start()
    {
        this.lineRenderer = this.GetComponent<LineRenderer>();
        this.lineRenderer.positionCount = 2;
        list = new Vector3[2];
    }

    // Update is called once per frame
    void Update()
    {
        if (cursorPosition.gameObject.activeSelf)
        {
            this.GetComponent<Renderer>().enabled = true;
            lineRenderer.enabled = true;
            this.transform.position = new Vector3(this.cursorPosition.position.x, 0, this.cursorPosition.position.z);
            list[0] = this.transform.position;
            list[1] = this.cursorPosition.position;
            this.lineRenderer.SetPositions(list);
        }
        else
        {
            this.GetComponent<Renderer>().enabled = false;
            lineRenderer.enabled = false;
        }
        //print(lineRenderer.GetPosition(0));
        //print(lineRenderer.GetPosition(1));
    }
}
