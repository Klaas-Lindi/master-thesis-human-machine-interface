using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIVrMenuButtons : MonoBehaviour
{
    protected Vector2 touchPosition;
    protected bool touchTouched;
    protected bool touchPressed;
    protected int activeSector;
    protected int oldSector;

    protected bool elementActive;
    protected UiVrMenuAnimation uiAnimation;

    protected float touchPadThresholdFromMiddle = 1.0f;

    public float TouchPadThresholdFromMiddle { get => touchPadThresholdFromMiddle; set => touchPadThresholdFromMiddle = value; }

    public virtual void init(bool startState, float touchPadThresholdFromMiddle)
    {
        touchPosition = new Vector2();
        touchTouched = false;
        touchPressed = false;
        elementActive = false;
        activeSector = 0;
        oldSector = -1;
        this.gameObject.SetActive(startState);
        this.touchPadThresholdFromMiddle = touchPadThresholdFromMiddle;
        uiAnimation = this.GetComponent<UiVrMenuAnimation>();
        uiAnimation.setAnimationActive(startState);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public virtual void setTouchPosition(Vector2 position)
    {
        touchPosition = position;
    }

    public virtual void setTouchTouched(bool state)
    {
        touchTouched = state;
        if (!state)
        {
            this.setTouchPosition(new Vector2());
        }
    }

    public virtual void setTouchPressed(bool state)
    {
        touchPressed = state;
    }
}
