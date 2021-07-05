using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.VideoModule;

public class CamshiftTracker : HandTracker
{
    private bool isInitialized = false;
    private Camera targetCamera;
    private int frameWidth;
    private int frameHeight;

    private bool useLab = false;
    private Mat rgbMat;
    private Mat hsvOrLabMat;

    private Mat handMask;
    private Mat camshiftDst;

    private Scalar lowerThresholdColor = null;
    private Scalar upperThresholdColor = null;

    private float handZDistance = 0.5f;
    private int maxContours = 2;
    private float minContourArea = 100f;
    private float minContourAreaFraction = 0.2f;

    private bool roiNeedsRecalculating = true;
    private OpenCVForUnity.CoreModule.Rect initRoiRect;
    private MatOfFloat range = new MatOfFloat(0, 256);
    private Mat roiHist = new Mat();
    private MatOfInt histSize = new MatOfInt(180);
    private MatOfInt channels = new MatOfInt(0);

    // Camshift termination criteria - either after 10 iterations, or once mobed by at least 1 pt.
    private TermCriteria camshiftTermCriteria = new TermCriteria(TermCriteria.EPS | TermCriteria.COUNT, 10, 1);

    public CamshiftTracker(bool useLab = false)
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
        camshiftDst = new Mat();
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

        if (camshiftDst != null)
        {
            camshiftDst.Dispose();
            camshiftDst = null;
        }
    }

    public void SetThresholdColors(Scalar lower, Scalar upper)
    {
        lowerThresholdColor = lower;
        upperThresholdColor = upper;

        roiNeedsRecalculating = true;
    }

    public void GetHandPositions(Mat rgbaMat, out List<HandTransform> hands, bool drawPreview = false)
    {
        hands = new List<HandTransform>();

        if (lowerThresholdColor == null || upperThresholdColor == null)
            return;

        // If a new threshold range was selected, update the ROI.
        if (roiNeedsRecalculating)
        {
            UpdateRoi(rgbaMat, drawPreview);
            roiNeedsRecalculating = false;
            return;
        }

        // Apply Camshift to find the hand bounding box in the current frame.
        var rect = CamshiftStep(rgbaMat);

        var screenCenterPoint = ScreenUtils.GetScreenPointFromFramePoint(rect.center, frameWidth, frameHeight);
        var handWorldPosition = targetCamera.ScreenToWorldPoint(new Vector3(screenCenterPoint.x, screenCenterPoint.y, handZDistance));
        hands.Add(new HandTransform(handWorldPosition));

        if (drawPreview)
            DrawRotatedRect(rect, rgbaMat);
    }

    private void UpdateRoi(Mat rgbaMat, bool drawPreview)
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

        // Find a contour with max area.
        double maxArea = 0;
        MatOfPoint2f maxContour = new MatOfPoint2f();
        foreach (var contour in contours)
        {
            double area = Imgproc.contourArea(contour);
            if (area > maxArea)
            {
                maxArea = area;
                contour.convertTo(maxContour, CvType.CV_32F);
            }
        }

        // Find a bounding rect for the contour. It will serve as the initial
        // Region of Interest for the Camshift algorithm.
        initRoiRect = Imgproc.boundingRect(maxContour);

        // Calculate the HSV histogram of the detected ROI.
        Mat roi = new Mat(hsvOrLabMat, initRoiRect);
        Mat roiMask = new Mat(handMask, initRoiRect);
        Imgproc.calcHist(new List<Mat> { roi }, channels, roiMask, roiHist, histSize, range);
        Core.normalize(roiHist, roiHist, 0, 255, Core.NORM_MINMAX);

        if (drawPreview)
        {
            DrawRect(initRoiRect, rgbaMat);
        }
    }

    private RotatedRect CamshiftStep(Mat rgbaMat)
    {
        Imgproc.cvtColor(rgbaMat, rgbMat, Imgproc.COLOR_RGBA2RGB);
        if (useLab)
            Imgproc.cvtColor(rgbMat, hsvOrLabMat, Imgproc.COLOR_RGB2Lab);
        else
            Imgproc.cvtColor(rgbMat, hsvOrLabMat, Imgproc.COLOR_RGB2HSV);
        
        // Calculate the back projection of the current frame against the ROI histogram.
        Imgproc.calcBackProject(new List<Mat> { hsvOrLabMat }, channels, roiHist, camshiftDst, range, 1);

        // Using the back projection, apply Camshift to find the hand position in the current frame.
        RotatedRect rect = Video.CamShift(camshiftDst, initRoiRect, camshiftTermCriteria);

        return rect;
    }

    private void DrawRect(OpenCVForUnity.CoreModule.Rect rect, Mat frameRect)
    {
        var p1 = new Point(rect.x, rect.y);
        var p2 = new Point(rect.x + rect.width, rect.y + rect.height);

        Imgproc.rectangle(frameRect, p1, p2, new Scalar(255, 0, 0, 255), 2);
    }

    private void DrawRotatedRect(RotatedRect rect, Mat frame)
    {
        Point[] points = new Point[4];
        rect.points(points);
        for (int i = 0; i < 4; ++i)
            Imgproc.line(frame, points[i], points[(i + 1) % 4], new Scalar(255, 0, 0, 255), 2);
    }
}
