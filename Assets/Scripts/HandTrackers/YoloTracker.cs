using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.DnnModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.ImgcodecsModule;

public class YoloTracker : HandTracker
{
    // TODO: Move common fields to the HandTracker class
    private bool isInitialized = false;
    private Camera targetCamera;

    private Mat rgbMat;

    private Net yoloNet;
    private Size yoloInputSize = new Size(320, 320);
    private float confidenceThreshold = 0.5f;
    private float nmsThreshold = 0.4f;

    // TODO: Remove once using custom model;
    private string classesFilename = "coco.names";
    private List<string> classNames;
    private string configFilename = "yolov4-tiny.weights";
    private string modelFilename = "yolov4-tiny.cfg";

    private List<string> outBlobNames;
    private List<string> outBlobTypes;
    private Mat input;
    private List<Mat> outputs;

    public void Initialize(int frameWidth, int frameHeight, Camera camera)
    {
        targetCamera = camera;
        rgbMat = new Mat();

        string configFilepath = Utils.getFilePath("dnn/" + configFilename);
        string modelFilepath = Utils.getFilePath("dnn/" + modelFilename);

        classNames = readClassNames(Utils.getFilePath("dnn/" + classesFilename));

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
        // TODO: Improve the function to actually return the hand positions.
        postprocess();

        foreach (var output in outputs)
        {
            output.Dispose();
        }

        if (drawPreview)
        {
            Imgproc.cvtColor(rgbMat, rgbaMat, Imgproc.COLOR_RGB2RGBA);
        }
    }

    public void SetThresholdColors(Scalar lower, Scalar upper) { }

    protected virtual List<string> readClassNames(string filename)
    {
        List<string> classNames = new List<string>();

        System.IO.StreamReader cReader = null;
        try
        {
            cReader = new System.IO.StreamReader(filename, System.Text.Encoding.Default);

            while (cReader.Peek() >= 0)
            {
                string name = cReader.ReadLine();
                classNames.Add(name);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError(ex.Message);
            return null;
        }
        finally
        {
            if (cReader != null)
                cReader.Close();
        }

        return classNames;
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

    protected virtual void postprocess()
    {
        MatOfInt outLayers = yoloNet.getUnconnectedOutLayers();
        string outLayerType = outBlobTypes[0];

        List<int> classIdsList = new List<int>();
        List<float> confidencesList = new List<float>();
        List<Rect2d> boxesList = new List<Rect2d>();

        Debug.Log(outputs.Count);

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

        for (int idx = 0; idx < boxesList.Count; ++idx)
        {
            Rect2d box = boxesList[idx];
            // TODO: Update the function to have better naming and only draw required stuff.
            drawPred(classIdsList[idx], confidencesList[idx], box.x, box.y, box.x + box.width, box.y + box.height, rgbMat);
        }
    }

    protected virtual void drawPred(int classId, float conf, double left, double top, double right, double bottom, Mat frame)
    {
        Imgproc.rectangle(frame, new Point(left, top), new Point(right, bottom), new Scalar(0, 255, 0, 255), 2);

        string label = conf.ToString();
        if (classNames != null && classNames.Count != 0)
        {
            if (classId < (int)classNames.Count)
            {
                label = classNames[classId] + ": " + label;
            }
        }

        int[] baseLine = new int[1];
        Size labelSize = Imgproc.getTextSize(label, Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, 1, baseLine);

        top = Mathf.Max((float)top, (float)labelSize.height);
        Imgproc.rectangle(frame, new Point(left, top - labelSize.height),
            new Point(left + labelSize.width, top + baseLine[0]), Scalar.all(255), Core.FILLED);
        Imgproc.putText(frame, label, new Point(left, top), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar(0, 0, 0, 255));
    }
}
