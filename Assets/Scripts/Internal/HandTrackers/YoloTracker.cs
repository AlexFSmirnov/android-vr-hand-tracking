using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.DnnModule;
using OpenCVForUnity.UnityUtils;

public class YoloTracker : HandTracker
{
    private bool isInitialized = false;
    private Camera targetCamera;
    private int frameWidth;
    private int frameHeight;
    private Mat rgbMat;

    private float handZDistance = 0.5f;

    private Net yoloNet;
    private Size yoloInputSize = new Size(416, 416);
    private float confidenceThreshold = 0.3f;
    private float nmsThreshold = 0.4f;

    private List<string> classNames = new List<string> { "hand" };
    private string configFilename;
    private string modelFilename;

    private List<string> outBlobNames;
    private List<string> outBlobTypes;
    private Mat input;
    private List<Mat> outputs;

    public YoloTracker(bool tiny=false)
    {
        if (tiny)
        {
            configFilename = "cross-hands-tiny-prn.weights";
            modelFilename = "cross-hands-tiny-prn.cfg";
        }
        else
        {
            configFilename = "cross-hands.weights";
            modelFilename = "cross-hands.cfg";
        }
    }

    public void Initialize(int frameWidth, int frameHeight, Camera camera)
    {
        targetCamera = camera;
        this.frameWidth = frameWidth;
        this.frameHeight = frameHeight;
        rgbMat = new Mat();

        string configFilepath = Utils.getFilePath("dnn/" + configFilename);
        string modelFilepath = Utils.getFilePath("dnn/" + modelFilename);

        yoloNet = Dnn.readNet(modelFilepath, configFilepath);
        outBlobNames = getOutputsNames(yoloNet);
        outBlobTypes = getOutputsTypes(yoloNet);

        isInitialized = true;
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

        if (input != null)
        {
            input.Dispose();
            input = null;
        }

        if (outputs.Count > 0)
        {
            foreach (var output in outputs)
            {
                output.Dispose();
            }
            outputs = new List<Mat>();
        }
    }

    public void GetHandPositions(Mat rgbaMat, out List<HandTransform> hands, bool drawPreview = false)
    {
        hands = new List<HandTransform>();

        Imgproc.cvtColor(rgbaMat, rgbMat, Imgproc.COLOR_RGBA2RGB);

        // Create an input blob from the current frame.
        input = Dnn.blobFromImage(rgbMat, 1.0f / 255f, yoloInputSize, new Scalar(0, 0, 0), false, false);

        // Run YOLO prediction.
        yoloNet.setInput(input);
        outputs = new List<Mat>();
        yoloNet.forward(outputs, outBlobNames);

        // Get the predicted hand positions.
        List<(Point, float)> pointsWithConfidence = GetPredictedPointsWithConfidence(drawPreview);

        foreach (var (centerPoint, confidence) in pointsWithConfidence)
        {
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
            Imgproc.cvtColor(rgbMat, rgbaMat, Imgproc.COLOR_RGB2RGBA);
        }

        foreach (var output in outputs)
        {
            output.Dispose();
        }
    }

    private List<string> getOutputsNames(Net net)
    {
        List<string> names = new List<string>();

        MatOfInt outLayers = net.getUnconnectedOutLayers();
        for (int i = 0; i < outLayers.total(); ++i)
        {
            names.Add(net.getLayer(new DictValue((int)outLayers.get(i, 0)[0])).get_name());
        }
        outLayers.Dispose();

        return names;
    }

    private List<string> getOutputsTypes(Net net)
    {
        List<string> types = new List<string>();

        MatOfInt outLayers = net.getUnconnectedOutLayers();
        for (int i = 0; i < outLayers.total(); ++i)
        {
            types.Add(net.getLayer(new DictValue((int)outLayers.get(i, 0)[0])).get_type());
        }
        outLayers.Dispose();

        return types;
    }

