using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UiStreamScreen : MonoBehaviour {

    public Renderer streamScreen;

    public float minTransperancy = 0.1f;
    public float speedTransition = 0.1f;

    private bool enableTransparat = false;

    private RawImage rawImage;
    private CanvasGroup childPanel;

    public bool EnableTransparat
    {
        get
        {
            return enableTransparat;
        }

        set
        {
            enableTransparat = value;
        }
    }

    // Use this for initialization
    void Start () {
        this.rawImage = this.GetComponentInChildren<RawImage>();
        childPanel = this.GetComponentInChildren<CanvasGroup>();
        childPanel.alpha = 1;
    }
	
	// Update is called once per frame
	void Update () {
        if (streamScreen.material.mainTexture != null)
            this.rawImage.texture = streamScreen.material.mainTexture;

        if (this.EnableTransparat)
        {
            if(childPanel.alpha > minTransperancy)
                childPanel.alpha -= speedTransition;
        }
        else 
        {
            if (childPanel.alpha <= 1)
                childPanel.alpha += speedTransition;
        }
    }
}
