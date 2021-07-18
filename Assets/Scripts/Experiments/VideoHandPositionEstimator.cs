using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.ImgprocModule;

public class VideoHandPositionEstimator : MonoBehaviour
{
    public GameManager.HandTrackerType handTrackerType;
    public string filename;
    public bool manualFrameByFrame;

    private Camera targetCamera;
    private VideoMatProvider videoMatProvider;
    private HandTracker handTracker;

    private GameObject previewCanvas;
    private RawImage previewImage;
    private bool drawPreview = true;

    private Mat rgbaFrameMat;
    private Texture2D previewTexture;

    private int colorPickerRadius = 45;

    private bool isWaitingForDecision = false;
    private int totalFrames = 0;
    private int correctFrames = 0;  // Detected in the right spot
    private int incorrectFrames = 0;  // Detected in the wrong spot
    private int missedFrames = 0;  // Not detected

    void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 30;
    }

    void Start()
    {
        targetCamera = GameObject.Find("Camera").GetComponent<Camera>();
        videoMatProvider = new VideoMatProvider(filename);

        switch (handTrackerType)
        {
            case GameManager.HandTrackerType.ArUco:
                handTracker = new ArUcoTracker();
                break;
            case GameManager.HandTrackerType.ThresholdHSV:
                handTracker = new ThresholdTracker(useLab: false);
                break;
            case GameManager.HandTrackerType.ThresholdLab:
                handTracker = new ThresholdTracker(useLab: true);
                break;
            case GameManager.HandTrackerType.CamshiftHSV:
                handTracker = new CamshiftTracker(useLab: false);
                break;
            case GameManager.HandTrackerType.CamshiftLab:
                handTracker = new CamshiftTracker(useLab: true);
                break;
            case GameManager.HandTrackerType.OpenPose:
                handTracker = new OpenPoseTracker();
                break;
            case GameManager.HandTrackerType.Yolo3:
                handTracker = new YoloTracker(tiny: false);
                break;
            case GameManager.HandTrackerType.Yolo3Tiny:
                handTracker = new YoloTracker(tiny: true);
                break;
        }

        previewCanvas = gameObject.transform.Find("PreviewCanvas").gameObject;
        previewImage = previewCanvas.transform.Find("PreviewImage").GetComponent<RawImage>();
    }

    void OnDestroy()
    {
        handTracker.Dispose();
        videoMatProvider.Dispose();

        if (previewTexture != null)
        {
            Texture2D.Destroy(previewTexture);
            previewTexture = null;
        }

        if (rgbaFrameMat != null)
        {
            rgbaFrameMat.Dispose();
            rgbaFrameMat = null;
        }
    }

    void Update()
    {
        if (manualFrameByFrame && isWaitingForDecision)
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                ++correctFrames;
                ++totalFrames;
                isWaitingForDecision = false;
            }
            else if (Input.GetKeyDown(KeyCode.W))
            {
                ++incorrectFrames;
                ++totalFrames;
                isWaitingForDecision = false;
            }
            else if (Input.GetKeyDown(KeyCode.E))
            {
                ++missedFrames;
                ++totalFrames;
                isWaitingForDecision = false;
            }
            return;
        }
        Debug.Log($"[{totalFrames}] C: {correctFrames} ({GetPercentage(correctFrames, totalFrames)})  I: {incorrectFrames} ({GetPercentage(incorrectFrames, totalFrames)})  M: {missedFrames} ({GetPercentage(missedFrames, totalFrames)})");   
        
        // Get an OpenCV matrix from the current video frame.
        rgbaFrameMat = videoMatProvider.GetMat();
        if (rgbaFrameMat == null || !rgbaFrameMat.isContinuous())
            return;

        // Initialize the hand tracker, if not yet initialized.
        if (!handTracker.IsInitialized())
        {
            handTracker.Initialize(rgbaFrameMat.width(), rgbaFrameMat.height(), targetCamera);
            return;
        }

        // Get the threshold colors from the center of the first frame.
        if (videoMatProvider.GetIsFirstFrame())
        {
            var centerPoint = new Point(rgbaFrameMat.width() / 2, rgbaFrameMat.height() / 2);
            var thresholdColorRange = ColorUtils.GetColorRangeFromCircle(centerPoint, colorPickerRadius, rgbaFrameMat);

            if (handTrackerType == GameManager.HandTrackerType.ThresholdHSV || handTrackerType == GameManager.HandTrackerType.CamshiftHSV)
                handTracker.SetThresholdColors(thresholdColorRange.hsvLower, thresholdColorRange.hsvUpper);
            else if (handTrackerType == GameManager.HandTrackerType.ThresholdLab || handTrackerType == GameManager.HandTrackerType.CamshiftLab)
                handTracker.SetThresholdColors(thresholdColorRange.labLower, thresholdColorRange.labUpper);
        }

        handTracker.GetHandPositions(rgbaFrameMat, out List<HandTransform> hands, drawPreview);

        // Preview of the color picker position.
        Imgproc.circle(rgbaFrameMat, new Point(rgbaFrameMat.width() / 2, rgbaFrameMat.height() / 2), colorPickerRadius, new Scalar(255, 0, 0, 255), 2);

        // Update (creating new if needed) the preview texture with the changed frame matrix and display it on the preview image.
        if (drawPreview)
        {
            if (previewTexture == null || rgbaFrameMat.width() != previewTexture.width || rgbaFrameMat.height() != previewTexture.height)
                previewTexture = new Texture2D(rgbaFrameMat.width(), rgbaFrameMat.height(), TextureFormat.RGBA32, false);

            Utils.fastMatToTexture2D(rgbaFrameMat, previewTexture);
            previewImage.texture = previewTexture;
        }

        isWaitingForDecision = true;
    }

    private string GetPercentage(int value, int total)
    {
        if (total == 0)
            return "0.00";
        
        return (value * 1.0f / total).ToString("0.00");
    }
}
