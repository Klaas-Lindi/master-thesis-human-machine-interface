using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OperatorArmState : MonoBehaviour
{
    private OperatorState parentOperator;

    // pose of the operator in the vr world
    private Pose endEffector = new Pose();

    /// <summary>
    /// Set or get the current operator pose in vr
    /// </summary>
    public Pose EndEffector
    {
        get
        {
            return endEffector;
        }

        set
        {
                endEffector = value;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        EndEffector.position = new Vector3();
        EndEffector.rotation = new Quaternion();

        parentOperator = this.GetComponent<OperatorState>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
