using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Runtime.InteropServices;
using System.Diagnostics;

public class SelectItem : MonoBehaviour
{
    [SerializeField]

    public FeedDatabase feedDatabase;
    public RectTransform selectSquareSprite;
    public RobotControlSAINT robotControl;
    public OperatorState operatorState;
    Vector2 startPos, direction, centreSave, scale;
    float angleTolerance = 0.00f;
    float sizeY, sizeX, rotSave, sizeYSave, sizeXSave;
    float[] v = new float[5];
    int passtoken = 1;
    public RectTransform screen2Fade, lassoActivation;
    public GameObject resetLasso, saveAndSendLasso, lassoDot, SendCoordinates;
    public int rsSizeX, rsSizeY;
    int dotcount;
    public List<Vector2> userSelect = new List<Vector2>();
    public List<Vector2> selectDirections = new List<Vector2>();
    int i = 0;
    int resetIndex = 1;
    public Vector2[] lasso, lasso2;
    string saveSelectBox, message;


    // Start is called before the first frame update
    void Start()
    {
        SendCoordinates.gameObject.SetActive(false);
        resetLasso.gameObject.SetActive(false);
        scale.x = rsSizeX;
        scale.y = rsSizeY;
    }

    // Update is called once per frame
    void Update()
    {

        if (Input.touchCount > 0 && passtoken == 1)
        {
            Touch t0 = Input.GetTouch(0);
            switch (t0.phase)
            {

                case TouchPhase.Began:

                    lassoDot.gameObject.SetActive(true);
                    startPos = t0.position;
                    generateDot(t0);
                    userSelect.Add(t0.position);
                    i++;
                    break;

                case TouchPhase.Moved:

                    userSelect.Add(t0.position);            //assign new point to list
                    direction = t0.position - userSelect[i - 1];
                    selectDirections.Add(direction);       //assign new direction vector to list
                    generateDot(t0);
                    i++;
                    break;

                case TouchPhase.Ended:

                    resetLasso.gameObject.SetActive(true);
                    lasso = userSelect.ToArray();
                    lasso2 = userSelect.ToArray();
                    Vector2[] lassoDir = selectDirections.ToArray();
                    if (lasso.Length > 3)
                    {
                        saveAndSendLasso.gameObject.SetActive(true);
                        resetIndex = 0;
                    }
                    if (resetIndex == 1)
                    {
                        resetLasso.gameObject.SetActive(false);
                    }
                    passtoken = resetIndex;

                    break;
            }
        }
    }

    public GameObject generateDot(Touch t0)
    {
        GameObject d = Instantiate(lassoDot) as GameObject;
        d.name = "SelectPoint " + i.ToString();
        d.transform.SetParent(lassoActivation, false);
        d.transform.position = t0.position;
        return d;
    }

    public void ResetLasso()
    {
        userSelect.Clear();
        selectDirections.Clear();
        lasso = userSelect.ToArray();
        lasso2 = userSelect.ToArray();
        Vector2[] lassoDir = selectDirections.ToArray();
        i = 0;
        passtoken = 1;
        lassoDot.gameObject.SetActive(false);
        foreach (Transform child in lassoActivation.transform)
        {
            Destroy(child.gameObject);
        }
        resetLasso.gameObject.SetActive(false);
        saveAndSendLasso.gameObject.SetActive(false);
        selectSquareSprite.transform.rotation = Quaternion.Euler(0, 0, 0);
        selectSquareSprite.gameObject.SetActive(false);
        resetIndex = 1;
    }

    public void DeleteLasso()
    {
        userSelect.Clear();
        selectDirections.Clear();
        lasso = userSelect.ToArray();
        lasso2 = userSelect.ToArray();
        Vector2[] lassoDir = selectDirections.ToArray();
        i = 0;
        passtoken = 1;
        lassoDot.gameObject.SetActive(false);
        foreach (Transform child in lassoActivation.transform)
        {
            Destroy(child.gameObject);
        }
        selectSquareSprite.transform.rotation = Quaternion.Euler(0, 0, 0);
        selectSquareSprite.gameObject.SetActive(false);
    }

