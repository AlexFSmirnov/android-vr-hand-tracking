using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils.Helper;
using OpenCVForUnity.UnityUtils;

[RequireComponent (typeof(WebCamTextureToMatHelper))]
public class HandPositionEstimator : MonoBehaviour
{
    private WebCamTextureToMatHelper webcamTextureToMatHelper;

    private GameObject previewCanvas;
    private RawImage previewImage;
    private Image colorPickerImage;

    private Texture2D previewTexture;

    private Point selectedPoint = null;

    void Start()
    {
        webcamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper>();

        previewCanvas = gameObject.transform.Find("PreviewCanvas").gameObject;
        previewImage = previewCanvas.transform.Find("PreviewImage").GetComponent<RawImage>();
        colorPickerImage = previewCanvas.transform.Find("ColorPickerImage").GetComponent<Image>();

        if (Application.platform == RuntimePlatform.Android) {
            webcamTextureToMatHelper.avoidAndroidFrontCameraLowLightIssue = true;
        }

        webcamTextureToMatHelper.Initialize();
    }

    public void OnWebCamTextureToMatHelperInitialized()
    {
        Mat frameMat = webcamTextureToMatHelper.GetMat();
        previewTexture = new Texture2D(frameMat.width(), frameMat.height(), TextureFormat.RGBA32, false);
        Utils.fastMatToTexture2D(frameMat, previewTexture);

        previewImage.texture = previewTexture;
    }

    public void OnWebCamTextureToMatHelperDisposed ()
    {
        if (previewTexture != null)
        {
            Texture2D.Destroy(previewTexture);
            previewTexture = null;
        }
    }

    void Update()
    {
        UpdateSelectedPoint();

        // Align color picker image with the selected point.
        if (selectedPoint != null) {
            // colorPickerImage.rectTransform.position = new Vector3((float)selectedPoint.x, (float)selectedPoint.y, 0);
        } else {
            colorPickerImage.rectTransform.position = new Vector3(-1000, -1000, 0);
        }

        if (!webcamTextureToMatHelper.IsPlaying() || !webcamTextureToMatHelper.DidUpdateThisFrame())
        {
            return;
        }

        // Get an rgba material from the current camera frame.
        Mat frameMat = webcamTextureToMatHelper.GetMat();

        if (selectedPoint != null) {
            var frameSelectedPoint = GetFramePointFromScreenPoint(selectedPoint, frameMat);

            Imgproc.circle(frameMat, frameSelectedPoint, 50, new Scalar(255, 0, 0, 255), 3);
            // Imgproc.circle(frameMat, new Point(100, 200), 50, new Scalar(255, 0, 0, 255), 3);
        }


        // Update Quad renderer texture with the processed frame material.
        Utils.fastMatToTexture2D(frameMat, previewTexture);
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
            if (Input.GetMouseButton(0)) {
                selectedPoint = new Point(Input.mousePosition.x, Input.mousePosition.y);
            }
            // if (Input.GetMouseButtonUp(0) && !EventSystem.current.IsPointerOverGameObject())
            // {
            //     clickedPoint = new Point(Input.mousePosition.x, Input.mousePosition.y);
            // }
        }
    }

    private Point GetFramePointFromScreenPoint(Point screenPoint, Mat frameMat) {
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
