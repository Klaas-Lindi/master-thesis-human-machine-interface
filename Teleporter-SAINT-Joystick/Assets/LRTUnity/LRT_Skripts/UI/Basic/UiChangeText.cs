using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UiChangeText : MonoBehaviour {

    public string TextFirst;
    public string TextAfter;
    private bool toggle = false;

	// Use this for initialization
	void Start () {
        this.GetComponent<Text>().text = TextFirst;
        toggle = false;
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void toggleTextOnClick()
    {
        toggle = !toggle;

        if(toggle)
        {
            this.GetComponent<Text>().text = TextAfter;
        }
        else
        {
            this.GetComponent<Text>().text = TextFirst;
        }
    }
}
