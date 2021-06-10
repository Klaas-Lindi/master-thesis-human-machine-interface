using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Globalization;
using System.Threading;


public class SemiautonomousHandler : MonoBehaviour
{
    public RobotControlSAINT robotControl;
    public UISaintScreenHandler screenHandler;
    public MouseSelectItem mouseSelectItem;
    public SelectItem selectItem;
    public GraspPoseMarking graspPoseMarking;
    public MouseGraspPoseMarking mouseGraspPoseMarking;
    public MessageToUser messageToUser;
    public SendCoordinates sendCoordinates;
    public SendCallback sendCallback;
    public GenericMiniApp miniApp;

    // general UI elements
    public GameObject SendCoordinates;
    public GameObject telemetry;
    public GameObject switchButton;
    public GameObject stateButton;
    public GameObject control;
    public GameObject sequenceVisualizer;
    public GameObject actionWindow;
    public GameObject enlargeButton;
    public GameObject viewOptions;
    public GameObject screen3D;
    public GameObject screen2;
    public GameObject screenLongshot;
    public GameObject firstPersonCharacter;
    public GameObject advancedCommandWindow;
    public GameObject stateCondition;
    public GameObject stateOperation;

    // Generic Mini App
    public GameObject miniAppButton1;
    public GameObject miniAppButton2;
    public GameObject miniAppButton3;

    // SA detect touch
    public GameObject switchSemiAutonomousDetect;
    public GameObject itemMarkingObject;
    public GameObject saveAndSendLassoSelect;
    public GameObject resetLassoSelect;
    public GameObject backLassoSelect;

    // SA detect mouse
    public GameObject mouseSwitchSemiAutonomousDetect;
    public GameObject mouseItemMarkingObject;
    public GameObject mouseSaveAndSendLassoSelect;
    public GameObject mouseResetLassoSelect;
    public GameObject mouseBackLassoSelect;

    // SA grasp touch
    public GameObject switchSemiAutonomousGraspPoint;
    public GameObject graspPointMarkingObject;
    public GameObject saveAndSendGraspPoint;
    public GameObject backGraspPoint;

    // SA grasp mouse
    public GameObject mouseSwitchSemiAutonomousGraspPoint;
    public GameObject mouseGraspPointMarkingObject;
    public GameObject mouseSaveAndSendGraspPoint;

    // public GameObject mouseResetGraspPoint;
    public GameObject mouseBackGraspPoint;

    // SA general 
    public GameObject graspPointMarkingSprite;
    public GameObject selectBoxSprite;
    public GameObject startManualGrasping;
    public GameObject lassoSprite;
    public GameObject SendCoordinatesObject;

    // check if box is empty
    public GameObject BoxEmptyObject;
    public GameObject boxIsEmpty;
    public GameObject boxIsNotEmpty;

    //check if robot can continue
    public GameObject ContinueCycleObject;
    public GameObject continueCycle;
    private Quaternion firstPersonCharacterRotation;
    private Vector3 firstPersonCharacterPosition;

    string message;
    bool coordinatesReceived = false;
    bool callbackReceived = false;
    public bool touchOperation;
    public bool userStudyModeActive;
    public bool testMode;

    // String for storing the coordinates (content of topic "item_marking")
    private string coordinates = "";
    public string Coordinates
    {
        get
        {
            return coordinates;
        }

        set
        {
            coordinates = value;
        }
    }

    // String for storing the callback to ROS (content of topic "operator_callback")
    private string callback = "";
    public string Callback
    {
        get
        {
            return callback;
        }

        set
        {
            callback = value;
        }
    }

