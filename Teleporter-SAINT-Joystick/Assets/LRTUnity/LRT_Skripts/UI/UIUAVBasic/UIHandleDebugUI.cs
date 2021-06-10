using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIHandleDebugUI : MonoBehaviour {

    public GameObject DebugUI;

    private RectTransform rect;

	// Use this for initialization
	void Start () {
        rect = this.GetComponent<RectTransform>();
        if (DebugUI.activeSelf)
        {
            rect.anchoredPosition.Set(rect.anchoredPosition.x, 50f);
        }
        else
        {
            rect.anchoredPosition3D.Set(rect.anchoredPosition3D.x, 0f, rect.anchoredPosition3D.z);
        }
    }
	
	// Update is called once per frame
	void Update () {

    }
}
