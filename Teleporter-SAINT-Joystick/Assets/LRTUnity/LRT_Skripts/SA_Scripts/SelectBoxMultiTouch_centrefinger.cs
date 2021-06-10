using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Runtime.InteropServices;

// Kanal über den Unity an ROS Informationen publisht: operator_command_id = RosSocket.Advertise<std_msgs.String>("operator_commands");

public class SelectBoxMultiTouch_centrefinger : MonoBehaviour
{

    [SerializeField]


    public RectTransform selectSquareSprite;
    public RobotControlSAINT robotControl;
    public OperatorState operatorState;
    string saveSelectBox;
    Vector2 centre, centreDelta, lowerLeft, lowerLeftDelta, centreCurr_lowerLeftCurr, centreCurr, lowerLeftCurr, centresave, centrenew;
    Vector2 squareStart, scale;
    float current_rotationangle, initAngle, initAngleCompare, initAngleDelta, rotationChange, rotSave;
    float sizeY, sizeX, sizeYsave, sizeXsave;
    int passtoken = 0;
    public RectTransform screen2Fade;
    public GameObject resetButton, saveButton, sendButton;                  //automatisches (de)aktivieren der Buttons
    public int rsSizeX, rsSizeY;
    public FeedDatabase feedDatabase;


    void Start()
    {
        sendButton.gameObject.SetActive(false);
        saveButton.gameObject.SetActive(false);
        resetButton.gameObject.SetActive(false);
        //screen2Fade = GetComponent<RectTransform>();
        scale.x = rsSizeX;
        scale.y = rsSizeY;

    }

    void Update()
    {
        switch (Input.touchCount)
        {
            case 1:

                Touch t0 = Input.GetTouch(0);
                centrenew = t0.position;
                centreDelta = centre - centrenew;
                centre = centre - centreDelta * passtoken;
                selectSquareSprite.position = centre;

                break;

            case 2:

                passtoken = 1;
                t0 = Input.GetTouch(0);
                centre = t0.position;
                Touch t1 = Input.GetTouch(1);
                lowerLeft = t1.position;
                centreCurr_lowerLeftCurr = lowerLeft - centre;
                initAngle = Vector2.Angle(centreCurr_lowerLeftCurr, Vector2.right);


                sizeX = Mathf.Abs(centre.x - lowerLeft.x) * 2f;
                sizeY = Mathf.Abs(centre.y - lowerLeft.y) * 2f;
                selectSquareSprite.position = centre;
                selectSquareSprite.sizeDelta = new Vector2(sizeX, sizeY);
                selectSquareSprite.gameObject.SetActive(true);
                resetButton.gameObject.SetActive(true);
                saveButton.gameObject.SetActive(true);
                break;

            case 3:

                t0 = Input.GetTouch(0);
                centreCurr = t0.position;
                centre = centreCurr;
                //centreDelta = centreCurr - centre;
                //centre = centre + centreDelta;

                t1 = Input.GetTouch(1);
                lowerLeftCurr = t1.position;
                centreCurr_lowerLeftCurr = lowerLeftCurr - centreCurr;
                current_rotationangle = Vector2.Angle(centreCurr_lowerLeftCurr, Vector2.right);
                rotationChange = initAngle - current_rotationangle;
                initAngle = initAngle - rotationChange;
                rotSave = rotSave + rotationChange;                                          // for storing overall rotation value
                selectSquareSprite.transform.Rotate(0.0f, 0.0f, rotationChange, Space.Self);

                sizeX = Mathf.Abs(centre.x - lowerLeft.x) * 2f;
                sizeY = Mathf.Abs(centre.y - lowerLeft.y) * 2f;
                selectSquareSprite.position = centre;
                selectSquareSprite.sizeDelta = new Vector2(sizeX, sizeY);
                selectSquareSprite.gameObject.SetActive(true);
                break;

            default:

                sizeXsave = sizeX;
                sizeYsave = sizeY;
                centresave = centre;
                //selectSquareSprite.position = centresave;
                //selectSquareSprite.sizeDelta = new Vector2(sizeXsave, sizeYsave);
                //selectSquareSprite.gameObject.SetActive(true);
                break;
        }
    }

    public void ResetSquare()
    {
        selectSquareSprite.gameObject.SetActive(false);
        System.Threading.Thread.Sleep(50);
        selectSquareSprite.transform.Rotate(0.0f, 0.0f, -rotSave, Space.Self);
        centre = centre * 0;
        lowerLeft = lowerLeft * 0;
        rotSave = 0f;
        sizeX = Mathf.Abs(centre.x - lowerLeft.x) * 2f;
        sizeY = Mathf.Abs(centre.y - lowerLeft.y) * 2f;
        selectSquareSprite.position = centre;
        selectSquareSprite.sizeDelta = new Vector2(sizeX, sizeY);
        passtoken = 0;
        resetButton.gameObject.SetActive(false);
        sendButton.gameObject.SetActive(false);
        saveButton.gameObject.SetActive(false);
    }

    public void SaveSelectbox()
    {
        selectSquareSprite.gameObject.SetActive(false);
        System.Threading.Thread.Sleep(100);
        centre = centresave;
        sizeX = sizeXsave;
        sizeY = sizeYsave;
        passtoken = 0;
        selectSquareSprite.position = centresave;
        selectSquareSprite.sizeDelta = new Vector2(sizeXsave, sizeYsave);
        selectSquareSprite.gameObject.SetActive(true);
        float width = screen2Fade.rect.width;
        float height = screen2Fade.rect.height;
        scale.x = rsSizeX / width;
        scale.y = rsSizeY / height;
        Vector3[] r = new Vector3[4];
        Vector3[] q = new Vector3[2];
        q[0].y = (height - centresave.y) * scale.y;     // y coordinate of centre
        q[0].x = centresave.x * scale.x;                // x coordinate of centre
        q[1].y = sizeYsave * scale.y;                   // y scale of rectangle     
        //q[1].x = sizeXsave * scale.x;                   // x scale of rectangle
        //selectSquareSprite.GetWorldCorners(r);
        //r[0].y = (height - r[0].y) * scale.y;
        //r[1].y = (height - r[1].y) * scale.y;
        //r[2].y = (height - r[2].y) * scale.y;
        //r[3].y = (height - r[3].y) * scale.y;
        //r[0].x = r[0].x * scale.x;
        //r[1].x = r[1].x * scale.x;
        //r[2].x = r[2].x * scale.x;
        //r[3].x = r[3].x * scale.x;


        //saveSelectBox = r[1].x.ToString("F1") + " " + r[1].y.ToString("F1") + " " + r[3].x.ToString("F1") + " " + r[3].y.ToString("F1") + " " + rotSave.ToString("F3");       //if ROS wants corners and rotation of the selectbox
        saveSelectBox = q[0].x.ToString("F1") + " " + q[0].y.ToString("F1") + " " + q[1].x.ToString("F1") + " " + q[1].y.ToString("F1") + " " + rotSave.ToString("F3");         //if ROS wants size, centre and angle of the selectbox

        saveButton.gameObject.SetActive(false);
        sendButton.gameObject.SetActive(true);
    }

    public void DeleteSelectbox()
    {
        selectSquareSprite.transform.Rotate(0.0f, 0.0f, -rotSave, Space.Self);
        selectSquareSprite.gameObject.SetActive(false);
    }

    public void SendSelectbox()
    {
        robotControl.ItemSelect = saveSelectBox;
        sendButton.gameObject.SetActive(false);
    }

    private bool IsPointerOverUIObject(Touch t0)
    {
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = new Vector2(t0.position.y, t0.position.x);
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        return results.Count > 3;
    }
}
