using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIWarningScrollText : MonoBehaviour {

    private ScrollRect myScrollRect;
    public float speed;
    public float refreshdelay;
    [Tooltip("Wait after marquee finished")]
    public float endTimeDelay = 0.5f;
    [Tooltip("Wait before begin of marquee")]
    public float beginTimeDelay = 0.5f;

    [Tooltip("Reference to the textfield")]
    public Text warningText;
    [Tooltip("Maximum number of characters to display static message instead of marquee")]
    public int maxStaticCharacters;

    private float timeStampEnd = 0.0f;
    private float timeStampBegin = 0.0f;
    // Use this for initialization
    void Start()
    {
        this.myScrollRect = this.GetComponent<ScrollRect>();
        myScrollRect.horizontalNormalizedPosition = 0.0f;
    }

    void Update()
    {
        if (warningText.text.Length > maxStaticCharacters)
        {
            if (myScrollRect.horizontalNormalizedPosition < 1)
            {
                if (beginTimeDelay < (Time.realtimeSinceStartup - timeStampBegin))
                {
                    myScrollRect.horizontalNormalizedPosition = myScrollRect.horizontalNormalizedPosition + speed;
                    timeStampEnd = Time.realtimeSinceStartup;
                }
            }
            if (myScrollRect.horizontalNormalizedPosition >= 1)
            {
                if (endTimeDelay < (Time.realtimeSinceStartup - timeStampEnd))
                {
                    StartCoroutine(refresh());
                    timeStampBegin = Time.realtimeSinceStartup;
                }
            }
        }
        else
        {
            myScrollRect.horizontalNormalizedPosition = 0.5f;
        }
    }
    IEnumerator refresh()
    {
        yield return new WaitForSeconds(refreshdelay);
        myScrollRect.horizontalNormalizedPosition = 0.0f;
        StopAllCoroutines();
    }


}
