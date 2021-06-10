using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIVRTransparencyHandler : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Distance where the transparancy begins")]
    public float thresholdDistance;

    [Header("Objects")]
    [Tooltip("The hmd object")]
    public GameObject head;

    [Tooltip("The operator Positionin object")]
    public GameObject operatorPosition;

    
    private Color albedoColor;
    private Color hdrColor;

    // Start is called before the first frame update
    void Start()
    {
        albedoColor = operatorPosition.GetComponent<Renderer>().material.color;
        operatorPosition.GetComponent<Renderer>().material.EnableKeyword("_EMISSION");
        hdrColor = operatorPosition.GetComponent<Renderer>().material.GetColor("_EmissionColor");
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 headPosition = head.transform.position;
        float quote = 1.0f;
        float distance = Vector3.Distance(headPosition, operatorPosition.transform.position);

        if (distance < thresholdDistance)
        {
            quote = (distance / thresholdDistance);
        }
               
        Color transparent = new Color(
            albedoColor.r,
            albedoColor.g,
            albedoColor.b,
            quote);

        operatorPosition.GetComponent<Renderer>().material.color = transparent;
        operatorPosition.GetComponent<Renderer>().material.SetColor("_EmissionColor", hdrColor * quote);
    }
}
