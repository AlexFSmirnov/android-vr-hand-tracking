using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ArucoModule;
using OpenCVForUnity.Calib3dModule;

public class ArUcoTracker : HandTracker
{
    private int dictionaryId;

    private Mat rgbMat;

    private List<Mat> corners;
    private Mat ids;
    private DetectorParameters detectorParameters;
    private Dictionary markerDictionary;

    public ArUcoTracker(int dictId = Aruco.DICT_4X4_50)
    {
        dictionaryId = dictId;
    }

    public void Initialize()
    {
        rgbMat = new Mat();
        corners = new List<Mat>();
        ids = new Mat();
        detectorParameters = DetectorParameters.create();
        markerDictionary = Aruco.getPredefinedDictionary(dictionaryId);
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
    public void GetHandPositions(Mat rgbaMat, out List<HandInfo> hands, bool drawPreview = false)
    {
        Imgproc.cvtColor(rgbaMat, rgbMat, Imgproc.COLOR_RGBA2RGB);

        int max_d = (int)Mathf.Max (rgbMat.width(), rgbMat.height());
        float fx = max_d;
        float fy = max_d;
        float cx = rgbaMat.width() / 2.0f;
        float cy = rgbaMat.height() / 2.0f;

        var camMatrix = new Mat (3, 3, CvType.CV_64FC1);
        camMatrix.put (0, 0, fx);
        camMatrix.put (0, 1, 0);
        camMatrix.put (0, 2, cx);
        camMatrix.put (1, 0, 0);
        camMatrix.put (1, 1, fy);
        camMatrix.put (1, 2, cy);
        camMatrix.put (2, 0, 0);
        camMatrix.put (2, 1, 0);
        camMatrix.put (2, 2, 1.0f);

        var distCoeffs = new MatOfDouble (0, 0, 0, 0);

        Aruco.detectMarkers(rgbMat, markerDictionary, corners, ids, detectorParameters, new List<Mat>(), camMatrix, distCoeffs);

        var rvecs = new Mat();
        var tvecs = new Mat();
        Aruco.estimatePoseSingleMarkers(corners, 0.05f, camMatrix, distCoeffs, rvecs, tvecs);

        for (int i = 0; i < ids.total(); i++) {
            using (Mat rvec = new Mat (rvecs, new OpenCVForUnity.CoreModule.Rect (0, i, 1, 1)))
            using (Mat tvec = new Mat (tvecs, new OpenCVForUnity.CoreModule.Rect (0, i, 1, 1))) {
                // In this example we are processing with RGB color image, so Axis-color correspondences are X: blue, Y: green, Z: red. (Usually X: red, Y: green, Z: blue)
                Calib3d.drawFrameAxes(rgbMat, camMatrix, distCoeffs, rvec, tvec, 0.05f * 0.5f);
            }
        }

        if (drawPreview)
        {
            Aruco.drawDetectedMarkers(rgbMat, corners, ids, new Scalar(0, 255, 0, 255));
            Imgproc.cvtColor(rgbMat, rgbaMat, Imgproc.COLOR_RGB2RGBA);
        }

        hands = new List<HandInfo>();
    }
}