    void Update()
    {
        enlargeButton.gameObject.SetActive(false);
        if (userStudyModeActive)
        {
            //advancedCommandWindow.SetActive(false);
            telemetry.gameObject.SetActive(false);
            //stateButton.gameObject.SetActive(false);
            sequenceVisualizer.gameObject.SetActive(false);
            actionWindow.gameObject.SetActive(false);
            viewOptions.gameObject.SetActive(false);
            stateCondition.gameObject.SetActive(false);
            stateOperation.gameObject.SetActive(false);
        }

        if (testMode)                                                                           // for testing stuff
        {
            screenHandler.SAReady = false;                                                      // step 1
            StartCoroutine(WaitAndActivateSAView(1));                                           // step 2
            screenHandler.ActivateSAview();                                                     // step 3
            messageToUser.setMessage("Please mark a grasping point on an object, close to the center of the box");
            if (touchOperation)                                                                 // Touchscreen ops
            {
                switchSemiAutonomousGraspPoint.gameObject.SetActive(true);

            }
            else                                                                                // Mouse ops
            {
                mouseSwitchSemiAutonomousGraspPoint.gameObject.SetActive(true);

            }
            testMode = false;
        }

        if (coordinatesReceived)                                                                //for controlling the "back" buttons when coordinates were received.
        {
            mouseBackLassoSelect.gameObject.SetActive(false);
            backGraspPoint.gameObject.SetActive(false);
            backLassoSelect.gameObject.SetActive(false);
            mouseBackGraspPoint.gameObject.SetActive(false);
            coordinatesReceived = false;
        }

        if (callbackReceived)                                                                   //for controlling callback functions when callback was received
        {
            boxIsEmpty.gameObject.SetActive(false);
            boxIsNotEmpty.gameObject.SetActive(false);
            continueCycle.gameObject.SetActive(false);
            callbackReceived = false;
        }

        message = robotControl.Msg;
        switch (message)
        {
            ///SemiAutonomousDetectItem
            case "semiAutonomousDetectItem":
                screenHandler.SAReady = false;                                                      // step 1
                StartCoroutine(WaitAndActivateSAView(1));                                            // step 2
                screenHandler.ActivateSAview();                                                     // step 3
                messageToUser.setMessage("Please mark an object, close to the center of the box");
                if (touchOperation)                                                                 // Touchscreen ops
                {
                    switchSemiAutonomousDetect.gameObject.SetActive(true);
                    break;
                }
                else                                                                               // Mouse ops
                {
                    mouseSwitchSemiAutonomousDetect.gameObject.SetActive(true);
                    break;
                }

            case "Exit Semi Autonomous Item Marking":

                if (touchOperation)                                                                 // Touchscreen ops
                {
                    lassoSprite.gameObject.SetActive(false);
                    selectBoxSprite.gameObject.SetActive(false);
                    saveAndSendLassoSelect.gameObject.SetActive(false);
                    resetLassoSelect.gameObject.SetActive(false);
                    backLassoSelect.gameObject.SetActive(false);
                    switchSemiAutonomousDetect.gameObject.SetActive(false);
                    itemMarkingObject.gameObject.SetActive(false);
                    selectItem.DeleteLasso();
                    screenHandler.SAReady = true;
                    StartCoroutine(WaitAndDeactivateSAView());
                    screenHandler.DeactivateSAview();
                    break;
                }
                else                                                                                // Mouse ops
                {
                    lassoSprite.gameObject.SetActive(false);
                    selectBoxSprite.gameObject.SetActive(false);
                    mouseSaveAndSendLassoSelect.gameObject.SetActive(false);
                    mouseResetLassoSelect.gameObject.SetActive(false);
                    mouseBackLassoSelect.gameObject.SetActive(false);
                    mouseSwitchSemiAutonomousDetect.gameObject.SetActive(false);
                    mouseItemMarkingObject.gameObject.SetActive(false);
                    mouseSelectItem.DeleteLasso();
                    screenHandler.SAReady = true;
                    StartCoroutine(WaitAndDeactivateSAView());
                    screenHandler.DeactivateSAview();
                    break;
                }

            ///SemiAutonomousGraspItem

            case "semiAutonomousGraspItem":
                screenHandler.SAReady = false;
                StartCoroutine(WaitAndActivateSAView(1));
                screenHandler.ActivateSAview();
                messageToUser.setMessage("Please mark a grasping point on an object, close to the center of the box");
                if (touchOperation)                                                                 // Touchscreen ops
                {
                    switchSemiAutonomousGraspPoint.gameObject.SetActive(true);
                    break;
                }
                else                                                                                // Mouse ops
                {
                    mouseSwitchSemiAutonomousGraspPoint.gameObject.SetActive(true);
                    break;
                }

            case "Exit Semi Autonomous Grasp Pose Marking":
                if (touchOperation)                                                                 // Touchscreen ops
                {
                    graspPointMarkingSprite.gameObject.SetActive(false);
                    saveAndSendGraspPoint.gameObject.SetActive(false);
                    backGraspPoint.gameObject.SetActive(false);
                    switchSemiAutonomousGraspPoint.gameObject.SetActive(false);
                    graspPointMarkingObject.gameObject.SetActive(false);
                    graspPoseMarking.DeleteGraspPoseMarking();
                    screenHandler.SAReady = true;
                    StartCoroutine(WaitAndDeactivateSAView());
                    screenHandler.DeactivateSAview();
                    break;
                }
                else                                                                                // Mouse ops
                {
                    graspPointMarkingSprite.gameObject.SetActive(false);
                    mouseSaveAndSendGraspPoint.gameObject.SetActive(false);
                    //mouseResetFGraspPoint.gameObject.SetActive(false);
                    mouseBackGraspPoint.gameObject.SetActive(false);
                    mouseSwitchSemiAutonomousGraspPoint.gameObject.SetActive(false);
                    mouseGraspPointMarkingObject.gameObject.SetActive(false);
                    mouseGraspPoseMarking.DeleteGraspPoseMarking();
                    screenHandler.SAReady = true;
                    StartCoroutine(WaitAndDeactivateSAView());
                    screenHandler.DeactivateSAview();
                    break;
                }

            /// Box empty check

            case "Check if box is empty":
                screenHandler.SAReady = false;
                StartCoroutine(WaitAndActivateSAView(0));
                screenHandler.ActivateSAview();
                boxIsEmpty.gameObject.SetActive(true);
                boxIsNotEmpty.gameObject.SetActive(true);
                BoxEmptyObject.gameObject.SetActive(true);
                startManualGrasping.gameObject.SetActive(false);
                break;

            case "Exit box empty check":
                screenHandler.SAReady = true;
                StartCoroutine(WaitAndDeactivateSAView());
                screenHandler.DeactivateSAview();
                BoxEmptyObject.gameObject.SetActive(false);
                boxIsEmpty.gameObject.SetActive(false);
                boxIsNotEmpty.gameObject.SetActive(false);
                break;

            /// Surrounding check

            case "Check surrounding":
                screenHandler.SAReady = false;
                StartCoroutine(WaitAndActivateSAView(0));
                screenHandler.ActivateSAview();
                continueCycle.gameObject.SetActive(true);
                ContinueCycleObject.gameObject.SetActive(true);
                break;

            case "Exit check surrounding":
                screenHandler.SAReady = true;
                StartCoroutine(WaitAndDeactivateSAView());
                screenHandler.DeactivateSAview();
                ContinueCycleObject.gameObject.SetActive(false);
                continueCycle.gameObject.SetActive(false);
                break;
        }
    }

