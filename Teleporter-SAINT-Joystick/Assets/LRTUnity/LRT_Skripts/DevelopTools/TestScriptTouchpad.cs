using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestScriptTouchpad : MonoBehaviour
{
    public UIVrMenu4Buttons testButton;
    public bool smallsteps;
    private Vector2 position;
 
    // Start is called before the first frame update
    void Start()
    {
        position = new Vector2();
        testButton.setTouchTouched(true);
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.RightArrow))
        {
            if(position.x < 1.0f)
                position += new Vector2(0.1f, 0);
        }
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            if (position.y < 1.0f)
                position += new Vector2(0, 0.1f);
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (position.x > -1.0f)
                position += new Vector2(-0.1f, 0);
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            if (position.y > -1.0f)
                position += new Vector2(0, -0.1f);
        }
        //print(position);
        testButton.setTouchTouched(true);
        testButton.setTouchPosition(position);

    }


}
