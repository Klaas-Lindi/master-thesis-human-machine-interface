using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UISaintTransparency : MonoBehaviour
{
    private bool isVisible = true;

    private Color color;
    private Color transparent;
    private Color nonTransparent;

    void Awake()
    {
        isVisible = true;

        color = this.GetComponent<RawImage>().color;
        nonTransparent = new Color(
            color.r,
            color.g,
            color.b,
            1.0f);
        transparent = new Color(
            color.r,
            color.g,
            color.b,
            0.0f);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void toggleVisibility()
    {
        this.isVisible = !this.isVisible;
        if (isVisible)
        {
            this.GetComponent<RawImage>().color = color;
        }
        else
        {
            this.GetComponent<RawImage>().color = transparent;
        }
    }

    public void removeVisibilityHard()
    {
        this.GetComponent<RawImage>().color = transparent;
    }

    public void makeVisibleHard()
    {
        this.GetComponent<RawImage>().color = nonTransparent;
    }
}
