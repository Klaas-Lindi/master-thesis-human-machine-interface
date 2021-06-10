using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UiMapInput : MonoBehaviour {

    public float moveYFaktorWheel = 0.25f;
    public float minSize = 10;
    public float maxSize = 500;

    public float translationFaktor = 1;
    public Vector2 boundary;

    public float tiltTransistionRate;
    public float maxTilt = 80;
    public float minTilt = 45;
    private float tilt;

    public float rotationRateX;
    public float rotationRateY;
    private Vector3 targetPoint;


    private Camera mapCamera;
    private Vector3 previousMousePosition;
    private Vector3 lastPosition;
    private Vector3 offset;
    private bool toogle2D = false;
    private bool toogleRot = false;
    private bool toogleActive = false;

    public Slider slider;

	// Use this for initialization
	void Start () {
        mapCamera = this.GetComponent<Camera>();
        targetPoint = new Vector3();
        tilt = maxTilt;
        offset = new Vector3(0, this.transform.position.y, 0);

    }
	
	// Update is called once per frame
	void LateUpdate () {
        Vector3 mouseCoordinates = Display.RelativeMouseAt(new Vector3(Input.mousePosition.x, Input.mousePosition.y));


        // Postition of groundplane intersection
        Vector3 direction = this.transform.TransformDirection(Vector3.forward);
        float factor = -this.transform.position.y / direction.y;
        Vector3 mapPosition = this.transform.position + factor * direction + new Vector3(0, 0f, 0f);

        //print(camera.ScreenToWorldPoint(Input.mousePosition));

        Vector3 mouseViewportPosition = mapCamera.ScreenToViewportPoint(Input.mousePosition);
        if (isMouseinScreen())
        {
            // Calculating important inputs;
            float wheelAxis = Input.GetAxis("Mouse ScrollWheel");

            if (Input.GetKey(KeyCode.Mouse1))
            {
                if (wheelAxis < 0 && mapCamera.orthographicSize < maxSize)
                {
                    mapCamera.orthographicSize += moveYFaktorWheel;
                    slider.value = (mapCamera.orthographicSize - minSize) / (maxSize - minSize);
                }
                if (wheelAxis > 0 && mapCamera.orthographicSize > minSize)
                {
                    mapCamera.orthographicSize -= moveYFaktorWheel;
                    slider.value = (mapCamera.orthographicSize - minSize) / (maxSize - minSize);
                }

                if (toogleRot)
                {
                    this.transform.RotateAround(targetPoint, Vector3.up,(mouseViewportPosition.x - previousMousePosition.x) * rotationRateX);
                    this.transform.RotateAround(targetPoint, Vector3.left, (mouseViewportPosition.y - previousMousePosition.y) * rotationRateY);


                    this.transform.LookAt(targetPoint);
                    //this.transform.rotation = Quaternion.Euler(this.transform.rotation.eulerAngles + (new Vector3(Input.mousePosition.x - previousMousePosition.x, 0, Input.mousePosition.y - previousMousePosition.y)) * translationFaktor);
                }
                else
                {
                    //Vector3 change = ((new Vector3(Input.mousePosition.x - previousMousePosition.x, 0, Input.mousePosition.y - previousMousePosition.y)) * translationFaktor);
                    float xDirection = -(mouseViewportPosition.x - previousMousePosition.x) * Mathf.Cos(Mathf.Deg2Rad* this.transform.rotation.eulerAngles.y) - (mouseViewportPosition.y - previousMousePosition.y) * Mathf.Sin(Mathf.Deg2Rad * this.transform.rotation.eulerAngles.y);
                    float yDirection =  (mouseViewportPosition.x - previousMousePosition.x) * Mathf.Sin(Mathf.Deg2Rad * this.transform.rotation.eulerAngles.y) - (mouseViewportPosition.y - previousMousePosition.y) * Mathf.Cos(Mathf.Deg2Rad * this.transform.rotation.eulerAngles.y);
                    Vector3 change = ((new Vector3(xDirection, 0, yDirection) * (mapCamera.orthographicSize/translationFaktor)));
                    targetPoint += change;
                    this.transform.position += change;

                    if (-boundary.x > this.transform.position.x)
                    {
                        this.transform.position = new Vector3(-boundary.x, this.transform.position.y, this.transform.position.z);
                        targetPoint.x -= (Input.mousePosition.x - previousMousePosition.x) * translationFaktor;
                    }
                    if (boundary.x < this.transform.position.x)
                    { 
                        this.transform.position = new Vector3(boundary.x, this.transform.position.y, this.transform.position.z);
                        targetPoint.x += (Input.mousePosition.x - previousMousePosition.x) * translationFaktor;
                    }
                    if (-boundary.y > this.transform.position.z)
                    { 
                        this.transform.position = new Vector3(this.transform.position.x, this.transform.position.y, -boundary.y);
                        targetPoint.z -= (Input.mousePosition.y - previousMousePosition.y) * translationFaktor;
                    }
                    if (boundary.y < this.transform.position.z)
                    { 
                        this.transform.position = new Vector3(this.transform.position.x, this.transform.position.y, boundary.y);
                        targetPoint.z += (Input.mousePosition.y - previousMousePosition.y) * translationFaktor;
                    }
                }
                //this.transform.LookAt(targetPoint);
            }        
        }
        previousMousePosition = mouseViewportPosition;
       /* if (!toogleRot)
        {
            //Tilt transition animation
            if (this.transform.rotation.eulerAngles.x < tilt)
                this.transform.RotateAround(targetPoint, Vector3.right, +tiltTransistionRate);
            if (this.transform.rotation.eulerAngles.x > tilt)
                this.transform.RotateAround(targetPoint, Vector3.right, -tiltTransistionRate);
        }*/

        if (toogleActive)
        {
            if (Mathf.Abs(this.transform.rotation.eulerAngles.y) > 1)
            {
                if (this.transform.rotation.eulerAngles.y < 0)
                    this.transform.RotateAround(targetPoint, Vector3.up, +tiltTransistionRate);
                if (this.transform.rotation.eulerAngles.y > 0)
                    this.transform.RotateAround(targetPoint, Vector3.up, -tiltTransistionRate);
            }
            else
            {
                if (Mathf.Abs(this.transform.rotation.eulerAngles.x - tilt) > 1)
                {
                    if (this.transform.rotation.eulerAngles.x < tilt)
                        this.transform.RotateAround(targetPoint, Vector3.right, +tiltTransistionRate);
                    if (this.transform.rotation.eulerAngles.x > tilt)
                        this.transform.RotateAround(targetPoint, Vector3.right, -tiltTransistionRate);
                }
                else
                {
                    toogleActive = false;
                }
            }
        }
        
    }

    /// <summary>
    /// Check if Mouse is over screen
    /// </summary>
    /// <returns>True if over screen else False</returns>
    private bool isMouseinScreen()
    {
        Vector3 tmp = mapCamera.ScreenToViewportPoint(Input.mousePosition);
        return (tmp.x >= 0.0f && tmp.x <= 1.0f && tmp.y <= 1.0f && tmp.y >= 0.0f);
    }

    public void Toogle2D3D()
    {
        if (toogleRot)
        {
            toogle2D = false;
            tilt = maxTilt;
        }
        else
        {
            toogle2D = !toogle2D;
            if (toogle2D)
            {
                tilt = minTilt;
            }
            else
            {
                tilt = maxTilt;
            }
        }
        toogleActive = true;

    }

    public void ToogleRotationTranslation()
    {
        toogleRot = !toogleRot;
    }

    public void SetHeight(float precentage)
    {
        mapCamera.orthographicSize = (maxSize - minSize) * precentage + minSize;
    }


    // Controller Functions
    public Vector3 GetWorldMousePosition()
    {
        Ray ray = this.mapCamera.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, Vector3.zero);
        float d;

        plane.Raycast(ray, out d);
        Vector3 hit = ray.GetPoint(d);

        return hit;
    }

    // Controller Functions
    public Vector3 GetScreenMousePosition()
    {
        return Input.mousePosition; // camera.ScreenToViewportPoint(Input.mousePosition); 
    }



}