    //public void SendLasso()
    //{
    //    resetLasso.gameObject.SetActive(false);
    //    saveLasso.gameObject.SetActive(false);
    //    float width = screen2Fade.rect.width;
    //    float height = screen2Fade.rect.height;
    //    scale.x = rsSizeX / width;
    //    scale.y = rsSizeY / height;
    //    Vector3[] r = new Vector3[4];
    //    Vector3[] q = new Vector3[2];
    //    q[0].y = (height - centreSave.y) * scale.y;     // y coordinate of centre
    //    q[0].x = centreSave.x * scale.x;                // x coordinate of centre
    //    q[1].y = sizeYSave * scale.y;                   // y scale of rectangle     
    //    q[1].x = sizeXSave * scale.x;                   // x scale of rectangle
    //    selectSquareSprite.GetWorldCorners(r);        ///if ROS wants corners and rotation of the selectbox
    //    rotSave = Vector2.Angle(r[3] - r[0], Vector2.right);
    //    //saveSelectBox = q[0].x.ToString("F1") + " " + q[0].y.ToString("F1") + " " + q[1].x.ToString("F1") + " " + q[1].y.ToString("F1") + " " + rotSave.ToString("F3");

    //    float[] xValsize = new float[4];
    //    float[] yValsize = new float[4];
    //    for (int entry = 0; entry < 4; entry++)
    //    {
    //        xValsize[entry] = r[entry].x;
    //        yValsize[entry] = r[entry].y;
    //    }
    //    float boxSizeXdim = Mathf.Max(xValsize) - Mathf.Min(xValsize);
    //    float boxSizeYdim = Mathf.Max(yValsize) - Mathf.Min(yValsize);

    //    selectSquareSprite.transform.rotation = Quaternion.Euler(0, 0, 0);
    //    selectSquareSprite.sizeDelta = new Vector2(boxSizeXdim, boxSizeYdim);
    //    selectSquareSprite.position = centreSave;



    //    boxSizeXdim = scale.x * (Mathf.Max(xValsize) - Mathf.Min(xValsize));
    //    boxSizeYdim = scale.y * (Mathf.Max(yValsize) - Mathf.Min(yValsize));
    //    selectSquareSprite.GetWorldCorners(r);

    //    r[0].y = (height - r[0].y) * scale.y;
    //    r[1].y = (height - r[1].y) * scale.y;
    //    r[2].y = (height - r[2].y) * scale.y;
    //    r[3].y = (height - r[3].y) * scale.y;
    //    r[0].x = r[0].x * scale.x;
    //    r[1].x = r[1].x * scale.x;
    //    r[2].x = r[2].x * scale.x;
    //    r[3].x = r[3].x * scale.x;

    //    System.Threading.Thread.Sleep(2000);

    //    //saveSelectBox = "100" + " " + "100" + " " + "200" + " " + "200" + " " + rotSave.ToString("F3");  //hardcoded values
    //    //saveSelectBox = r[1].x.ToString("F1") + " " + r[1].y.ToString("F1") + " " + q[1].x.ToString("F1") + " " + q[1].y.ToString("F1") + " " + rotSave.ToString("F3");       //if ROS needs corners AND rotation of the selectbox
    //    saveSelectBox = r[1].x.ToString("F1") + " " + r[1].y.ToString("F1") + " " + boxSizeXdim.ToString("F1") + " " + boxSizeYdim.ToString("F1") + " " + rotSave.ToString("F3");       //if ROS only needs Box without being capable of handling rotation
    //    robotControl.ItemSelect = saveSelectBox;                               // send coordinates to topic
    //    SendCoordinates.gameObject.SetActive(true);
    //}

