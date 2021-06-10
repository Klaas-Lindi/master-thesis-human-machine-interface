using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WaypointScreenHandler : MonoBehaviour
{
    public Camera mainCamera;

    public GameObject XZxSlider;
    public GameObject XZzSlider;
    public GameObject XYxSlider;
    public GameObject XYySlider;
    public GameObject ZYzSlider;
    public GameObject ZYySlider;

    //int cullingMask = mainCamera.cullingMask;
    public GameObject XYcam;
    public GameObject ZYcam;
    public GameObject XZcam;

    public GameObject switchToXYView;
    public GameObject switchToZYView;
    public GameObject switchToXZView;

    public RawImage XYwpt;                              //waypoint marking screens
    public RawImage ZYwpt;
    public RawImage XZwpt;

    private RectTransform rtXYwpt;
    private RectTransform rtZYwpt;
    private RectTransform rtXZwpt;

    private UISaintTransparency uiXYwpt;
    private UISaintTransparency uiZYwpt;
    private UISaintTransparency uiXZwpt;

    private ScreenSettings gone;
    private ScreenSettings fullscreen;

    void Start()
    {
        rtXYwpt = XYwpt.GetComponent<RectTransform>();
        rtZYwpt = ZYwpt.GetComponent<RectTransform>();
        rtXZwpt = XZwpt.GetComponent<RectTransform>();

        uiXYwpt = XYwpt.GetComponent<UISaintTransparency>();
        uiZYwpt = ZYwpt.GetComponent<UISaintTransparency>();
        uiXZwpt = XZwpt.GetComponent<UISaintTransparency>();

        gone = new ScreenSettings();
        fullscreen = new ScreenSettings();
        fullscreen.offsetMin = Vector2.zero;
        fullscreen.offsetMax = Vector2.zero;
        fullscreen.anchorMin = Vector2.zero;
        fullscreen.anchorMax = Vector2.one;
        fullscreen.sizeDelta = Vector2.zero;

        getRTValues(rtXYwpt, gone);

        XYcam.gameObject.SetActive(false);
        ZYcam.gameObject.SetActive(false);
        XZcam.gameObject.SetActive(false);

    }

    void getRTValues(RectTransform orignal, ScreenSettings setting)
    {
        setting.offsetMin = orignal.offsetMin;
        setting.offsetMax = orignal.offsetMax;
        setting.anchorMin = orignal.anchorMin;
        setting.anchorMax = orignal.anchorMax;
        setting.sizeDelta = orignal.sizeDelta;
    }

    void setRTValues(RectTransform orignal, ScreenSettings setter)
    {
        orignal.offsetMin = setter.offsetMin;
        orignal.offsetMax = setter.offsetMax;
        orignal.anchorMin = setter.anchorMin;
        orignal.anchorMax = setter.anchorMax;
        orignal.sizeDelta = setter.sizeDelta;
    }

    public class ScreenSettings
    {
        public Vector2 offsetMin;
        public Vector2 offsetMax;
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 sizeDelta;

        public ScreenSettings()
        {
            this.offsetMin = new Vector2();
            this.offsetMin = Vector2.zero;
            this.offsetMax = new Vector2();
            this.offsetMax = Vector2.zero;
            this.anchorMin = new Vector2();
            this.anchorMin = Vector2.zero;
            this.anchorMax = new Vector2();
            this.anchorMax = Vector2.one;
            this.sizeDelta = new Vector2();
            this.sizeDelta = Vector2.zero;
        }
    }

    public void ActivateWaypointMarkingView()
    {
        XZxSlider.gameObject.SetActive(true);
        XZzSlider.gameObject.SetActive(true);
        switchToXYView.gameObject.SetActive(true);
        switchToZYView.gameObject.SetActive(true);
        mainCamera.cullingMask = 0;
        XZcam.gameObject.SetActive(true);
        setRTValues(rtXZwpt, fullscreen);
        XZwpt.transform.SetSiblingIndex(0);
        uiXZwpt.makeVisibleHard();
    }

    public void SwitchWaypointScreen(string desiredView)
    {
        switch (desiredView)
        {
            case "XY":

                XYxSlider.gameObject.SetActive(true);
                XYySlider.gameObject.SetActive(true);
                ZYySlider.gameObject.SetActive(false);
                ZYzSlider.gameObject.SetActive(false);
                XZxSlider.gameObject.SetActive(false);
                XZzSlider.gameObject.SetActive(false);

                XYcam.gameObject.SetActive(true);
                ZYcam.gameObject.SetActive(false);
                XZcam.gameObject.SetActive(false);

                setRTValues(rtXYwpt, fullscreen);
                setRTValues(rtZYwpt, gone);
                setRTValues(rtXZwpt, gone);

                uiXYwpt.makeVisibleHard();
                uiZYwpt.removeVisibilityHard();
                uiXZwpt.removeVisibilityHard();
                break;

            case "ZY":

                XYxSlider.gameObject.SetActive(false);
                XYySlider.gameObject.SetActive(false);
                ZYySlider.gameObject.SetActive(true);
                ZYzSlider.gameObject.SetActive(true);
                XZxSlider.gameObject.SetActive(false);
                XZzSlider.gameObject.SetActive(false);

                XYcam.gameObject.SetActive(false);
                ZYcam.gameObject.SetActive(true);
                XZcam.gameObject.SetActive(false);

                setRTValues(rtXYwpt, gone);
                setRTValues(rtZYwpt, fullscreen);
                setRTValues(rtXZwpt, gone);

                uiXYwpt.removeVisibilityHard();
                uiZYwpt.makeVisibleHard();
                uiXZwpt.removeVisibilityHard();
                break;

            case "XZ":

                XYxSlider.gameObject.SetActive(false);
                XYySlider.gameObject.SetActive(false);
                ZYySlider.gameObject.SetActive(false);
                ZYzSlider.gameObject.SetActive(false);
                XZxSlider.gameObject.SetActive(true);
                XZzSlider.gameObject.SetActive(true);

                XYcam.gameObject.SetActive(false);
                ZYcam.gameObject.SetActive(false);
                XZcam.gameObject.SetActive(true);

                setRTValues(rtXYwpt, gone);
                setRTValues(rtZYwpt, gone);
                setRTValues(rtXZwpt, fullscreen);

                uiXYwpt.removeVisibilityHard();
                uiZYwpt.removeVisibilityHard();
                uiXZwpt.makeVisibleHard();
                break;

            default:
                print("Error! Only 'XY', 'ZY' and 'XZ' available as arguments");
                break;
        }
    }

    public void DeactivateWaypointMarkingView()
    {
        XYxSlider.gameObject.SetActive(false);
        XYySlider.gameObject.SetActive(false);
        ZYySlider.gameObject.SetActive(false);
        ZYzSlider.gameObject.SetActive(false);
        XZxSlider.gameObject.SetActive(false);
        XZzSlider.gameObject.SetActive(false);

        switchToXYView.gameObject.SetActive(false);
        switchToZYView.gameObject.SetActive(false);
        switchToXZView.gameObject.SetActive(false);

        mainCamera.cullingMask = -1;                                // sets the culling mask to "everything" so everything that the camera is on gets rendered
        
        setRTValues(rtXYwpt, gone);
        setRTValues(rtZYwpt, gone);
        setRTValues(rtXZwpt, gone);
        
        uiXYwpt.removeVisibilityHard();
        uiZYwpt.removeVisibilityHard();
        uiXZwpt.removeVisibilityHard();
        
        XYcam.gameObject.SetActive(false);
        ZYcam.gameObject.SetActive(false);
        XZcam.gameObject.SetActive(false);
    }
}