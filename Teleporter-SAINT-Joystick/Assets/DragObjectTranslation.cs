using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragObjectTranslation : MonoBehaviour
{
    private Vector3 offset;

    private float yCoord;
    public Camera camera;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per framel
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
        gameObject.transform.position = new Vector3(-GetMouseWorldPos().x + offset.x, 0, GetMouseWorldPos().y + offset.y);
    }

}
