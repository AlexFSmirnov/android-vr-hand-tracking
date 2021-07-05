using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils;

public class HandPositionEstimator : MonoBehaviour
{
    public enum HandTrackerType { ArUco, ThresholdHSV, ThresholdLab, CamshiftHSV, CamshiftLab, OpenPose, Yolo3, Yolo3Tiny };
    public HandTrackerType handTrackerType = HandTrackerType.ArUco;

    public GameObject handObj;

    private StageManager stageManager;
    private Camera targetCamera;
    private CameraMatProvider cameraMatProvider;
    private HandTracker handTracker;

    private GameObject previewCanvas;
    private RawImage previewImage;
    private Image colorPickerImage;

    private Mat rgbaFrameMat;
    private Texture2D previewTexture;

    private Point selectedPoint = null;
    private int colorPickerRadius = 45;
    private ColorRange thresholdColorRange = null;

    void Start()
    {
        stageManager = GameObject.Find("StageManager").GetComponent<StageManager>();
        stageManager.SetFirstStage(handTrackerType);

        #if UNITY_EDITOR || UNITY_STANDALONE
        targetCamera = GameObject.Find("DesktopDebug/Camera").GetComponent<Camera>();
        cameraMatProvider = GameObject.Find("CameraMatProviders/DesktopContainer/DesktopCameraMatProvider").GetComponent<CameraMatProvider>();
        #else
        targetCamera = GameObject.Find("AR/AR Session Origin/AR Camera").GetComponent<Camera>();
        cameraMatProvider = GameObject.Find("CameraMatProviders/MobileContainer/MobileCameraMatProvider").GetComponent<CameraMatProvider>();
        #endif

        switch (handTrackerType)
        {
            case HandTrackerType.ArUco:
                handTracker = new ArUcoTracker();
                break;
            case HandTrackerType.ThresholdHSV:
                handTracker = new ThresholdTracker(useLab: false);
                break;
            case HandTrackerType.ThresholdLab:
                handTracker = new ThresholdTracker(useLab: true);
                break;
            case HandTrackerType.CamshiftHSV:
                handTracker = new CamshiftTracker(useLab: false);
                break;
            case HandTrackerType.CamshiftLab:
                handTracker = new CamshiftTracker(useLab: true);
                break;
            case HandTrackerType.OpenPose:
                handTracker = new OpenPoseTracker();
                break;
            case HandTrackerType.Yolo3:
                handTracker = new YoloTracker(tiny: false);
                break;
            case HandTrackerType.Yolo3Tiny:
                handTracker = new YoloTracker(tiny: true);
                break;
        }

        previewCanvas = gameObject.transform.Find("PreviewCanvas").gameObject;
        previewImage = previewCanvas.transform.Find("PreviewImage").GetComponent<RawImage>();
        colorPickerImage = previewCanvas.transform.Find("ColorPickerImage").GetComponent<Image>();
    }

    void OnDestroy()
    {
        handTracker.Dispose();

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
        // Get an OpenCV matrix from the current camera frame.
        rgbaFrameMat = cameraMatProvider.GetMat();
        if (rgbaFrameMat == null)
            return;

        // If using a tracker that requires a color picker, update it.
        if (stageManager.GetStage() == StageManager.Stage.ThresholdColorPicker)
        {
            UpdateColorPicker(rgbaFrameMat);
        }

        // Initialize the hand tracker, if not yet initialized.
        if (!handTracker.IsInitialized())
        {
            handTracker.Initialize(rgbaFrameMat.width(), rgbaFrameMat.height(), targetCamera);
            return;
        }

        bool drawPreview = stageManager.isDebug || stageManager.GetStage() == StageManager.Stage.ThresholdColorPicker;

        handTracker.GetHandPositions(rgbaFrameMat, out List<HandTransform> hands, drawPreview);

        if (stageManager.GetStage() == StageManager.Stage.Main)
        {

            // TODO: Improve hand objects - should support at least 2.
            if (hands.Count > 0) {
                handObj.transform.localPosition = hands[0].position;
            }
        }

        // Update (creating new if needed) the preview texture with the changed frame matrix and display it on the preview image.
        if (drawPreview)
        {
            if (previewTexture == null || rgbaFrameMat.width() != previewTexture.width || rgbaFrameMat.height() != previewTexture.height)
                previewTexture = new Texture2D(rgbaFrameMat.width(), rgbaFrameMat.height(), TextureFormat.RGBA32, false);

            Utils.fastMatToTexture2D(rgbaFrameMat, previewTexture);
            previewImage.texture = previewTexture;
        }
        else
        {
            previewImage.color = new Color(0, 0, 0, 0);
        }
    }

    private void UpdateColorPicker(Mat rgbaMat)
    {
        // Get new coords of the selected (touch or mouse) screen point.
        UpdateSelectedPoint();

        // Align color picker image with the selected point.
        if (selectedPoint != null)
        {
            colorPickerImage.rectTransform.position = new Vector3((float)selectedPoint.x, (float)selectedPoint.y, 0);

            var frameSelectedPoint = ScreenUtils.GetFramePointFromScreenPoint(selectedPoint, rgbaMat.width(), rgbaMat.height());
            thresholdColorRange = ColorUtils.GetColorRangeFromCircle(frameSelectedPoint, colorPickerRadius, rgbaMat);

            if (handTrackerType == HandTrackerType.ThresholdHSV || handTrackerType == HandTrackerType.CamshiftHSV)
                handTracker.SetThresholdColors(thresholdColorRange.hsvLower, thresholdColorRange.hsvUpper);
            else if (handTrackerType == HandTrackerType.ThresholdLab || handTrackerType == HandTrackerType.CamshiftLab)
                handTracker.SetThresholdColors(thresholdColorRange.labLower, thresholdColorRange.labUpper);

            colorPickerImage.color = new Color(
                (float)thresholdColorRange.rgbAverage.val[0] / 255,
                (float)thresholdColorRange.rgbAverage.val[1] / 255,
                (float)thresholdColorRange.rgbAverage.val[2] / 255
            );
        }
        else
        {
            colorPickerImage.rectTransform.position = new Vector3(-1000, -1000, 0);
        }
        
    }

    private void UpdateSelectedPoint()
    {
        selectedPoint = null;

        if (Application.platform == RuntimePlatform.Android)
        {
            if (Input.touchCount == 1)
            {
                Touch t = Input.GetTouch(0);
                selectedPoint = new Point(t.position.x, t.position.y);
            }
        }
        else
        {
            if (Input.GetMouseButton(0))
            {
                selectedPoint = new Point(Input.mousePosition.x, Input.mousePosition.y);
            }
        }

        // Ignore the point if it is inside a button.
        if (selectedPoint != null)
        {
            var raycastResults = new List<RaycastResult>();
            var pointerEventData = new PointerEventData(EventSystem.current);
            pointerEventData.position = new Vector2((float)selectedPoint.x, (float)selectedPoint.y);

            EventSystem.current.RaycastAll(pointerEventData, raycastResults);

            foreach (var result in raycastResults)
            {
                if (result.gameObject.name.Contains("Button"))
                {
                    selectedPoint = null;
                    return;
                }
            }
        }
    }
}