    private List<(Point, float)> GetPredictedPointsWithConfidence(bool drawPreview)
    {
        MatOfInt outLayers = yoloNet.getUnconnectedOutLayers();
        string outLayerType = outBlobTypes[0];

        List<int> classIdsList = new List<int>();
        List<float> confidencesList = new List<float>();
        List<Rect2d> boxesList = new List<Rect2d>();

        for (int i = 0; i < outputs.Count; ++i)
        {
            // Network produces output blob with a shape NxC where N is a number of
            // detected objects and C is a number of classes + 4 where the first 4
            // numbers are [center_x, center_y, width, height]

            float[] positionData = new float[5];
            float[] confidenceData = new float[outputs[i].cols() - 5];
            for (int p = 0; p < outputs[i].rows(); p++)
            {
                outputs[i].get(p, 0, positionData);
                outputs[i].get(p, 5, confidenceData);

                int maxIdx = confidenceData.Select((val, idx) => new { V = val, I = idx }).Aggregate((max, working) => (max.V > working.V) ? max : working).I;
                float confidence = confidenceData[maxIdx];
                if (confidence > confidenceThreshold)
                {
                    float centerX = positionData[0] * rgbMat.cols();
                    float centerY = positionData[1] * rgbMat.rows();
                    float width = positionData[2] * rgbMat.cols();
                    float height = positionData[3] * rgbMat.rows();
                    float left = centerX - width / 2;
                    float top = centerY - height / 2;

                    classIdsList.Add(maxIdx);
                    confidencesList.Add((float)confidence);
                    boxesList.Add(new Rect2d(left, top, width, height));
                }
            }
        }

        // // Non-maximum suppression is required if number of outputs > 1
        if (outLayers.total() > 1)
        {
            Dictionary<int, List<int>> class2indices = new Dictionary<int, List<int>>();
            for (int i = 0; i < classIdsList.Count; i++)
            {
                if (confidencesList[i] >= confidenceThreshold)
                {
                    if (!class2indices.ContainsKey(classIdsList[i]))
                        class2indices.Add(classIdsList[i], new List<int>());

                    class2indices[classIdsList[i]].Add(i);
                }
            }

            List<Rect2d> nmsBoxesList = new List<Rect2d>();
            List<float> nmsConfidencesList = new List<float>();
            List<int> nmsClassIdsList = new List<int>();
            foreach (int key in class2indices.Keys)
            {
                List<Rect2d> localBoxesList = new List<Rect2d>();
                List<float> localConfidencesList = new List<float>();
                List<int> classIndicesList = class2indices[key];
                for (int i = 0; i < classIndicesList.Count; i++)
                {
                    localBoxesList.Add(boxesList[classIndicesList[i]]);
                    localConfidencesList.Add(confidencesList[classIndicesList[i]]);
                }

                using (MatOfRect2d localBoxes = new MatOfRect2d(localBoxesList.ToArray()))
                using (MatOfFloat localConfidences = new MatOfFloat(localConfidencesList.ToArray()))
                using (MatOfInt nmsIndices = new MatOfInt())
                {
                    Dnn.NMSBoxes(localBoxes, localConfidences, confidenceThreshold, nmsThreshold, nmsIndices);
                    for (int i = 0; i < nmsIndices.total(); i++)
                    {
                        int idx = (int)nmsIndices.get(i, 0)[0];
                        nmsBoxesList.Add(localBoxesList[idx]);
                        nmsConfidencesList.Add(localConfidencesList[idx]);
                        nmsClassIdsList.Add(key);
                    }
                }
            }

            boxesList = nmsBoxesList;
            classIdsList = nmsClassIdsList;
            confidencesList = nmsConfidencesList;
        }

        // Get a list of center points from detected boxes.
        List<(Point, float)> pointsWithConfidence = new List<(Point, float)>();
        for (int i = 0; i < boxesList.Count; ++i)
        {
            Rect2d box = boxesList[i];
            Point centerPoint = new Point((double)(box.x + box.width / 2), (double)(box.y + box.height / 2));
            float confidence = confidencesList[i];

            pointsWithConfidence.Add((centerPoint, confidence));

            if (drawPreview)
            {
                drawBox(box, confidence, rgbMat);
            }
        }

        // Sort points in order of decreasing confidence.
        pointsWithConfidence.Sort(ComparePointsWithConfidence);

        return pointsWithConfidence;
    }

    private void drawBox(Rect2d box, float confidence, Mat frame)
    {
        double left = box.x;
        double top = box.y;
        double right = box.x + box.width;
        double bottom = box.y + box.height;

        Imgproc.rectangle(frame, new Point(left, top), new Point(right, bottom), new Scalar(0, 255, 0, 255), 2);

        string label = confidence.ToString();

        int[] baseLine = new int[1];
        Size labelSize = Imgproc.getTextSize(label, Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, 1, baseLine);

        top = Mathf.Max((float)top, (float)labelSize.height);
        Imgproc.rectangle(frame, new Point(left, top - labelSize.height), new Point(left + labelSize.width, top + baseLine[0]), Scalar.all(255), Core.FILLED);
        Imgproc.putText(frame, label, new Point(left, top), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar(0, 0, 0, 255));
    }

    private int ComparePointsWithConfidence((Point, float) first, (Point, float) second)
    {
        var (_point1, confidence1) = first;
        var (_point2, confidence2) = second;

        if (confidence1 == confidence2)
            return 0;
        else if (confidence1 > confidence2)
            return -1;
        else
            return 1;
    }

    public void SetThresholdColors(Scalar lower, Scalar upper) { }
}