    public void ManualGraspingStarted()
    {
        robotControl.ItemSelect = "manual";                               // Telling ROS that teleoperation mode instead of semi autonomous interaction was selected
        SendCoordinates.gameObject.SetActive(true);
    }

    public void CoordinatesReceived()                               // Token for checking if coordinates were received
    {
        coordinatesReceived = true;
        sendCoordinates.Received();
    }

    public void CallbackReceived()                                  // Token for checking if callback was received
    {
        callbackReceived = true;
        sendCallback.Received();
    }

    public void DatabaseInput(string exitPoint)                     /// Feeding the database
    {
        if (graspPointMarkingObject.activeSelf)
        {
            graspPoseMarking.FeedDatabase(exitPoint);
        }

        if (mouseGraspPointMarkingObject.activeSelf)
        {
            mouseGraspPoseMarking.FeedDatabase(exitPoint);
        }

        if (itemMarkingObject.activeSelf)
        {
            selectItem.FeedDatabase(exitPoint);
        }

        if (mouseItemMarkingObject.activeSelf)
        {
            mouseSelectItem.FeedDatabase(exitPoint);
        }
    }

    public void StartMiniApp(string appLayout)
    {
        string[] words = appLayout.Split(' ');
        if (words[2] == "longshot")
        {
            screenHandler.ActivateLongshotview();
        }
        else if (words[2] == "ego")
        {
            screenHandler.ActivateSAview();
        }
        ActivateMiniAppView();

        if (words[1] == "1")                          // words[1] = number of buttons(1-3); words[2] = type of camera wanted ("longshot" or "ego"); words[3] = text button 1; words[4] = text button 2; words[5] = text button 3
        {
            miniAppButton1.gameObject.SetActive(true);
            miniAppButton1.GetComponentInChildren<Text>().text = words[3];
        }
        if (words[1] == "2")
        {
            miniAppButton1.gameObject.SetActive(true);
            miniAppButton2.gameObject.SetActive(true);
            miniAppButton1.GetComponentInChildren<Text>().text = words[3];
            miniAppButton2.GetComponentInChildren<Text>().text = words[4];
        }
        if (words[1] == "3")
        {
            miniAppButton1.gameObject.SetActive(true);
            miniAppButton2.gameObject.SetActive(true);
            miniAppButton3.gameObject.SetActive(true);
            miniAppButton1.GetComponentInChildren<Text>().text = words[3];
            miniAppButton2.GetComponentInChildren<Text>().text = words[4];
            miniAppButton3.GetComponentInChildren<Text>().text = words[5];
        }
    }

