using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ArucoModule;
using OpenCVForUnity.Calib3dModule;
using OpenCVForUnity.UnityUtils;

class HandTransformWithTimestamp
{
    public HandTransform handTransform;
    public float timestamp;
    public bool isTrusted;

    public HandTransformWithTimestamp(HandTransform handTransform, float timestamp, bool isTrusted)
    {
        this.handTransform = handTransform;
        this.timestamp = timestamp;
        this.isTrusted = isTrusted;
    }
}

public class ArUcoTracker : HandTracker
{
    private bool isInitialized = false;
    private Camera targetCamera;
    private Mat rgbMat;

    private int dictionaryId;

    private List<Mat> corners;
    private Mat ids;
    private DetectorParameters detectorParameters;
    private Dictionary markerDictionary;

    private Mat camMatrix;
    private Mat distCoeffs;

    private HashSet<int> idsDetectedThisFrame = new HashSet<int>();

    // A map from marker id to previous detected hand transforms
    private Dictionary<int, Queue<HandTransformWithTimestamp>> previousHandTransforms = new Dictionary<int, Queue<HandTransformWithTimestamp>>();

    // For each hand id contains the current number of untrusted predictions.
    private Dictionary<int, int> untrustedPredictions = new Dictionary<int, int>();
    private int maxUntrustedPredictions = 10;


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

        idsDetectedThisFrame.Clear();

        // Going through all detected markers and adding them to the respective previous position queues.
        for (int i = 0; i < ids.total(); i++)
        {
            int markerId = (int)ids.get(i, 0)[0];
            idsDetectedThisFrame.Add(markerId);

            using (Mat rvec = new Mat(rvecs, new OpenCVForUnity.CoreModule.Rect(0, i, 1, 1)))
            using (Mat tvec = new Mat(tvecs, new OpenCVForUnity.CoreModule.Rect(0, i, 1, 1)))
            {
                var markerFrameCenterPoint = GetMarkerFrameCenterPoint(corners[i]);
                var handTransform = GetHandTransformFromARUcoVecs(rvec, tvec, markerFrameCenterPoint, rgbaMat.width(), rgbMat.height());

                AddNewHandTransform(handTransform, markerId, isTrusted: true);

                if (drawPreview)
                {
                    Imgproc.circle(rgbMat, markerFrameCenterPoint, 5, new Scalar(255, 0, 0), 3);
                    Calib3d.drawFrameAxes(rgbMat, camMatrix, distCoeffs, rvec, tvec, 0.05f * 0.5f);
                }
            }
        }

        // Going through all previously detected ids.
        foreach (var handId in previousHandTransforms.Keys)
        {
            // Trying to predict the position of the non-detected markers based on their previous positions.
            if (!idsDetectedThisFrame.Contains(handId))
                PredictNewPosition(handId);


            // Using the latest detected/predicted position to draw the hand.
            if (previousHandTransforms[handId].Count > 0)
            {
                var handTransform = previousHandTransforms[handId].ToArray()[previousHandTransforms[handId].Count - 1].handTransform;
                hands.Add(handTransform);
            }
        }

