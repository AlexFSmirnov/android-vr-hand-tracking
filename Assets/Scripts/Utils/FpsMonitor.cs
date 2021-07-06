using UnityEngine;
using TMPro;

public class FpsMonitor : MonoBehaviour
{
    private TextMeshProUGUI text;
    private bool isEnabled = false;

    private int framesSinceLastSecond = 0;
    private float lastSecondTimestamp = -100;
    private int framesSinceStart = 0;
    private float startTimestamp = -100;

    private float fps = -1;
    private float firstMinuteFps = -1;

    void Start()
    {
        text = gameObject.transform.Find("FpsText").GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        if (!isEnabled)
        {
            Stop();
            return;
        }

        ++framesSinceLastSecond;
        ++framesSinceStart;

        if (Time.unscaledTime - lastSecondTimestamp > 1)
        {
            fps = framesSinceLastSecond / (Time.unscaledTime - lastSecondTimestamp);
            lastSecondTimestamp = Time.unscaledTime;
            framesSinceLastSecond = 0;
        }

        if (Time.unscaledTime - startTimestamp > 60 && firstMinuteFps < 0)
        {
            firstMinuteFps = framesSinceStart / (Time.unscaledTime - startTimestamp);
        }

        UpdateFpsText();
    }

    public void Run()
    {
        isEnabled = true;
        text.color = new Color(255, 255, 255, 255);

        lastSecondTimestamp = Time.unscaledTime;
        startTimestamp = Time.unscaledTime;
    }

    public void Stop()
    {
        isEnabled = false;
        text.color = new Color(0, 0, 0, 0);
        framesSinceLastSecond = 0;
        framesSinceStart = 0;
        fps = -1;
        firstMinuteFps = -1;
    }

    private void UpdateFpsText()
    {
        string fpsString = fps > 0 ? fps.ToString("0.00") : "-";
        string firstMinuteFpsString = firstMinuteFps > 0 ? firstMinuteFps.ToString("0.00") : "-";

        text.text = $"{fpsString} FPS (1m avg: {firstMinuteFpsString})";
    }
}
