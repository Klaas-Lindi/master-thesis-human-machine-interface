using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Runtime.InteropServices;

public class MouseGraspPoseMarking : MonoBehaviour
{
    [SerializeField]
    public RobotControlSAINT robotControl;
    public OperatorState operatorState;
    public MessageToUser messageToUser;
    public FeedDatabase feedDatabase;
    public RectTransform screen2Fade, graspPointMarkingSprite;
    string graspPoseMarkingString;
    Vector2 graspPoint, graspPointSave, scale, anglePoint;
    Vector3 r;
    float currentRotationAngle, saveRotate;
    public GameObject saveAndSendButton, SendCoordinatesObject;
    public int rsSizeX, rsSizeY;
    bool saved = false;

    void Start()
    {
        saveAndSendButton.gameObject.SetActive(false);
        graspPointMarkingSprite.gameObject.SetActive(false);
        graspPointMarkingSprite.transform.rotation = Quaternion.Euler(0, 0, 0);
        scale.x = rsSizeX;
        scale.y = rsSizeY;
    }

    void Update()
    {
        if (!saved)
        {
            if (Input.GetMouseButton(0))
            {
                graspPoint = Input.mousePosition;
                graspPointMarkingSprite.transform.position = graspPoint;
                graspPointMarkingSprite.gameObject.SetActive(true);
            }

            if (Input.GetMouseButton(1))
            {
                anglePoint = Input.mousePosition;
                if (graspPoint.y > anglePoint.y)                                                        // wenn Winkelfinger unter Positionsfinger, und...
                {
                    currentRotationAngle = Vector2.Angle(-anglePoint + graspPoint, Vector2.right);

                    if (graspPoint.x > anglePoint.x)                                                    // ...Winkelfinger links von Positionsfinger ist  
                    {
                        saveRotate = currentRotationAngle;                          //passt
                    }
                    else                                                                                // ...Winkelfinger rechts oder direkt unter Positionsfinger ist
                    {
                        saveRotate = currentRotationAngle - 180;
                    }
                }
                else                                                                                    // wenn Winkelfinger über oder auf Höhe von Positionsfinger, und...                                                                                                             
                {
                    currentRotationAngle = Vector2.Angle(anglePoint - graspPoint, Vector2.right);

                    if (graspPoint.x > anglePoint.x)                                                    // ...Winkelfinger links von Positionsfinger ist
                    {
                        saveRotate = currentRotationAngle - 180;
                    }
                    else                                                                                // ...Winkelfinger rechts oder direkt über Positionsfinger ist
                    {
                        saveRotate = currentRotationAngle;
                    }
                }
                graspPointMarkingSprite.transform.rotation = Quaternion.Euler(0, 0, currentRotationAngle);
                saveAndSendButton.gameObject.SetActive(true);
            }

            graspPointSave = graspPoint;
            graspPointMarkingSprite.transform.position = graspPointSave;

        }
    }

    //public void ResetGraspPoseMarking()
    //{
    //    saved = true;
    //    resetButton.gameObject.SetActive(false);
    //    graspPoint = new Vector2(800, 800);
    //    graspPointMarkingSprite.transform.rotation = Quaternion.Euler(0, 0, 0);
    //    graspPointMarkingSprite.gameObject.SetActive(true);
    //    saveButton.gameObject.SetActive(false);
    //    sendButton.gameObject.SetActive(false);
    //    graspPointMarkingSprite.transform.position = graspPoint;
    //    saved = false;
    //}

    //public void SaveGraspPoseMarking()
    //{
    //    saved = true;
    //    saveButton.gameObject.SetActive(false);
    //    graspPoint = graspPointSave;
    //    graspPointMarkingSprite.transform.position = graspPointSave;
    //    graspPointMarkingSprite.gameObject.SetActive(true);
    //    float width = screen2Fade.rect.width;
    //    float height = screen2Fade.rect.height;
    //    scale.x = rsSizeX / width;
    //    scale.y = rsSizeY / height;
    //    r.y = (height - graspPoint.y) * scale.y;
    //    r.x = graspPoint.x * scale.x;
    //    graspPoseMarkingString = r.x.ToString("F1") + " " + r.y.ToString("F1") + " " + saveRotate.ToString("F3");
    //    sendButton.gameObject.SetActive(true);
    //}

    public void SaveAndSendGraspPoseMarking()
    {
        saveAndSendButton.gameObject.SetActive(false);
        graspPoint = graspPointSave;
        graspPointMarkingSprite.transform.position = graspPointSave;
        graspPointMarkingSprite.gameObject.SetActive(true);
        float width = screen2Fade.rect.width;
        float height = screen2Fade.rect.height;
        scale.x = rsSizeX / width;
        scale.y = rsSizeY / height;
        r.y = (height -graspPoint.y) * scale.y;                //if UV-Rect-H value of Screen2fade == 1, this line has to be:      r.y = (height - graspPoint.y) * scale.y;   
        r.x = (graspPoint.x) * scale.x;
        graspPoseMarkingString = r.x.ToString("F1") + " " + r.y.ToString("F1") + " " + saveRotate.ToString("F3");
        graspPointMarkingSprite.transform.rotation = Quaternion.Euler(0, 0, 0);
        graspPointMarkingSprite.transform.position = new Vector2(800, 800);
        saveRotate = 0;
        robotControl.ItemSelect = graspPoseMarkingString;
        SendCoordinatesObject.gameObject.SetActive(true);         //always set robotControl.ItemSelect before calling this line
    }

    public void DeleteGraspPoseMarking()
    {
        graspPointMarkingSprite.transform.rotation = Quaternion.Euler(0, 0, 0);
        saveRotate = 0;
        graspPoint = new Vector2(800, 800);
        graspPointMarkingSprite.gameObject.SetActive(false);
        saved = false;
    }

    //public void SendGraspPoseMarking()
    //{
    //    graspPointMarkingSprite.transform.rotation = Quaternion.Euler(0, 0, 0);
    //    graspPoint = new Vector2(800, 800);
    //    saveRotate = 0;
    //    robotControl.ItemSelect = graspPoseMarkingString;
    //    SendCoordinatesObject.gameObject.SetActive(true);         //always set robotControl.ItemSelect before calling this line
    //    sendButton.gameObject.SetActive(false);
    //}

    public void FeedDatabase(string successMessage)
    {
        feedDatabase.WriteToDatabase("semiAutonomousGraspItem", successMessage);
    }
}