using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIVrButtonAnimation : MonoBehaviour
{
    public bool enableAnimation = false;
    public Vector3 offset;
    public float transitionTime;
    public float transitionScale = 0.0f;

    private float timeButtonActivated;

    private RectTransform rt;
    private bool animationActivated = false;
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
                    rt.localScale = origin_scale + new Vector3(transitionScale * (deltaTime / transitionTime), transitionScale * (deltaTime / transitionTime), transitionScale * (deltaTime / transitionTime));
                    rt.localPosition = origin_position + new Vector3(offset.x * (deltaTime / transitionTime), offset.y * (deltaTime / transitionTime), offset.z * (deltaTime / transitionTime));
                }
                else
                {
                    rt.localScale = origin_scale + new Vector3(transitionScale, transitionScale, transitionScale);
                    rt.localPosition = origin_position + offset;
                }
            }
            else
            {
                if (deltaTime < transitionTime && rt.localPosition != origin_position)
                {
                    rt.localScale = origin_scale + new Vector3(transitionScale * (1 - (deltaTime / transitionTime)), transitionScale * (1 - (deltaTime / transitionTime)), transitionScale * (1 - (deltaTime / transitionTime)));
                    rt.localPosition = origin_position + new Vector3(offset.x * (1 - (deltaTime / transitionTime)), offset.y * (1 - (deltaTime / transitionTime)), offset.z * (1 - (deltaTime / transitionTime)));
                }
                else
                {
                    rt.localScale = origin_scale;
                    rt.localPosition = origin_position;
                }
            }
        }
    }

    public void setAnimationActive(bool state)
    {
        timeButtonActivated = Time.realtimeSinceStartup;
        animationActivated = state;
    }
}
