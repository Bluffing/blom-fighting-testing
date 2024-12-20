using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FPS : MonoBehaviour
{
    public Text text;
    public bool showFPS = true;
    public bool showFPSWarning = true;

    // Update is called once per frame
    void Update()
    {
        float fps = 1 / Time.deltaTime;
        if (showFPS)
            text.text = fps.ToString();
        if (showFPSWarning && fps < 100)
            Debug.Log($"fps dropped : {fps}");
    }
}
