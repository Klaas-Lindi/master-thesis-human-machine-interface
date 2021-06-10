using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIVrMenu4Buttons : UIVrMenuButtons
{
    public RectTransform touchPadDummy;

    public Button buttonTop;
    public Button buttonRight;
    public Button buttonDown;
    public Button buttonLeft;

    override public void init(bool startState, float touchPadThresholdFromMiddle)
    {
        touchPosition = new Vector2();
        touchTouched = false;
        touchPressed = false;
        elementActive = startState;
        activeSector = 0;
        oldSector = -1;
        this.gameObject.SetActive(startState);
        this.touchPadThresholdFromMiddle = touchPadThresholdFromMiddle;
        touchPadDummy.localScale = new Vector3(touchPadThresholdFromMiddle, touchPadThresholdFromMiddle, touchPadThresholdFromMiddle);
        uiAnimation = this.GetComponent<UiVrMenuAnimation>();
        uiAnimation.setAnimationActive(startState);
    }

    override public void setTouchPosition(Vector2 position)
    {
        touchPosition = position;
        bool sectorChanged = false;
        
        if (touchTouched)
        {
            if (position.magnitude >= TouchPadThresholdFromMiddle)
            {
                float phi = Mathf.Rad2Deg * Mathf.Atan2(position.y, position.x);
                //print("Position: " + touchPosition + "Phi: " + phi);
                if (phi < 135 && phi >= 45)
                {
                    if (activeSector != 1)
                    {
                        oldSector = activeSector;
                        sectorChanged = true;
                    }
                    activeSector = 1;
                }
                else if ((phi < 45 && phi >= 0) || (phi >= -45 && phi <= 0))
                {
                    if (activeSector != 2)
                    {
                        oldSector = activeSector;
                        sectorChanged = true;
                    }
                    activeSector = 2;
                }
                else if (phi < -45 && phi > -135)
                {
                    if (activeSector != 3)
                    {
                        oldSector = activeSector;
                        sectorChanged = true;
                    }
                    activeSector = 3;
                }
                else if (phi <= -135 || phi >= 135)
                {
                    if (activeSector != 4)
                    {
                        oldSector = activeSector;
                        sectorChanged = true;
                    }
                    activeSector = 4;
                }
            }
            else
            {
                if (activeSector != 0)
                {
                    oldSector = activeSector;
                    sectorChanged = true;
                }
                activeSector = 0;
            }
        }
        else
        {
            
            if (activeSector != 0)
            {
                oldSector = activeSector;
                sectorChanged = true;
            }
            activeSector = 0;
            
        }

        if (sectorChanged)
        {
            touchPadDummy.localScale = new Vector3(touchPadThresholdFromMiddle, touchPadThresholdFromMiddle, touchPadThresholdFromMiddle);

            if (buttonTop != null && buttonTop.IsActive())
            {
                if (activeSector == 1)
                {
                    buttonTop.OnSelect(null);
                    buttonTop.GetComponent<UIVrButtonAnimation>().setAnimationActive(true);
                    buttonTop.OnSubmit(null);
                }
                if (oldSector == 1)
                {
                    buttonTop.OnDeselect(null);
                    buttonTop.GetComponent<UIVrButtonAnimation>().setAnimationActive(false);
                    buttonTop.OnSubmit(null);
                }
            }

            if (buttonRight != null && buttonRight.IsActive())
            {
                if (activeSector == 2)
                {
                    buttonRight.OnSelect(null);
                    buttonRight.GetComponent<UIVrButtonAnimation>().setAnimationActive(true);
                    buttonRight.OnSubmit(null);
                }
                if (oldSector == 2)
                {
                    buttonRight.OnDeselect(null);
                    buttonRight.GetComponent<UIVrButtonAnimation>().setAnimationActive(false);
                    buttonRight.OnSubmit(null);
                }
            }

            if (buttonDown != null && buttonDown.IsActive())
            {
                if (activeSector == 3)
                {
                    buttonDown.OnSelect(null);
                    buttonDown.GetComponent<UIVrButtonAnimation>().setAnimationActive(true);
                    buttonDown.OnSubmit(null);
                }
                if (oldSector == 3)
                {
                    buttonDown.OnDeselect(null);
                    buttonDown.GetComponent<UIVrButtonAnimation>().setAnimationActive(false);
                    buttonDown.OnSubmit(null);
                }
            }

            if (buttonLeft != null && buttonLeft.IsActive())
            {
                if (activeSector == 4)
                {
                    buttonLeft.OnSelect(null);
                    buttonLeft.GetComponent<UIVrButtonAnimation>().setAnimationActive(true);
                    buttonLeft.OnSubmit(null);
                }
                if (oldSector == 4)
                {
                    buttonLeft.OnDeselect(null);
                    buttonLeft.GetComponent<UIVrButtonAnimation>().setAnimationActive(false);
                    buttonLeft.OnSubmit(null);
                }
            }
        }
    }
}
