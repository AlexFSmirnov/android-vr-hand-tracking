using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils.Helper;
using OpenCVForUnity.UnityUtils;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class HandPositionEstimatorAR : MonoBehaviour
{
    // private WebCamTextureToMatHelper webcamTextureToMatHelper;
    public ARCameraManager cameraManager;

    private GameObject previewCanvas;
    private RawImage previewImage;
    private Image colorPickerImage;

    private Texture2D previewTexture;

    private Point selectedPoint = null;

    void Start()
    {
        // webcamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper>();

        previewCanvas = gameObject.transform.Find("PreviewCanvas").gameObject;
        previewImage = previewCanvas.transform.Find("PreviewImage").GetComponent<RawImage>();
        colorPickerImage = previewCanvas.transform.Find("ColorPickerImage").GetComponent<Image>();

        // if (Application.platform == RuntimePlatform.Android) {
        //     webcamTextureToMatHelper.avoidAndroidFrontCameraLowLightIssue = true;
        // }

        // webcamTextureToMatHelper.Initialize();
    }

    // public void OnWebCamTextureToMatHelperInitialized()
    // {
    //     Mat frameMat = webcamTextureToMatHelper.GetMat();
    //     previewTexture = new Texture2D(frameMat.width(), frameMat.height(), TextureFormat.RGBA32, false);
    //     Utils.fastMatToTexture2D(frameMat, previewTexture);

    //     previewImage.texture = previewTexture;
    // }

    // public void OnWebCamTextureToMatHelperDisposed ()
    // {
    //     if (previewTexture != null)
    //     {
    //         Texture2D.Destroy(previewTexture);
    //         previewTexture = null;
    //     }
    // }

    unsafe void Update()
    {
        // Attempt to get the latest camera image. If this method succeeds,
        // it acquires a native resource that must be disposed (see below).
        if (!cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
        {
            return;
        }

        if (previewTexture == null || previewTexture.width != image.width || previewTexture.height != image.height)
        {
            previewTexture = new Texture2D(image.width, image.height, TextureFormat.RGBA32, false);
        }

        // Convert the image to format, flipping the image across the Y axis.
        // We can also get a sub rectangle, but we'll get the full image here.
        var conversionParams = new XRCpuImage.ConversionParams(image, TextureFormat.RGBA32, XRCpuImage.Transformation.MirrorX);

        // Texture2D allows us write directly to the raw texture data
        // This allows us to do the conversion in-place without making any copies.
        var rawTextureData = previewTexture.GetRawTextureData<byte>();
        try
        {
            image.Convert(conversionParams, new IntPtr(rawTextureData.GetUnsafePtr()), rawTextureData.Length);
        }
        finally
        {
            // We must dispose of the XRCpuImage after we're finished
            // with it to avoid leaking native resources.
            image.Dispose();
        }

        // Apply the updated texture data to our texture
        previewTexture.Apply();

        // Set the RawImage's texture so we can visualize it.
        previewImage.texture = previewTexture;

        // UpdateSelectedPoint();

        // // Align color picker image with the selected point.
        // if (selectedPoint != null) {
        //     // colorPickerImage.rectTransform.position = new Vector3((float)selectedPoint.x, (float)selectedPoint.y, 0);
        // } else {
        //     colorPickerImage.rectTransform.position = new Vector3(-1000, -1000, 0);
        // }

        // if (!webcamTextureToMatHelper.IsPlaying() || !webcamTextureToMatHelper.DidUpdateThisFrame())
        // {
        //     return;
        // }

        // // Get an rgba material from the current camera frame.
        // Mat frameMat = webcamTextureToMatHelper.GetMat();

        // if (selectedPoint != null) {
        //     var frameSelectedPoint = GetFramePointFromScreenPoint(selectedPoint, frameMat);

        //     Imgproc.circle(frameMat, frameSelectedPoint, 50, new Scalar(255, 0, 0, 255), 3);
        //     // Imgproc.circle(frameMat, new Point(100, 200), 50, new Scalar(255, 0, 0, 255), 3);
        // }


        // // Update Quad renderer texture with the processed frame material.
        // Utils.fastMatToTexture2D(frameMat, previewTexture);
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
