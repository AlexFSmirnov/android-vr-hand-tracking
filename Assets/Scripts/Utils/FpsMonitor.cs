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
        text = gameObject.transform.Find("PreviewCanvas/FpsText").GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        ++framesSinceLastSecond;
        ++framesSinceStart;

        if (Time.time - lastSecondTimestamp > 1)
        {
            fps = framesSinceLastSecond / (Time.time - lastSecondTimestamp);
            lastSecondTimestamp = Time.time;
            framesSinceLastSecond = 0;
        }

        if (Time.time - startTimestamp > 60 && firstMinuteFps < 0)
        {
            firstMinuteFps = framesSinceStart / (Time.time - startTimestamp);
        }

        UpdateFpsText();
    }

    public void Run()
    {
        isEnabled = true;
        text.color = new Color(255, 255, 255, 255);

        lastSecondTimestamp = Time.time;
        startTimestamp = Time.time;
    }

    private void UpdateFpsText()
    {
        string fpsString = fps > 0 ? fps.ToString("0.00") : "-";
        string firstMinuteFpsString = firstMinuteFps > 0 ? firstMinuteFps.ToString("0.00") : "-";

        text.text = $"{fpsString} FPS (1m avg: {firstMinuteFpsString})";
    }
}
