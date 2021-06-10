using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UISaintAugmentedScreen : MonoBehaviour
{
    [Header("Object Settings")]
    [Tooltip("UI element which contains the render image")]
    public RawImage render;
    [Tooltip("GameObject which has to be moved so that the screen get transparent")]
    public GameObject triggerObject;

    [Header("Animation Settings")]
    public float maxOccupied = 1.0f;
    public float speedTransition = 0.1f;
    public float sensitivity = 0.1f;


    private float alpha;

    private bool enableTransparat = false;

    private Vector3 oldPosition;
    private Quaternion oldRotation;

    public bool EnableTransparat
    {
        get
        {
            return enableTransparat;
        }

        set
        {
            enableTransparat = value;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        //renderCanvas = render.GetComponent<CanvasGroup>();
        alpha = 0.0f; //1 is opaque, 0 is transparent
    }

    // Update is called once per frame
    void Update()
    {
        float distance = Vector3.Distance(triggerObject.transform.position, oldPosition);
        float angle = Quaternion.Angle(triggerObject.transform.rotation, oldRotation);
        
        if (100*distance > sensitivity || angle > sensitivity) 
            this.EnableTransparat = true;
        else
            this.EnableTransparat = false;

        oldPosition = triggerObject.transform.position;
        oldRotation = triggerObject.transform.rotation;



        if (this.EnableTransparat)
        {
            if (alpha < maxOccupied)
                alpha += speedTransition;
        }
        else
        {
            if (alpha >= 0)
                alpha -= speedTransition;
        }

        Color currColor = render.color;
        currColor.a = alpha;
        render.color = currColor;
    }
}
