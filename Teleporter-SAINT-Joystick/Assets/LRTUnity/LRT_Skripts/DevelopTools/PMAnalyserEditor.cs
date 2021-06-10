using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;


/// <summary>
/// Inspector changes for PMAnaylse for testing the path prediction, predictive collision detection and automatism handling
/// </summary>
[CustomEditor(typeof(PMAnalyser))]
public class PMAnalyserEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw everything else
        DrawDefaultInspector();

        //Get Script
        PMAnalyser myScript = (PMAnalyser)target;

        // Set GUI Elements
        if (GUILayout.Button("Set Current UAV Position"))
        {
            myScript.SetUavPostition();
        }
        if (GUILayout.Button("Set Operator Automatism"))
        {
            myScript.SetOperatorAutomatism();
        }
    }
}
#endif

