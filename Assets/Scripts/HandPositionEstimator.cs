using UnityEngine;
using UnityEngine.UI;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;

public class HandPositionEstimator : MonoBehaviour
{
    private CameraMatProvider cameraMatProvider;

    private GameObject previewCanvas;
    private RawImage previewImage;
    private Image colorPickerImage;

    private Texture2D previewTexture;

    private Point selectedPoint = null;

    void Start()
    {
        #if UNITY_EDITOR || UNITY_STANDALONE
        cameraMatProvider = GameObject.Find("CameraMatProviders/DesktopContainer/DesktopCameraMatProvider").GetComponent<CameraMatProvider>();
        #else
        cameraMatProvider = GameObject.Find("CameraMatProviders/MobileContainer/MobileCameraMatProvider").GetComponent<CameraMatProvider>();
        #endif

        previewCanvas = gameObject.transform.Find("PreviewCanvas").gameObject;
        previewImage = previewCanvas.transform.Find("PreviewImage").GetComponent<RawImage>();
        colorPickerImage = previewCanvas.transform.Find("ColorPickerImage").GetComponent<Image>();
    }

    void Update()
    {
        // Get new coords of the selected (touch or mouse) screen point.
        UpdateSelectedPoint();

        // Align color picker image with the selected point.
        if (selectedPoint != null)
            colorPickerImage.rectTransform.position = new Vector3((float)selectedPoint.x, (float)selectedPoint.y, 0);
        else
            colorPickerImage.rectTransform.position = new Vector3(-1000, -1000, 0);

        // Get an OpenCV material from the current camera frame.
        Mat frameMat = cameraMatProvider.GetMat();
        if (frameMat == null)
            return;

        // TODO: Perform hand detection and stuff.
        if (selectedPoint != null)
        {
            var frameSelectedPoint = GetFramePointFromScreenPoint(selectedPoint, frameMat);

            Imgproc.circle(frameMat, frameSelectedPoint, 50, new Scalar(255, 0, 0, 255), 3);
        }

        // Update (creating new if needed) the preview texture with the changed frame material and display it on the preview image.
        if (previewTexture == null || frameMat.width() != previewTexture.width || frameMat.height() != previewTexture.height)
            previewTexture = new Texture2D(frameMat.width(), frameMat.height(), TextureFormat.RGBA32, false);

        Utils.fastMatToTexture2D(frameMat, previewTexture);
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
                // if (t.phase == TouchPhase.Ended && !EventSystem.current.IsPointerOverGameObject(t.fingerId))
                // {
                //     clickedPoint = new Point(t.position.x, t.position.y);
                // }
            }
        }
        else
        {
            if (Input.GetMouseButton(0))
            {
                selectedPoint = new Point(Input.mousePosition.x, Input.mousePosition.y);
            }
            // if (Input.GetMouseButtonUp(0) && !EventSystem.current.IsPointerOverGameObject())
            // {
            //     clickedPoint = new Point(Input.mousePosition.x, Input.mousePosition.y);
            // }
        }
    }

    private Point GetFramePointFromScreenPoint(Point screenPoint, Mat frameMat)
    {
        var canvasRect = previewCanvas.GetComponent<RectTransform>();

        float canvasScale = canvasRect.localScale.x;
        float canvasWidth = canvasRect.sizeDelta.x;
        float canvasHeight = canvasRect.sizeDelta.y;

        float frameWidth = frameMat.width();
        float frameHeight = frameMat.height();

        var canvasPoint = screenPoint / canvasScale;
        var offsetPoint = new Point(
            canvasPoint.x + (frameWidth - canvasWidth) / 2,
            canvasPoint.y + (frameHeight - canvasHeight) / 2
        );

        return new Point(
            offsetPoint.x,
            frameHeight - offsetPoint.y
        );
    }
}
