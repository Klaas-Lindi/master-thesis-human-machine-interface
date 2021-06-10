using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UISaintWindowHandler : MonoBehaviour
{
    public bool defaultActive = false;
    private bool isVisble;

    // Start is called before the first frame update
    void Start()
    {
        this.isVisble = defaultActive;
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void setVisibility(bool isVisible)
    {
        if (this.isVisble && isVisible)
            this.gameObject.SetActive(true);
        else
            this.gameObject.SetActive(false);
    }

    public void toogleWindow()
    {
        this.isVisble = !this.isVisble;
        this.gameObject.SetActive(this.isVisble);
    }
}
