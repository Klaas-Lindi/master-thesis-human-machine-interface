using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UISaintHiddenWindowVisibilityToggle : MonoBehaviour
{
    private bool wasWindowVisibility = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }  
    
    public void toogleWindowVisibility()
    {
        wasWindowVisibility = !wasWindowVisibility;
    }


    public void setVisibility(bool isVisible)
    {
        if (wasWindowVisibility && isVisible)
            this.gameObject.SetActive(true);
        else
            this.gameObject.SetActive(false);
    }

}