        if (drawPreview)
        {
            Aruco.drawDetectedMarkers(rgbMat, corners, ids, new Scalar(0, 255, 0, 255));
            Imgproc.cvtColor(rgbMat, rgbaMat, Imgproc.COLOR_RGB2RGBA);
        }
    }

    private void PredictNewPosition(int handId)
    {
        // If the amount of untrusted predictions is too high, the accuraccy suffers significantly,
        // so we remove the marker completely until we get some trusted information on it again.
        if (untrustedPredictions.ContainsKey(handId) && untrustedPredictions[handId] > maxUntrustedPredictions)
        {
            previousHandTransforms[handId].Clear();
            return;
        }

        HandTransform predictedTransform;
        var previousTransformsArr = previousHandTransforms[handId].ToArray();

        // If not enough data to make a prediction, return the latest transform.
        if (previousTransformsArr.Length < 3)
        {
            predictedTransform = previousTransformsArr[previousTransformsArr.Length - 1].handTransform;
        }
        // Otherwise, calculate the new position based on the velocity and the acceleration of the hand.
        else
        {
            //          delta1            delta2
            //  <pos1> - - - - <pos2> - - - - - - - - <pos3>
            var deltaPosition1 = previousTransformsArr[1].handTransform.position - previousTransformsArr[0].handTransform.position;
            var deltaPosition2 = previousTransformsArr[2].handTransform.position - previousTransformsArr[1].handTransform.position;
            Debug.Log("pos1");
            Debug.Log(previousTransformsArr[2].handTransform.position.ToString("F4"));
            Debug.Log("pos2");
            Debug.Log(previousTransformsArr[1].handTransform.position.ToString("F4"));

            var deltaTime1 = previousTransformsArr[1].timestamp - previousTransformsArr[0].timestamp;
            var deltaTime2 = previousTransformsArr[2].timestamp - previousTransformsArr[1].timestamp;
            var deltaTime3 = Time.time - previousTransformsArr[2].timestamp;
            Debug.Log($"time - {deltaTime1} {deltaTime2} {deltaTime3}");

            var distance1 = deltaPosition1.magnitude;
            var distance2 = deltaPosition2.magnitude;
            Debug.Log($"distance - {distance1} {distance2}");

            var velocity1 = distance1 / deltaTime1;
            var velocity2 = distance2 / deltaTime2;
            Debug.Log($"velocity - {velocity1} {velocity2}");

            var acceleration = velocity1 == 0 ? 1 : velocity2 / velocity1;
            acceleration = 1;

            var velocity3 = velocity2 * acceleration;
            var distance3 = velocity3 * deltaTime3;

            var deltaPosition = deltaPosition2.normalized * distance3;
            var newPosition = previousTransformsArr[2].handTransform.position + deltaPosition;

            predictedTransform = new HandTransform(newPosition);
        }

        AddNewHandTransform(predictedTransform, handId, isTrusted: false);
    }

    private void AddNewHandTransform(HandTransform handTransform, int handId, bool isTrusted)
    {
        var handTransformWithTimestamp = new HandTransformWithTimestamp(handTransform, Time.time, isTrusted);

        // If no previous positions exist, initialize a queue for them.
        if (!previousHandTransforms.ContainsKey(handId))
        {
            previousHandTransforms.Add(handId, new Queue<HandTransformWithTimestamp>());
        }

        // Enqueue the current hand transform.
        if (previousHandTransforms[handId].Count == 0)
            previousHandTransforms[handId].Enqueue(handTransformWithTimestamp);
        else if (previousHandTransforms[handId].Count > 0)
        {
            var prevPosition = previousHandTransforms[handId].ToArray()[previousHandTransforms[handId].Count - 1].handTransform.position;
            var deltaPosition = handTransform.position - prevPosition;
            if (deltaPosition.magnitude > 0.001)
            {
                previousHandTransforms[handId].Enqueue(handTransformWithTimestamp);
            }
        }

        // Always store at most 3 previoius positions.
        if (previousHandTransforms[handId].Count > 3)
        {
            previousHandTransforms[handId].Dequeue();
        }

        // Initialize the untrusted predictions count for the current id.
        if (!untrustedPredictions.ContainsKey(handId))
        {
            untrustedPredictions.Add(handId, 0);
        }

        // If the newest and oldest hand transforms are trusted, reset the untrusted predictions counter.
        if (previousHandTransforms[handId].Peek().isTrusted && isTrusted)
        {
            untrustedPredictions[handId] = 0;
        }

        // If the current transform is untrusted, increase the untrusted counter.
        if (!isTrusted)
        {
            ++untrustedPredictions[handId];
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

    public void SetThresholdColors(Scalar lower, Scalar upper) { }
}
