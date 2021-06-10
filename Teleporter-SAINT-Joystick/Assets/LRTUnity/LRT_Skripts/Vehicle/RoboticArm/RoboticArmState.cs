using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoboticArmState : MonoBehaviour
{
    private float[] jointPosition;

    public float[] JointPosition { get => jointPosition; set => jointPosition = value; }


    // Start is called before the first frame update
    void Start()
    {
        this.JointPosition = new float[9];
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
