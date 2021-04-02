using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.IO;
using System.Linq;
using OpenCVForUnity.ArucoModule;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.Calib3dModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.ImgcodecsModule;
using OpenCVForUnity.UnityUtils.Helper;

public class ArUcoCalibrationManager : MonoBehaviour
{
    private CameraMatProvider cameraMatProvider;
    private Mat rgbaFrameMat;
    private Texture2D cameraPreviewTexture;

    private GameObject canvas;
    private RawImage cameraPreviewImage;
    private Button shutterButton;

    private Mat camMatrix = null;
    private Mat distCoeffs = null;
    
    private Mat rgbFrameMat;
    private List<Mat> allRgbFrames;
    private List<List<Mat>> allCorners;
    private List<Mat> allIds;
    private List<List<Mat>> allRejectedCorners;
    private Dictionary markerDictionary;
    private DetectorParameters detectorParameters;

    void Start()
    {
        cameraMatProvider = GameObject.Find("CameraMatProvider").GetComponent<CameraMatProvider>();

        canvas = gameObject.transform.Find("Canvas").gameObject;
        cameraPreviewImage = canvas.transform.Find("CameraPreviewImage").GetComponent<RawImage>();
        shutterButton = canvas.transform.Find("ShutterButton").GetComponent<Button>();

        shutterButton.interactable = false;

        distCoeffs = new MatOfDouble(0, 0, 0, 0, 0);

        rgbFrameMat = new Mat();
        allCorners = new List<List<Mat>>();
        allIds = new List<Mat>();
        allRejectedCorners = new List<List<Mat>>();

        markerDictionary = Aruco.getPredefinedDictionary(Aruco.DICT_6X6_50);
        detectorParameters = DetectorParameters.create();
        detectorParameters.set_cornerRefinementMethod(1);  // do cornerSubPix() of OpenCV
    }

    void OnDestroy()
    {
        if (cameraPreviewTexture != null)
        {
            Texture2D.Destroy(cameraPreviewTexture);
            cameraPreviewTexture = null;
        }

        if (rgbaFrameMat != null)
        {
            rgbaFrameMat.Dispose();
            rgbaFrameMat = null;
        }
    }

    void Update()
    {
        // Get an OpenCV matrix from the current camera frame.
        rgbaFrameMat = cameraMatProvider.GetMat();
        if (rgbaFrameMat == null)
            return;

        if (camMatrix == null)
            camMatrix = CreateCameraMatrix(rgbaFrameMat.width(), rgbaFrameMat.height());

        shutterButton.interactable = true;

        // Update (creating new if needed) the preview texture with the changed frame matrix and display it on the preview image.
        if (cameraPreviewTexture == null || rgbaFrameMat.width() != cameraPreviewTexture.width || rgbaFrameMat.height() != cameraPreviewTexture.height)
            cameraPreviewTexture = new Texture2D(rgbaFrameMat.width(), rgbaFrameMat.height(), TextureFormat.RGBA32, false);

        Utils.fastMatToTexture2D(rgbaFrameMat, cameraPreviewTexture);
        cameraPreviewImage.texture = cameraPreviewTexture;
    }

    public void OnShutterButtonClick()
    {
        Imgproc.cvtColor(rgbaFrameMat, rgbFrameMat, Imgproc.COLOR_RGBA2RGB);

        List<Mat> corners = new List<Mat>();
        Mat ids = new Mat();
        List<Mat> rejectedCorners = new List<Mat>();

        Aruco.detectMarkers(rgbFrameMat, markerDictionary, corners, ids, detectorParameters, rejectedCorners, camMatrix, distCoeffs);

        if (ids.total() > 0)
        {
            allRgbFrames.Add(rgbFrameMat);
            allCorners.Add(corners);
            allIds.Add(ids);
        }
        else
        {
            Debug.Log("No frames visible");
        }
    }

    private void CalibrateCamera()
    {
        // TODO: finish stuff
    }

    private Mat CreateCameraMatrix(float width, float height)
    {
        int max_d = (int)Mathf.Max(width, height);
        double fx = max_d;
        double fy = max_d;
        double cx = width / 2.0f;
        double cy = height / 2.0f;

        Mat camMatrix = new Mat(3, 3, CvType.CV_64FC1);
        camMatrix.put(0, 0, fx);
        camMatrix.put(0, 1, 0);
        camMatrix.put(0, 2, cx);
        camMatrix.put(1, 0, 0);
        camMatrix.put(1, 1, fy);
        camMatrix.put(1, 2, cy);
        camMatrix.put(2, 0, 0);
        camMatrix.put(2, 1, 0);
        camMatrix.put(2, 2, 1.0f);

        return camMatrix;
    }
}
