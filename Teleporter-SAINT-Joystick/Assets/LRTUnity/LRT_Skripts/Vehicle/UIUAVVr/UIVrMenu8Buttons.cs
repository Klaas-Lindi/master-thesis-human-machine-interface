using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIVrMenu8Buttons : UIVrMenuButtons
{
    public RectTransform touchPadDummy;

    public Button buttonTopTopRight;
    public Button buttonRightTopRight;
    public Button buttonRightDownRight;
    public Button buttonDownDownRight;
    public Button buttonDownDownLeft;
    public Button buttonLeftDownLeft;
    public Button buttonLeftTopLeft;
    public Button buttonTopTopLeft;

    bool sectorChanged;

    override public void init(bool startState, float touchPadThresholdFromMiddle)
    {
        touchPosition = new Vector2();
        touchPadDummy.localScale = new Vector3(touchPadThresholdFromMiddle, touchPadThresholdFromMiddle, touchPadThresholdFromMiddle);
        uiAnimation = this.GetComponent<UiVrMenuAnimation>();
        touchTouched = false;
        touchPressed = false;
        elementActive = false;
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
        this.sectorChanged = false;

        if (touchTouched && (position.magnitude >= TouchPadThresholdFromMiddle))
        {
            // Calculate location in angle
            float phi = ((Mathf.Rad2Deg * Mathf.Atan2(position.y, position.x)) + 360) % 360;

            // Set Active Sector
            if (phi < 90 && phi >= 45)
                setActiveSektor(1);
            else if ((phi < 45 && phi >= 0))
                setActiveSektor(2);
            else if ((phi < 360 && phi >= 315))
                setActiveSektor(3);
            else if ((phi < 315 && phi >= 270))
                setActiveSektor(4);
            else if ((phi < 270 && phi >= 225))
                setActiveSektor(5);
            else if ((phi < 225 && phi >= 180))
                setActiveSektor(6);
            else if ((phi < 180 && phi >= 135))
                setActiveSektor(7); 
            else if ((phi < 135 && phi >= 90))
                setActiveSektor(8);

        }
        else
        {
            setActiveSektor(0);
        }

        if (this.sectorChanged)
        {
            touchPadDummy.localScale = new Vector3(touchPadThresholdFromMiddle, touchPadThresholdFromMiddle, touchPadThresholdFromMiddle);
            handleButtonAnimation(buttonTopTopRight, 1);
            handleButtonAnimation(buttonRightTopRight, 2);
            handleButtonAnimation(buttonRightDownRight, 3);
            handleButtonAnimation(buttonDownDownRight, 4);
            handleButtonAnimation(buttonDownDownLeft, 5);
            handleButtonAnimation(buttonLeftDownLeft, 6);
            handleButtonAnimation(buttonLeftTopLeft, 7);
            handleButtonAnimation(buttonTopTopLeft, 8);
        }
    }

    private void setActiveSektor(int index)
    {
        if (this.activeSector != index)
        {
            this.oldSector = this.activeSector;
            this.sectorChanged = true;
        }
        this.activeSector = index;
        
    }

    private void handleButtonAnimation(Button button, int index)
    {
        if (button != null && button.IsActive())
        {
            if (this.activeSector == index)
            {
                button.OnSelect(null);
                button.GetComponent<UIVrButtonAnimation>().setAnimationActive(true);
                button.OnSubmit(null);
            }
            if (this.oldSector == index)
            {
                button.OnDeselect(null);
                button.GetComponent<UIVrButtonAnimation>().setAnimationActive(false);
                button.OnSubmit(null);
            }
        }
    }
}
