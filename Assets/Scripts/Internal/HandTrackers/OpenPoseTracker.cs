using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.DnnModule;
using OpenCVForUnity.UnityUtils;

public class OpenPoseTracker : HandTracker
{
    private bool isInitialized = false;
    private Camera targetCamera;
    private Mat rgbMat;

    private Net openPoseNet = null;
    private string caffemodelFilename = "pose_iter_102000.caffemodel";
    private string prototxtFilename = "pose_deploy.prototxt";

    private Dictionary<string, int> bodyParts;
    private string[,] posePairs;

    public void Initialize(int frameWidth, int frameHeight, Camera camera)
    {
        targetCamera = camera;
        rgbMat = new Mat();

        bodyParts = new Dictionary<string, int>() {{ "Wrist", 0 },
            { "ThumbMetacarpal", 1 },{ "ThumbProximal", 2 },{ "ThumbMiddle", 3 },{ "ThumbDistal", 4 },
            { "IndexFingerMetacarpal", 5 }, {"IndexFingerProximal", 6 },{ "IndexFingerMiddle", 7 },{ "IndexFingerDistal", 8 },
            { "MiddleFingerMetacarpal", 9 },{ "MiddleFingerProximal", 10 },{ "MiddleFingerMiddle", 11 },{ "MiddleFingerDistal", 12 },
            { "RingFingerMetacarpal", 13 },{ "RingFingerProximal", 14 },{ "RingFingerMiddle", 15 },{ "RingFingerDistal", 16 },
            { "LittleFingerMetacarpal", 17 }, {"LittleFingerProximal", 18 }, {"LittleFingerMiddle", 19 },{ "LittleFingerDistal", 20 }
        };

        posePairs = new string[,] { {"Wrist", "ThumbMetacarpal"}, {"ThumbMetacarpal", "ThumbProximal"},
            {"ThumbProximal", "ThumbMiddle"}, {"ThumbMiddle", "ThumbDistal"},
            {"Wrist", "IndexFingerMetacarpal"}, {"IndexFingerMetacarpal", "IndexFingerProximal"},
            {"IndexFingerProximal", "IndexFingerMiddle"}, {"IndexFingerMiddle", "IndexFingerDistal"},
            {"Wrist", "MiddleFingerMetacarpal"}, {"MiddleFingerMetacarpal", "MiddleFingerProximal"},
            {"MiddleFingerProximal", "MiddleFingerMiddle"}, {"MiddleFingerMiddle", "MiddleFingerDistal"},
            {"Wrist", "RingFingerMetacarpal"}, {"RingFingerMetacarpal", "RingFingerProximal"},
            {"RingFingerProximal", "RingFingerMiddle"}, {"RingFingerMiddle", "RingFingerDistal"},
            {"Wrist", "LittleFingerMetacarpal"}, {"LittleFingerMetacarpal", "LittleFingerProximal"},
            {"LittleFingerProximal", "LittleFingerMiddle"}, {"LittleFingerMiddle", "LittleFingerDistal"} };

        var caffemodelFilepath = Utils.getFilePath("dnn/" + caffemodelFilename);
        var prototxtFilepath = Utils.getFilePath("dnn/" + prototxtFilename);

        if (string.IsNullOrEmpty(caffemodelFilepath) || string.IsNullOrEmpty(prototxtFilepath))
        {
            Debug.LogError(caffemodelFilepath + " or " + prototxtFilepath + " could not be loaded.");
        }
        else
        {
            openPoseNet = Dnn.readNet(caffemodelFilepath, prototxtFilepath);
        }

        if (openPoseNet == null)
        {
            Debug.LogError("Model could not be loaded.");
        }
        else
        {
            isInitialized = true;
        }
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
    }

    public void GetHandPositions(Mat rgbaMat, out List<HandTransform> hands, bool drawPreview = false)
    {
        hands = new List<HandTransform>();

        Imgproc.cvtColor(rgbaMat, rgbMat, Imgproc.COLOR_RGBA2RGB);
        float frameWidth = rgbMat.cols();
        float frameHeight = rgbMat.rows();

        // Load the current frame into the OpenPose network and run predictions.
        Mat input = Dnn.blobFromImage(rgbMat, 1.0f / 255f, new Size(368, 368), new Scalar(0, 0, 0), false, false);
        openPoseNet.setInput(input);
        Mat output = openPoseNet.forward();

        // Get positions of predicted hand key points.
        List<Point> points = new List<Point>();
        float[] data = new float[output.size(2) * output.size(3)];
        output = output.reshape(1, output.size(1));
        for (int i = 0; i < bodyParts.Count; i++)
        {
            output.get(i, 0, data);

            Mat heatMap = new Mat(1, data.Length, CvType.CV_32FC1);
            heatMap.put(0, 0, data);

            //Originally, we try to find all the local maximums. To simplify a sample
            //we just find a global one. However only a single pose at the same time
            //could be detected this way.
            Core.MinMaxLocResult result = Core.minMaxLoc(heatMap);

            heatMap.Dispose();

            double x = (frameWidth * (result.maxLoc.x % 46)) / 46;
            double y = (frameHeight * (result.maxLoc.x / 46)) / 46;

            if (result.maxVal > 0.1)
            {
                points.Add(new Point(x, y));
            }
            else
            {
                points.Add(null);
            }
        }

        // For each pose pair (edge), draw a preview line.
        for (int i = 0; i < posePairs.GetLength(0); i++)
        {
            string partFrom = posePairs[i, 0];
            string partTo = posePairs[i, 1];

            int idFrom = bodyParts[partFrom];
            int idTo = bodyParts[partTo];

            if (points[idFrom] != null && points[idTo] != null)
            {
                Imgproc.line(rgbMat, points[idFrom], points[idTo], new Scalar(0, 255, 0), 3);
                Imgproc.circle(rgbMat, points[idFrom], 3, new Scalar(0, 0, 255), Core.FILLED);
                Imgproc.circle(rgbMat, points[idTo], 3, new Scalar(0, 0, 255), Core.FILLED);
            }
        }

        if (drawPreview)
        {
            Imgproc.cvtColor(rgbMat, rgbaMat, Imgproc.COLOR_RGB2RGBA);
        }
    }

    public void SetThresholdColors(Scalar lower, Scalar upper) { }
}
