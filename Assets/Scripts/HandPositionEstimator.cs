using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;

public class HandPositionEstimator : MonoBehaviour
{
    public enum HandTrackerType { ArUco, Threshold };
    public HandTrackerType handTrackerType = HandTrackerType.ArUco;

    public GameObject handObj;

    private Camera targetCamera;
    private CameraMatProvider cameraMatProvider;
    private HandTracker handTracker;

    private GameObject previewCanvas;
    private RawImage previewImage;
    private Image colorPickerImage;

    private Mat rgbaFrameMat;
    private Texture2D previewTexture;

    private Point selectedPoint = null;

    void Start()
    {
        #if UNITY_EDITOR || UNITY_STANDALONE
        targetCamera = GameObject.Find("DesktopDebug/Camera").GetComponent<Camera>();
        cameraMatProvider = GameObject.Find("CameraMatProviders/DesktopContainer/DesktopCameraMatProvider").GetComponent<CameraMatProvider>();
        #else
        targetCamera = GameObject.Find("AR/AR Session Origin/AR Camera").GetComponent<Camera>();
        cameraMatProvider = GameObject.Find("CameraMatProviders/MobileContainer/MobileCameraMatProvider").GetComponent<CameraMatProvider>();
        #endif

        handTracker = new ArUcoTracker();

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
        if (handTrackerType == HandTrackerType.Threshold)
        {
            // Get new coords of the selected (touch or mouse) screen point.
            UpdateSelectedPoint();

            // Align color picker image with the selected point.
            if (selectedPoint != null)
                colorPickerImage.rectTransform.position = new Vector3((float)selectedPoint.x, (float)selectedPoint.y, 0);
            else
                colorPickerImage.rectTransform.position = new Vector3(-1000, -1000, 0);
        }

        // Get an OpenCV matrix from the current camera frame.
        rgbaFrameMat = cameraMatProvider.GetMat();
        if (rgbaFrameMat == null)
            return;

        if (!handTracker.IsInitialized())
        {
            handTracker.Initialize(rgbaFrameMat.width(), rgbaFrameMat.height(), targetCamera);
            return;
        }

        // TODO: Sample colors from the selected point.
        if (selectedPoint != null && handTrackerType == HandTrackerType.Threshold)
        {
            var frameSelectedPoint = ScreenUtils.GetFramePointFromScreenPoint(selectedPoint, rgbaFrameMat.width(), rgbaFrameMat.height());
            Imgproc.circle(rgbaFrameMat, frameSelectedPoint, 50, new Scalar(255, 0, 0, 255), 3);
        }

        handTracker.GetHandPositions(rgbaFrameMat, out List<HandTransform> hands, true);

        // TODO: Improve hand objects - should support at least 2.
        if (hands.Count > 0) {
            // handObj.GetComponent<Rigidbody>().MovePosition(hands[0].position);
            handObj.transform.localPosition = hands[0].position;
            // handObj.transform.eulerAngles = hands[0].rotation;
        }

        // Update (creating new if needed) the preview texture with the changed frame matrix and display it on the preview image.
        if (previewTexture == null || rgbaFrameMat.width() != previewTexture.width || rgbaFrameMat.height() != previewTexture.height)
            previewTexture = new Texture2D(rgbaFrameMat.width(), rgbaFrameMat.height(), TextureFormat.RGBA32, false);

        Utils.fastMatToTexture2D(rgbaFrameMat, previewTexture);
        previewImage.texture = previewTexture;
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
    }
}
