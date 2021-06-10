using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIButtonAnimation : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {

    public bool enableAnimation = false;
    public Vector3 offset;
    public float transitionTime;

    private float timeButtonActivated;

    private RectTransform rt;
    private bool animationActivated = false;
    private bool buttonClicked = false;
    private bool mouseHover = false;

    // Use this for initialization
    void Start () {
        rt = this.GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update()
    {
        if (enableAnimation)
        {
            float deltaTime = Time.realtimeSinceStartup - timeButtonActivated;
            if (animationActivated)
            {
                if (deltaTime < transitionTime)
                {
                    rt.anchoredPosition = new Vector3(offset.x * (deltaTime / transitionTime), offset.y * (deltaTime / transitionTime), offset.z * (deltaTime / transitionTime));
                    this.setAlpha(deltaTime / transitionTime);
                }
                else
                {
                    rt.anchoredPosition = offset;
                }
            }
            else
            {
                if (deltaTime < transitionTime)
                {
                    rt.anchoredPosition = new Vector3(offset.x * (1 - (deltaTime / transitionTime)), offset.y * (1 - (deltaTime / transitionTime)), offset.z * (1 - (deltaTime / transitionTime)));
                    this.setAlpha(1 - (deltaTime / transitionTime));
                }
                else
                {
                    rt.anchoredPosition = new Vector3();
                    this.gameObject.SetActive(false);
                }
            }
        }
    }

    private void setAlpha(float alpha)
    {
        foreach (Image tmp in this.GetComponentsInChildren<Image>())
        {
            tmp.color = new Color(tmp.color.r, tmp.color.g, tmp.color.b, alpha);
        }
    }

    public void setAnimationActive(bool state)
    {
        timeButtonActivated  = Time.realtimeSinceStartup;
        animationActivated = state;
    }

    public void buttonOnClick()
    {
        buttonClicked = true;
    }

    public bool GetButtonDown()
    {
        bool result = buttonClicked;
        buttonClicked = false;
        return result;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        mouseHover = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        mouseHover = false;
    }

    public bool GetMouseOverButton()
    {
        return mouseHover;
    }
}
