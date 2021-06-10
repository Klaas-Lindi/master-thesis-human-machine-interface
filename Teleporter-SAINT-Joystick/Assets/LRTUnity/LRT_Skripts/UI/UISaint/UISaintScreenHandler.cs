using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UISaintScreenHandler : MonoBehaviour
{ 
    public RawImage screenStationary;                   // supervising wpt
    public RawImage screenRealSense;
    public RawImage screenEgo;
    public RawImage screen3Dworld;

    private RectTransform rtStationary, rtStationarySave;
    private RectTransform rtRealSense, rtRealSenseSave;
    private RectTransform rtEgo, rtEgoSave;
    private RectTransform rt3Dworld, rt3DworldSave;

    private ScreenSettings oldStationary;
    private ScreenSettings oldRealsense;
    private ScreenSettings oldEgo;
    private ScreenSettings fullscreen;

    private UISaintTransparency uiStationary, uiStationarySave;
    private UISaintTransparency uiRealSense, uiRealSenseSave;
    private UISaintTransparency uiEgo, uiEgoSave;
    private UISaintTransparency ui3DWorld, ui3DWorldSave;

    private int currentMainScreen;
    private int currentSideScreen;

    private bool saReady = false;
    public bool SAReady
    {
        get
        {
            return saReady;
        }

        set
        {
            saReady = value;
        }
    }

    void Start()
    {
        rtStationary = screenStationary.GetComponent<RectTransform>();                                  // size of screen
        rtRealSense = screenRealSense.GetComponent<RectTransform>();
        rtEgo = screenEgo.GetComponent<RectTransform>();
        rt3Dworld = screen3Dworld.GetComponent<RectTransform>();

        rtStationarySave = rtStationary;
        rtRealSenseSave = rtRealSense;
        rtEgoSave = rtEgo;
        rt3DworldSave = rt3Dworld;

        uiStationary = screenStationary.GetComponent<UISaintTransparency>();                            // colour and visibility
        uiRealSense = screenRealSense.GetComponent<UISaintTransparency>();
        uiEgo = screenEgo.GetComponent<UISaintTransparency>();
        ui3DWorld = screen3Dworld.GetComponent<UISaintTransparency>();

        uiStationarySave = uiStationary;
        uiRealSenseSave = uiRealSense;
        uiEgoSave = uiEgo;
        ui3DWorldSave = ui3DWorld;

        fullscreen = new ScreenSettings();
        oldRealsense = new ScreenSettings();
        oldEgo = new ScreenSettings();
        oldStationary = new ScreenSettings();

        fullscreen.offsetMin = Vector2.zero;
        fullscreen.offsetMax = Vector2.zero;
        fullscreen.anchorMin = Vector2.zero;
        fullscreen.anchorMax = Vector2.one;
        fullscreen.sizeDelta = Vector2.zero;

        getRTValues(rtStationary, oldStationary);
        getRTValues(rtRealSense, oldRealsense);
        getRTValues(rtEgo, oldEgo);

        currentMainScreen = 0;
        currentSideScreen = 2;
        uiStationary.toggleVisibility();
        //ui3DWorld.toggleVisibility();

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void enlargeScreen()
    {
        //print("CurrentMain: " + currentMainScreen + " CurrentSide: " + currentSideScreen);
        switch (currentMainScreen)
        {
            case 1:
                setRTValues(rtStationary, oldStationary);

                if (currentSideScreen == 2)
                {
                    screen3Dworld.gameObject.SetActive(true);
                    setRTValues(rtRealSense, fullscreen);
                    setRTValues(rtEgo, fullscreen);
                    screenRealSense.transform.SetSiblingIndex(0);
                    screenEgo.transform.SetSiblingIndex(0);

                }
                else if (currentSideScreen == 0)
                {
                    screen3Dworld.gameObject.SetActive(false);
                }
                //screenStationary.transform.SetSiblingIndex(0);

                break;
            case 2:
                setRTValues(rtRealSense, oldRealsense);
                setRTValues(rtEgo, oldEgo);

                if (currentSideScreen == 1)
                {
                    screen3Dworld.gameObject.SetActive(true);
                    setRTValues(rtStationary, fullscreen);
                    screenStationary.transform.SetSiblingIndex(0);

                }
                else if (currentSideScreen == 0)
                {
                    screen3Dworld.gameObject.SetActive(false);
                }

                break;
            case 0:
                screen3Dworld.gameObject.SetActive(true);
                //ui3DWorld.toggleVisibility();

                if (currentSideScreen == 1)
                {
                    setRTValues(rtStationary, fullscreen);
                    screenStationary.transform.SetSiblingIndex(0);
                }
                else if (currentSideScreen == 2)
                {
                    setRTValues(rtRealSense, fullscreen);
                    setRTValues(rtEgo, fullscreen);
                }
                break;
        }
        int tmp = currentSideScreen;
        currentSideScreen = currentMainScreen;
        currentMainScreen = tmp;

        //print("CurrentMain setted to " + currentMainScreen + " CurrentSide setted to " + currentSideScreen);


    }

    public void switchScreen()
    {
        //print("CurrentMain: " + currentMainScreen + " CurrentSide: " + currentSideScreen);

        switch (currentSideScreen)
        {
            case 1: // Camera Stationary Screen      
                uiStationary.toggleVisibility();
                if (currentMainScreen == 0)
                {
                    uiRealSense.toggleVisibility();
                    uiEgo.toggleVisibility();
                    currentSideScreen = 2;
                }
                else if (currentMainScreen == 2)
                {
                    ui3DWorld.toggleVisibility();
                    currentSideScreen = 0;
                }

                break;
            case 2: // Camera Ego Screen
                uiRealSense.toggleVisibility();
                uiEgo.toggleVisibility();
                if (currentMainScreen == 0)
                {
                    uiStationary.toggleVisibility();
                    currentSideScreen = 1;
                }
                else if (currentMainScreen == 1)
                {
                    ui3DWorld.toggleVisibility();
                    currentSideScreen = 0;
                }

                break;
            case 0: // Main 3D World Screen
                ui3DWorld.toggleVisibility();
                if (currentMainScreen == 1)
                {
                    uiRealSense.toggleVisibility();
                    uiEgo.toggleVisibility();
                    currentSideScreen = 2;
                }
                else if (currentMainScreen == 2)
                {
                    uiStationary.toggleVisibility();
                    currentSideScreen = 1;
                }

                break;
        }
        //print("CurrentSide setted to " + currentSideScreen);
    }

    public void toggleHide()
    {
        switch (currentMainScreen)
        {
            case 1: // Camera Stationary Screen      
                if (currentSideScreen == 0)
                {
                    ui3DWorld.toggleVisibility();
                }
                else if (currentSideScreen == 2)
                {
                    uiRealSense.toggleVisibility();
                    uiEgo.toggleVisibility();
                }
                break;
            case 2: // Camera Ego Screen
                if (currentSideScreen == 0)
                {
                    ui3DWorld.toggleVisibility();
                }
                else if (currentSideScreen == 1)
                {
                    uiStationary.toggleVisibility();
                }
                break;
            case 0: // Main 3D World Screen
                if (currentSideScreen == 1)
                {
                    uiStationary.toggleVisibility();
                }
                else if (currentSideScreen == 2)
                {
                    uiRealSense.toggleVisibility();
                    uiEgo.toggleVisibility();
                }
                break;
        }
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

    public void ActivateSAview()
    {
        SAReady = false;
        Debug.Log("ego");
        screen3Dworld.gameObject.SetActive(true);

        rtStationarySave = screenStationary.GetComponent<RectTransform>();
        rtRealSenseSave = screenRealSense.GetComponent<RectTransform>();
        rtEgoSave = screenEgo.GetComponent<RectTransform>();
        rt3DworldSave = screen3Dworld.GetComponent<RectTransform>();

        uiStationarySave = screenStationary.GetComponent<UISaintTransparency>();
        uiRealSenseSave = screenRealSense.GetComponent<UISaintTransparency>();
        uiEgoSave = screenEgo.GetComponent<UISaintTransparency>();
        ui3DWorldSave = screen3Dworld.GetComponent<UISaintTransparency>();

        ui3DWorld.removeVisibilityHard();                                                       // set transparencies according to SA view
        uiEgo.removeVisibilityHard();
        uiStationary.removeVisibilityHard();
        uiRealSense.makeVisibleHard();

        setRTValues(rtRealSense, fullscreen);                                                   // set SA view to fullscreen

        screen3Dworld.gameObject.SetActive(false);

        SAReady = true;
    }

    public void ActivateLongshotview()
    {
        Debug.Log("longshot");
        screen3Dworld.gameObject.SetActive(true);

        rtStationarySave = screenStationary.GetComponent<RectTransform>();
        rtRealSenseSave = screenRealSense.GetComponent<RectTransform>();
        rtEgoSave = screenEgo.GetComponent<RectTransform>();
        rt3DworldSave = screen3Dworld.GetComponent<RectTransform>();

        uiStationarySave = screenStationary.GetComponent<UISaintTransparency>();
        uiRealSenseSave = screenRealSense.GetComponent<UISaintTransparency>();
        uiEgoSave = screenEgo.GetComponent<UISaintTransparency>();
        ui3DWorldSave = screen3Dworld.GetComponent<UISaintTransparency>();

        ui3DWorld.removeVisibilityHard();                                                       // set transparencies according to SA view
        uiEgo.removeVisibilityHard();
        uiRealSense.removeVisibilityHard();
        uiStationary.makeVisibleHard();

        setRTValues(rtStationary, fullscreen);                                                  // set SA view to fullscreen

        screen3Dworld.gameObject.SetActive(false);
    }

    public void DeactivateLongshotview()
    {
        screen3Dworld.gameObject.SetActive(true);

        rtStationary = rtStationarySave;
        rtRealSense = rtRealSenseSave;
        rtEgo = rtEgoSave;
        rt3Dworld = rt3DworldSave;

        uiStationary = uiStationarySave;
        uiRealSense = uiRealSenseSave;
        uiRealSense.makeVisibleHard();
        uiEgo = uiEgoSave;
        ui3DWorld = ui3DWorldSave;

        setRTValues(rtStationary, oldStationary);
        setRTValues(rtRealSense, oldRealsense);
        setRTValues(rtEgo, oldEgo);
    }

    public void DeactivateSAview()
    {
        SAReady = true;

        screen3Dworld.gameObject.SetActive(true);

        rtStationary = rtStationarySave;
        rtRealSense = rtRealSenseSave;
        rtEgo = rtEgoSave;
        rt3Dworld = rt3DworldSave;

        uiStationary = uiStationarySave;
        uiRealSense = uiRealSenseSave;
        uiRealSense.makeVisibleHard();
        uiEgo = uiEgoSave;
        ui3DWorld = ui3DWorldSave;

        setRTValues(rtStationary, oldStationary);
        setRTValues(rtRealSense, oldRealsense);
        setRTValues(rtEgo, oldEgo);

        SAReady = false;
    }
}