    public void StopMiniApp(string appLayout)
    {
        DeactivateMiniAppView();
        string[] words = appLayout.Split(' ');
        if (words[1] == "1")                          // words[1] = number of buttons(1-3); words[2] = type of camera wanted ("longshot" or "ego"); words[3] = text button 1; words[4] = text button 2; words[5] = text button 3
        {
            miniAppButton1.gameObject.SetActive(false);
        }
        if (words[1] == "2")
        {
            miniAppButton1.gameObject.SetActive(false);
            miniAppButton2.gameObject.SetActive(false);
        }
        if (words[1] == "3")
        {
            miniAppButton1.gameObject.SetActive(false);
            miniAppButton2.gameObject.SetActive(false);
            miniAppButton3.gameObject.SetActive(false);
        }
        if (words[2] == "longshot")
        {
            screenHandler.DeactivateLongshotview();
        }
        else screenHandler.DeactivateSAview();
    }

    bool ScreenSwitched()
    {
        if (screenHandler.SAReady)
            return true;
        else
            return false;
    }

    bool ScreenSwitchedBack()
    {
        if (!screenHandler.SAReady)
            return true;
        else
            return false;
    }

    IEnumerator WaitAndActivateSAView(int indexer)
    {
        yield return new WaitUntil(ScreenSwitched);
        ActivateSAView(indexer);                                                   // step 4
    }

    IEnumerator WaitAndDeactivateSAView()
    {
        yield return new WaitUntil(ScreenSwitchedBack);
        DeactivateSAView();
    }

    public void ActivateMiniAppView()
    {
        advancedCommandWindow.SetActive(false);
        telemetry.gameObject.SetActive(false);
        switchButton.gameObject.SetActive(false);
        stateButton.gameObject.SetActive(false);
        control.gameObject.SetActive(false);
        //enlargeButton.gameObject.SetActive(false);
        viewOptions.gameObject.SetActive(false);
        firstPersonCharacterRotation = firstPersonCharacter.transform.rotation;
        firstPersonCharacterPosition = firstPersonCharacter.transform.position;
    }

    public void DeactivateMiniAppView()
    {
        telemetry.gameObject.SetActive(true);
        switchButton.gameObject.SetActive(true);
        stateButton.gameObject.SetActive(true);
        control.gameObject.SetActive(true);
        //enlargeButton.gameObject.SetActive(true);
        viewOptions.gameObject.SetActive(true);
        firstPersonCharacterRotation = firstPersonCharacter.transform.rotation;
        firstPersonCharacterPosition = firstPersonCharacter.transform.position;
    }

    public void ActivateSAView(int indexer2)                                            // Routine for activating/deactivating the SA view: 1: set bool in UIsaintscreenhandler, 2:start the coroutine to wait until the screenhandler is finished, 3:start the screenhandler function
                                                                                        // the coroutine is waiting for to finish, 4: continue after coroutine has finished
    {
        advancedCommandWindow.SetActive(false);
        telemetry.gameObject.SetActive(false);
        switchButton.gameObject.SetActive(false);
        stateButton.gameObject.SetActive(false);
        control.gameObject.SetActive(false);
        //enlargeButton.gameObject.SetActive(false);
        viewOptions.gameObject.SetActive(false);
        //screen3D.gameObject.SetActive(false);
        //screen2.gameObject.SetActive(false);
        if (indexer2 == 1)
        {
            startManualGrasping.gameObject.SetActive(true);
        }
        firstPersonCharacterRotation = firstPersonCharacter.transform.rotation;
        firstPersonCharacterPosition = firstPersonCharacter.transform.position;
    }

    public void DeactivateSAView()
    {
        telemetry.gameObject.SetActive(true);
        switchButton.gameObject.SetActive(true);
        stateButton.gameObject.SetActive(true);
        control.gameObject.SetActive(true);
        //enlargeButton.gameObject.SetActive(true);
        viewOptions.gameObject.SetActive(true);
        //screen3D.gameObject.SetActive(true);
        //screen2.gameObject.SetActive(false);
        startManualGrasping.gameObject.SetActive(false);
        firstPersonCharacter.transform.rotation = firstPersonCharacterRotation;
        firstPersonCharacter.transform.position = firstPersonCharacterPosition;
        System.Threading.Thread.Sleep(250);
    }
}
