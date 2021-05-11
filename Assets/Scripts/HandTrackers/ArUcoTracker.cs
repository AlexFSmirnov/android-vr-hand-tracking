using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ArucoModule;
using OpenCVForUnity.Calib3dModule;
using OpenCVForUnity.UnityUtils;

public class ArUcoTracker : HandTracker
{
    private bool isInitialized = false;
    private Camera targetCamera;

    private int dictionaryId;

    private Mat rgbMat;

    private List<Mat> corners;
    private Mat ids;
    private DetectorParameters detectorParameters;
    private Dictionary markerDictionary;

    private Mat camMatrix;
    private Mat distCoeffs;

    public ArUcoTracker(int dictId = Aruco.DICT_4X4_50)
    {
        dictionaryId = dictId;
    }

    public void Initialize(int frameWidth, int frameHeight, Camera camera)
    {
        targetCamera = camera;

        rgbMat = new Mat();
        corners = new List<Mat>();
        ids = new Mat();
        detectorParameters = DetectorParameters.create();
        markerDictionary = Aruco.getPredefinedDictionary(dictionaryId);

        distCoeffs = new MatOfDouble(0, 0, 0, 0);
        InitializeCameraMatrix(frameWidth, frameHeight);

        isInitialized = true;
    }

    public bool IsInitialized()
    {
        return isInitialized;
    }

    public void InitializeCameraMatrix(int frameWidth, int frameHeight)
    {
        int max_d = (int)Mathf.Max(frameWidth, frameHeight);
        double fx = max_d;
        double fy = max_d;
        double cx = frameWidth / 2.0f;
        double cy = frameHeight / 2.0f;

        camMatrix = new Mat(3, 3, CvType.CV_64FC1);
        camMatrix.put(0, 0, fx);
        camMatrix.put(0, 1, 0);
        camMatrix.put(0, 2, cx);
        camMatrix.put(1, 0, 0);
        camMatrix.put(1, 1, fy);
        camMatrix.put(1, 2, cy);
        camMatrix.put(2, 0, 0);
        camMatrix.put(2, 1, 0);
        camMatrix.put(2, 2, 1.0f);
    }

    public void Dispose()
    {
        if (rgbMat != null)
        {
            rgbMat.Dispose();
            rgbMat = null;
        }

        if (corners != null)
        {
            for (int i = 0; i < corners.Count; ++i)
            {
                if (corners[i] != null)
                {
                    corners[i].Dispose();
                    corners[i] = null;
                }
            }
            corners = null;
        }

        if (ids != null)
        {
            ids.Dispose();
            ids = null;
        }
    }

    // TODO: GetHandPositions, if no data is available, should interpolate using several previous frames. Extract to HandTracker?
    public void GetHandPositions(Mat rgbaMat, out List<HandTransform> hands, bool drawPreview = false)
    {
        hands = new List<HandTransform>();

        Imgproc.cvtColor(rgbaMat, rgbMat, Imgproc.COLOR_RGBA2RGB);

        Aruco.detectMarkers(rgbMat, markerDictionary, corners, ids, detectorParameters, new List<Mat>(), camMatrix, distCoeffs);

        var rvecs = new Mat();
        var tvecs = new Mat();
        Aruco.estimatePoseSingleMarkers(corners, 0.03f, camMatrix, distCoeffs, rvecs, tvecs);

        for (int i = 0; i < ids.total(); i++)
        {
            using (Mat rvec = new Mat(rvecs, new OpenCVForUnity.CoreModule.Rect(0, i, 1, 1)))
            using (Mat tvec = new Mat(tvecs, new OpenCVForUnity.CoreModule.Rect(0, i, 1, 1)))
            {
                var markerFrameCenterPoint = GetMarkerFrameCenterPoint(corners[i]);
                hands.Add(GetHandTransformFromARUcoVecs(rvec, tvec, markerFrameCenterPoint, rgbMat.width(), rgbMat.height()));

                if (drawPreview)
                {
                    Imgproc.circle(rgbMat, markerFrameCenterPoint, 5, new Scalar(255, 0, 0), 3);
                    Calib3d.drawFrameAxes(rgbMat, camMatrix, distCoeffs, rvec, tvec, 0.05f * 0.5f);
                }
            }
        }

        if (drawPreview)
        {
            Aruco.drawDetectedMarkers(rgbMat, corners, ids, new Scalar(0, 255, 0, 255));
            Imgproc.cvtColor(rgbMat, rgbaMat, Imgproc.COLOR_RGB2RGBA);
        }
    }

    private HandTransform GetHandTransformFromARUcoVecs(Mat rvec, Mat tvec, Point markerFrameCenterPoint, float frameWidth, float frameHeight)
    {
        double[] tvecArr = new double[3];
        tvec.get(0, 0, tvecArr);
        float markerZ = (float)tvecArr[2];

        var markerScreenCenterPoint = ScreenUtils.GetScreenPointFromFramePoint(markerFrameCenterPoint, frameWidth, frameHeight);

        var markerWorldPosition = targetCamera.ScreenToWorldPoint(new Vector3(markerScreenCenterPoint.x, markerScreenCenterPoint.y, markerZ));
        var markerWorldRotation = GetHandRotationFromARUcoRvec(rvec);

        return new HandTransform(
            markerWorldPosition,
            markerWorldRotation
        );
    }

    private Vector3 GetHandRotationFromARUcoRvec(Mat rvec)
    {
        double[] rvecArr = new double[3];
        rvec.get(0, 0, rvecArr);

        var rot = ARUtils.ConvertRvecToRot(rvecArr);

        var markerLocalRotation = new Vector3(
            -rot.eulerAngles.x,
            rot.eulerAngles.y,
            -rot.eulerAngles.z
        );

        return markerLocalRotation + targetCamera.transform.localEulerAngles;
    }

    private Point GetMarkerFrameCenterPoint(Mat corners)
    {
        float[] topLeftArr = new float[2];
        float[] bottomRightArr = new float[2];

        corners.get(0, 0, topLeftArr);
        corners.get(0, 2, bottomRightArr);

        var topLeft = new Point(topLeftArr[0], topLeftArr[1]);
        var bottomRight = new Point(bottomRightArr[0], bottomRightArr[1]);

        return (topLeft + bottomRight) / 2;
    }
}
