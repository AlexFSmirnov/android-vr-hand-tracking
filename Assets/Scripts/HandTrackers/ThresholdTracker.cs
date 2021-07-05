using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;

public class ThresholdTracker : HandTracker
{
    private bool isInitialized = false;
    private Camera targetCamera;
    private int frameWidth;
    private int frameHeight;

    private bool useLab = false;
    private Mat rgbMat;
    private Mat hsvOrLabMat;

    private Mat handMask;

    private Scalar lowerThresholdColor = null;
    private Scalar upperThresholdColor = null;

    private float handZDistance = 0.5f;
    private int maxContours = 2;
    private float minContourArea = 100f;
    private float minContourAreaFraction = 0.2f;

    public ThresholdTracker(bool useLab = false)
    {
        this.useLab = useLab;
    }

    public void Initialize(int frameWidth, int frameHeight, Camera camera)
    {
        targetCamera = camera;
        isInitialized = true;
        this.frameWidth = frameWidth;
        this.frameHeight = frameHeight;

        rgbMat = new Mat();
        hsvOrLabMat = new Mat();

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

        if (hsvOrLabMat != null)
        {
            hsvOrLabMat.Dispose();
            hsvOrLabMat = null;
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

        var contours = GetContoursFromImage(rgbaMat);

        foreach (var contour in contours)
        {
            var centerPoint = GetCenterOfContour(contour);
            var screenCenterPoint = ScreenUtils.GetScreenPointFromFramePoint(centerPoint, frameWidth, frameHeight);

            var handWorldPosition = targetCamera.ScreenToWorldPoint(new Vector3(screenCenterPoint.x, screenCenterPoint.y, handZDistance));

            hands.Add(new HandTransform(handWorldPosition));

            if (drawPreview)
            {
                Imgproc.circle(rgbaMat, centerPoint, 5, new Scalar(0, 255, 0, 255), 2);
            }
        }

        if (drawPreview)
        {
            Imgproc.drawContours(rgbaMat, contours, -1, new Scalar(255, 0, 0, 255), 2);
        }
    }

    private List<MatOfPoint> GetContoursFromImage(Mat rgbaMat)
    {
        Imgproc.cvtColor(rgbaMat, rgbMat, Imgproc.COLOR_RGBA2RGB);

        if (useLab)
            Imgproc.cvtColor(rgbMat, hsvOrLabMat, Imgproc.COLOR_RGB2Lab);
        else
            Imgproc.cvtColor(rgbMat, hsvOrLabMat, Imgproc.COLOR_RGB2HSV);

        // Blur the image for better thesholding.
        Imgproc.blur(hsvOrLabMat, hsvOrLabMat, new Size(3, 3));

        // Mask all colors between the provided HSV range.
        Core.inRange(hsvOrLabMat, lowerThresholdColor, upperThresholdColor, handMask);

        // Dilate the mask for the same purposes as bluring.
        Imgproc.dilate(handMask, handMask, new Mat());

        // Find hand contours in the thresholded mask.
        var contours = new List<MatOfPoint>();
        Imgproc.findContours(handMask, contours, new Mat(), Imgproc.RETR_EXTERNAL, Imgproc.CHAIN_APPROX_SIMPLE);

        // Find max contour area for filtering.
        double maxArea = 0;
        foreach (var contour in contours)
        {
            double area = Imgproc.contourArea(contour);
            if (area > maxArea)
            {
                maxArea = area;
            }
        }

        // Filter contours that are too small.
        var filteredContours = new List<MatOfPoint>();
        foreach (var contour in contours)
        {
            var area = Imgproc.contourArea(contour);

            if (area >= minContourAreaFraction * maxArea && area >= minContourArea)
            {
                filteredContours.Add(contour);
            }
        }

        // Sort contours in decending order.
        filteredContours.Sort(CompareContours);

        // Store the <maxContours> biggest contours.
        var handContours = new List<MatOfPoint>();
        for (int i = 0; i < filteredContours.Count; ++i)
        {
            if (i >= maxContours)
            {
                break;
            }

            handContours.Add(filteredContours[i]);
        }

        return handContours;
    }

    private int CompareContours(MatOfPoint c1, MatOfPoint c2)
    {
        var area1 = Imgproc.contourArea(c1);
        var area2 = Imgproc.contourArea(c2);

        if (area1 == area2)
        {
            return 0;
        }
        else if (area1 > area2)
        {
            return -1;
        }
        else
        {
            return 1;
        }
    }

    private Point GetCenterOfContour(MatOfPoint contour)
    {
        var moments = Imgproc.moments(contour);

        var centerX = moments.get_m10() / moments.get_m00();
        var centerY = moments.get_m01() / moments.get_m00();

        return new Point(centerX, centerY);
    }
}
