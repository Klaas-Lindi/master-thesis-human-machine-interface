using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIChangeImage : MonoBehaviour {

    public Sprite ImageFirst;
    public Sprite ImageAfter;
    private bool toggle = false;

    // Use this for initialization
    void Start () {
        this.GetComponent<Image>().sprite = ImageFirst;
        toggle = false;
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    public void toggleImageOnClick()
    {
        toggle = !toggle;

        if (toggle)
        {
            this.GetComponent<Image>().sprite = ImageAfter;
        }
        else
        {
            this.GetComponent<Image>().sprite = ImageFirst;
        }
    }

    public void setImageOnClick(bool state)
    {
        toggle = state;

        if (toggle)
        {
            this.GetComponent<Image>().sprite = ImageAfter;
        }
        else
        {
            this.GetComponent<Image>().sprite = ImageFirst;
        }
    }
}
