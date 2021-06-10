using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UiVrMenuAnimation : MonoBehaviour
{
    public bool enableAnimation = false;
    public Vector3 offset;
    public float transitionTime;
    public float transitionScale = 0.0f;

    private float timeButtonActivated;

    private RectTransform rt;
    private bool animationActivated = false;
    private bool buttonClicked = false;
    private bool mouseHover = false;
    private bool transitionFinished = false;
    private Vector3 origin_position;
    private Vector3 origin_scale;

    // Use this for initialization
    void Start()
    {
        rt = this.GetComponent<RectTransform>();
        origin_position = this.GetComponent<RectTransform>().localPosition;
        origin_scale = this.GetComponent<RectTransform>().localScale;
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
                    rt.localScale = origin_scale + new Vector3(transitionScale * (1 - (deltaTime / transitionTime)), transitionScale * (1 - (deltaTime / transitionTime)), transitionScale * (1 - (deltaTime / transitionTime)));
                    rt.localPosition = origin_position + new Vector3(offset.x * (1 - (deltaTime / transitionTime)), offset.y * (1 - (deltaTime / transitionTime)), offset.z * (1 - (deltaTime / transitionTime)));
                    this.setAlpha(deltaTime / transitionTime);
                }
                else
                {
                    rt.localScale = origin_scale;
                    rt.localPosition = origin_position;
                    this.setAlpha(1);
                    transitionFinished = true;
                }
            }
            else
            {
                if (deltaTime < transitionTime && rt.localPosition != (origin_position + offset))
                {
                    rt.localScale = origin_scale + new Vector3(transitionScale * (deltaTime / transitionTime), transitionScale * (deltaTime / transitionTime), transitionScale * (deltaTime / transitionTime));
                    rt.localPosition = origin_position + new Vector3(offset.x * (deltaTime / transitionTime), offset.y * (deltaTime / transitionTime), offset.z * (deltaTime / transitionTime));
                    this.setAlpha(1 - (deltaTime / transitionTime));
                }
                else
                {
                    rt.localScale = origin_scale + new Vector3(transitionScale, transitionScale, transitionScale);
                    rt.localPosition = origin_position + offset;
                    this.setAlpha(0);
                    transitionFinished = true;
                    print("Deactivated: "+gameObject.name);
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
        timeButtonActivated = Time.realtimeSinceStartup;
        animationActivated = state;
        transitionFinished = false;
    }

    public bool isTransitionFinished()
    {
        return transitionFinished;
    }
}
