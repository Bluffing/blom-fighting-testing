using UnityEngine;
using UnityEngine.UI;

public class FPS : MonoBehaviour
{
    private Text text;
    public bool showFPS = true;
    public bool showFPSWarning = true;

    public void Start()
    {
        if (!TryGetComponent(out text))
            Debug.LogError("Text component not found");
    }

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
