using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragObjectHeight : MonoBehaviour
{
    private Vector3 offset;

    private float yCoord;
    public Camera camera;
    public GameObject twinObject;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnMouseDown()
    {
        yCoord = camera.WorldToScreenPoint(gameObject.transform.position).y;

        offset = gameObject.transform.position - GetMouseWorldPos();
    }

    private Vector3 GetMouseWorldPos()
    {
        Vector3 mousePoint = Input.mousePosition;

        mousePoint.z = yCoord;

        return camera.ScreenToViewportPoint(mousePoint);
    }

    private void OnMouseDrag()
    {
        gameObject.transform.position = new Vector3(0,GetMouseWorldPos().y) + offset;
        twinObject.transform.position = new Vector3(0, GetMouseWorldPos().y) + offset;
    }
}
