using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ArucoModule;

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

    public void GetHandPositions(Mat rgbaMat, out List<HandInfo> hands, bool drawPreview = false)
    {
        Imgproc.cvtColor(rgbaMat, rgbMat, Imgproc.COLOR_RGBA2RGB);

        Aruco.detectMarkers(rgbMat, markerDictionary, corners, ids, detectorParameters);

        if (drawPreview)
        {
            Aruco.drawDetectedMarkers(rgbMat, corners, ids, new Scalar(0, 255, 0, 255));
            Imgproc.cvtColor(rgbMat, rgbaMat, Imgproc.COLOR_RGB2RGBA);
        }

        hands = new List<HandInfo>();
    }
}
