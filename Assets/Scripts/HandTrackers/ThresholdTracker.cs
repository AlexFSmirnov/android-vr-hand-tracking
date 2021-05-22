using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;

public class ThresholdTracker : HandTracker
{
    private bool isInitialized = false;
    private Camera targetCamera;

    private Mat rgbMat;
    private Mat hsvMat;

    private Mat handMask;

    private Scalar lowerThresholdColor = null;
    private Scalar upperThresholdColor = null;

    public void Initialize(int frameWidth, int frameHeight, Camera camera)
    {
        targetCamera = camera;
        isInitialized = true;

        rgbMat = new Mat();
        hsvMat = new Mat();

        handMask = new Mat();
    }

    public bool IsInitialized()
    {
        return isInitialized;
    }

    public void Dispose()
    {
        if (rgbMat != null)
        {
            rgbMat.Dispose();
            rgbMat = null;
        }

        if (hsvMat != null)
        {
            hsvMat.Dispose();
            hsvMat = null;
        }

        if (handMask != null)
        {
            handMask.Dispose();
            handMask = null;
        }
    }

    public void SetThresholdColors(Scalar lower, Scalar upper)
    {
        lowerThresholdColor = lower;
        upperThresholdColor = upper;
    }

    public void GetHandPositions(Mat rgbaMat, out List<HandTransform> hands, bool drawPreview = false)
    {
        hands = new List<HandTransform>();

        if (lowerThresholdColor == null || upperThresholdColor == null)
            return;

        Imgproc.cvtColor(rgbaMat, rgbMat, Imgproc.COLOR_RGBA2RGB);
        Imgproc.cvtColor(rgbMat, hsvMat, Imgproc.COLOR_RGB2HSV);

        Imgproc.blur(hsvMat, hsvMat, new Size(2, 2));

        // Core.inRange(hsvMat, new Scalar(0, 48, 80), new Scalar(20, 255, 255), handMask);
        Core.inRange(hsvMat, lowerThresholdColor, upperThresholdColor, handMask);

        Imgproc.dilate(handMask, handMask, new Mat());

        if (drawPreview)
        {
            Imgproc.cvtColor(handMask, rgbMat, Imgproc.COLOR_GRAY2RGB);
            // Imgproc.cvtColor(hsvMat, rgbMat, Imgproc.COLOR_HSV2RGB);
            Imgproc.cvtColor(rgbMat, rgbaMat, Imgproc.COLOR_RGB2RGBA);
            // Imgproc.cvtColor(rgbMat, rgbaMat, Imgproc.COLOR_RGB2GRAY);
        }
    }
}
