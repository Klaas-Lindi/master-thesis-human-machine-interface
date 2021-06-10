using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UiMapScale : MonoBehaviour {

    [Header("Scaling Settings")]
    [Tooltip("Set the generale scale of all visible UI-Elements in Map")]
    public float scale = 10;

    [Header("Scaling Objects")]
    public Transform currentUAV;
    public Transform predictedUAV;
    public GameObject predictedPath;

    public Transform UiAnchor;
    public Transform UiOperatorPosition;
    public GameObject UiOperatorOrientation;

    public Transform UiCursorPosition;
    public GameObject UiVirtualPath;
    public Transform UiAutomatismText;
    public Transform UiWaypoint;

    private Camera mapCamera;

    // Use this for initialization
    void Start () {
        mapCamera = this.GetComponent<Camera>();
        UiOperatorOrientation.GetComponent<UIOperatorRotation>().distance = 5;
    }
	
	// Update is called once per frame
	void Update () {
        float factor = mapCamera.orthographicSize/50 * scale;

        // Set Scales
        currentUAV.localScale = this.GetScale(factor, 0.001f);
        predictedUAV.localScale = this.GetScale(factor, 0.001f);

        UiAnchor.localScale = this.GetScale(factor, 0.1f);
        UiOperatorPosition.localScale = this.GetScale(factor, 0.2f);
        UiOperatorOrientation.GetComponent<Transform>().localScale = this.GetScale(factor, 0.5f);
        UiCursorPosition.localScale = this.GetScale(factor, 0.1f);
        UiVirtualPath.GetComponent<Transform>().localScale = this.GetScale(factor, 0.5f);
        UiAutomatismText.localScale = this.GetScale(factor, 0.02f);
        UiWaypoint.localScale = this.GetScale(factor, 0.1f);

        // Set Distance
        UiOperatorOrientation.GetComponent<UIOperatorRotation>().distance = 5 * mapCamera.orthographicSize / 50;

        // Set Lines
        UiVirtualPath.GetComponent<LineRenderer>().widthMultiplier = 0.5f * mapCamera.orthographicSize / 50;
        predictedPath.GetComponent<LineRenderer>().widthMultiplier = 0.5f * mapCamera.orthographicSize / 50;
    }

    Vector3 GetScale(float factor, float correction)
    {
        return new Vector3(factor * correction, factor * correction, factor * correction);
    }
}
