using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UISlider : MonoBehaviour {

    public float min;
    public float max;
    private float value;

    public string textPrefix;
    public string textPostfix;
    public GameObject UiTextObject;
    public Vector3 textOffset = new Vector3(0, 50, 0);

	// Use this for initialization
	void Start () {
        SetPrecantage(this.GetComponentInChildren<Slider>().value);
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    public void SetPrecantage(float precentage)
    {
        value = (max - min) * precentage + min;
    }

    public void SetValueFloat(float value)
    {
        if (value > max)
        {
            this.value = max;
        }
        else if(value < min)
        {
            this.value = min;
        }
        else
        {
            this.value = value;
        }
    }

    public float GetValue()
    {
        return value;
    }

    public void showText()
    {
        UiTextObject.GetComponent<UiTextAnimation>().startDeclineAnimation();
        UiTextObject.GetComponentInChildren<Text>().rectTransform.anchoredPosition3D = Input.mousePosition + textOffset;
        UiTextObject.GetComponentInChildren<Text>().text = textPrefix + value.ToString("0.00") + textPostfix;
    }
}
