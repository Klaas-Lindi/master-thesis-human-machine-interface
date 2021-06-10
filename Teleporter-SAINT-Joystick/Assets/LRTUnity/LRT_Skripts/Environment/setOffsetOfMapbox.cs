using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class setOffsetOfMapbox : MonoBehaviour
{
    // Start is called before the first frame update
    public float setOffsetMapbox= -477f;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        this.transform.position = new Vector3(0f, setOffsetMapbox, 0f);
    }
}