    public void SaveAndSendLasso()
    {
        saveAndSendLasso.gameObject.SetActive(false);
        //calculates minimum bounding box around lasso
        int angleIter = 90;                        // has to be matched to desired angle rotation/stepsize
        float maxX, maxY, minX, minY, area=0f;
        int boxRotation = 0, index = 0, length = lasso.Length;
        float[] areas = new float[angleIter];
        float[] sizeX = new float[angleIter];
        float[] sizeY = new float[angleIter];
        float[] xVal = new float[length];
        float[] yVal = new float[length];
        Vector2[] max = new Vector2[angleIter];
        Vector2[] min = new Vector2[angleIter];
        Vector2 centre;

        for (float alpha = 1f; alpha <= angleIter; alpha++)
        {
            for (int iter = 0; iter < i; iter++)
            {
                lasso[iter] = RotateToXAxis(lasso2[iter], -alpha * Mathf.Deg2Rad);
                xVal[iter] = lasso[iter].x;
                yVal[iter] = lasso[iter].y;
            }
            max[(int)alpha - 1].x = Mathf.Max(xVal);
            max[(int)alpha - 1].y = Mathf.Max(yVal);
            min[(int)alpha - 1].x = Mathf.Min(xVal);
            min[(int)alpha - 1].y = Mathf.Min(yVal);
            sizeY[(int)alpha - 1] = Mathf.Abs(min[(int)alpha - 1].y - max[(int)alpha - 1].y);
            sizeX[(int)alpha - 1] = Mathf.Abs(min[(int)alpha - 1].x - max[(int)alpha - 1].x);
            areas[(int)alpha - 1] = sizeX[(int)alpha - 1] * sizeY[(int)alpha - 1];
        }
        area = Mathf.Min(areas);
        for (int indIter = 0; indIter < angleIter; indIter++)                                       //search for entry in areas with the minimum area to get the index
        {
            if (areas[indIter] == area)
            {
                index = indIter;
                boxRotation = index + 1;
            }
        }
        /* uncomment if Vision can handle box angles
        centre.x = (max[index].x + min[index].x) / 2;
        centre.y = (max[index].y + min[index].y) / 2;
        float rotAngle = (float)boxRotation;
        centre = RotateToXAxis(centre, (rotAngle) * Mathf.Deg2Rad);
        selectSquareSprite.position = centre;
        selectSquareSprite.sizeDelta = new Vector2(sizeX[index], sizeY[index]);
        selectSquareSprite.transform.Rotate(0.0f, 0.0f, rotAngle, Space.Self);
        selectSquareSprite.gameObject.SetActive(true);
        rotSave = rotAngle;
       // if (rotSave > 90)
        sizeXSave = sizeX[index];
        sizeYSave = sizeY[index];
        */
        //comment out if Vision can hanle box angles >>>>>>>
        centre.x = (max[0].x + min[0].x) / 2;
        centre.y = (max[0].y + min[0].y) / 2;
        float rotAngle = 1f;
        centre = RotateToXAxis(centre, (rotAngle) * Mathf.Deg2Rad);
        selectSquareSprite.position = centre;
        selectSquareSprite.sizeDelta = new Vector2(sizeX[0], sizeY[0]);
        selectSquareSprite.transform.Rotate(0.0f, 0.0f, rotAngle, Space.Self);
        selectSquareSprite.gameObject.SetActive(true);
        rotSave = rotAngle;
        sizeXSave = sizeX[0];
        sizeYSave = sizeY[0];
        // <<<<<<<<

        centreSave = centre;

        resetLasso.gameObject.SetActive(false);
        float width = screen2Fade.rect.width;
        float height = screen2Fade.rect.height;
        scale.x = rsSizeX / width;
        scale.y = rsSizeY / height;
        Vector3[] r = new Vector3[4];
        Vector3[] q = new Vector3[2];
        q[0].y = (centreSave.y) * scale.y;     // y coordinate of centre, if UV-Rect-H value of Screen2Fade == 1, this line has to be:       q[0].y = (height - centreSave.y) * scale.y;
        q[0].x = centreSave.x * scale.x;                // x coordinate of centre
        q[1].y = sizeYSave * scale.y;                   // y scale of rectangle     
        q[1].x = sizeXSave * scale.x;                   // x scale of rectangle
        selectSquareSprite.GetWorldCorners(r);
        rotSave = Vector2.Angle(r[3] - r[0], Vector2.right);
        //saveSelectBox = q[0].x.ToString("F1") + " " + q[0].y.ToString("F1") + " " + q[1].x.ToString("F1") + " " + q[1].y.ToString("F1") + " " + rotSave.ToString("F3");

        float[] xValsize = new float[4];
        float[] yValsize = new float[4];
        for (int entry = 0; entry < 4; entry++)
        {
            xValsize[entry] = r[entry].x;
            yValsize[entry] = r[entry].y;
        }

        float boxSizeXdim = q[1].x;// Mathf.Max(xValsize) - Mathf.Min(xValsize);
        float boxSizeYdim = q[1].y;// Mathf.Max(yValsize) - Mathf.Min(yValsize);

        //selectSquareSprite.transform.rotation = Quaternion.Euler(0, 0, 0);
        //selectSquareSprite.sizeDelta = new Vector2(boxSizeXdim, boxSizeYdim);
        //selectSquareSprite.position = centreSave;


        boxSizeXdim = scale.x * (Mathf.Max(xValsize) - Mathf.Min(xValsize));
        boxSizeYdim = scale.y * (Mathf.Max(yValsize) - Mathf.Min(yValsize));
 

        r[0].y = (height - r[0].y) * scale.y;                   //if UV-Rect-H value of Screen2Fade == -1, this line has to be:        r[0].y = (r[0].y) * scale.y; 
        r[1].y = (height - r[1].y) * scale.y;                   //if UV-Rect-H value of Screen2Fade == -1, this line has to be:        r[1].y = (r[1].y) * scale.y;   
        r[2].y = (height - r[2].y) * scale.y;                   //if UV-Rect-H value of Screen2Fade == -1, this line has to be:        r[2].y = (r[2].y) * scale.y; 
        r[3].y = (height - r[3].y) * scale.y;                   //if UV-Rect-H value of Screen2Fade == -1, this line has to be:        r[3].y = (r[3].y) * scale.y; 
        r[0].x = (r[0].x) * scale.x;                    //if UV-Rect-W value of Screen2Fade == 1, this line has to be:         r[0].x = (width - r[0].x) * scale.x;           
        r[1].x = (r[1].x) * scale.x;
        r[2].x = (r[2].x) * scale.x;
        r[3].x = (r[3].x) * scale.x;

        //print("Eckpunkte: " + r[0] + " " + r[1] + " " + r[2] + " " + r[3] + " Box size y: " + boxSizeYdim + " Box size x: " + boxSizeXdim);
        if (r[1].x < 0) r[1].x = 0;
        if (r[1].x > rsSizeX) r[1].x = rsSizeX;
        if (r[1].y < 0) r[1].y = 0;
        if (r[1].y > rsSizeY) r[1].y = rsSizeY;
        if (r[1].x + boxSizeXdim > rsSizeX) r[1].x = rsSizeX - boxSizeXdim;
        if (r[1].y + boxSizeYdim > rsSizeY) r[1].y = rsSizeY - boxSizeYdim;
        //print("Eckpunkte: " + r[0] +" "+ r[1] + " " + r[2] + " " + r[3] + " Box size y: " + boxSizeYdim + " Box size x: " + boxSizeXdim);

        System.Threading.Thread.Sleep(2000);

        //saveSelectBox = "100" + " " + "100" + " " + "200" + " " + "200" + " " + rotSave.ToString("F3");  //hardcoded values
        //saveSelectBox = r[1].x.ToString("F1") + " " + r[1].y.ToString("F1") + " " + q[1].x.ToString("F1") + " " + q[1].y.ToString("F1") + " " + rotSave.ToString("F3");       //if ROS needs corners AND rotation of the selectbox
        saveSelectBox = r[1].x.ToString("F1") + " " + r[1].y.ToString("F1") + " " + boxSizeXdim.ToString("F1") + " " + boxSizeYdim.ToString("F1") + " " + rotSave.ToString("F3");       //if ROS only needs Box without being capable of handling rotation
        robotControl.ItemSelect = saveSelectBox;                               // send coordinates to topic
        SendCoordinates.gameObject.SetActive(true);


    }

    static Vector2 RotateToXAxis(Vector2 v, float angle)
    {
        var newX = v.x * Mathf.Cos(angle) - v.y * Mathf.Sin(angle);
        var newY = v.x * Mathf.Sin(angle) + v.y * Mathf.Cos(angle);
        return new Vector2(newX, newY);
    }

    public void FeedDatabase(string successMessage)
    {        
        feedDatabase.WriteToDatabase("semiAutonomousDetectItem", successMessage);
    }
}
