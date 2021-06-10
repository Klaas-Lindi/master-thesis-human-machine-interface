
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


public class LRTUnitySettings : MonoBehaviour
{
    // Start is called before the first frame update
    void Awake()
    {
        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, "HMI_VR_ACTIVE");
        //PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, "HMI_SM_ACTIVE");
    }
    
    // Update is called once per frames
    void Update()
    {
        
    }
}
