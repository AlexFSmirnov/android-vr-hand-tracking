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
    public RawImage previewImage;
    public Image colorPickerImage;

    private WebCamTextureToMatHelper webcamTextureToMatHelper;

    private Texture2D previewTexture;

    private Point selectedPoint = null;

    void Start()
    {
        webcamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper>();

        if (Application.platform == RuntimePlatform.Android) {
            webcamTextureToMatHelper.avoidAndroidFrontCameraLowLightIssue = true;
        }

        webcamTextureToMatHelper.Initialize();
    }

    public void OnWebCamTextureToMatHelperInitialized()
    {
        Mat webcamTextureMat = webcamTextureToMatHelper.GetMat();
        previewTexture = new Texture2D(webcamTextureMat.width(), webcamTextureMat.height(), TextureFormat.RGBA32, false);
        Utils.fastMatToTexture2D(webcamTextureMat, previewTexture);

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
            Debug.Log(selectedPoint);
            colorPickerImage.rectTransform.position = new Vector3((float)selectedPoint.x, (float)selectedPoint.y, 0);
        } else {
            colorPickerImage.rectTransform.position = new Vector3(-1000, -1000, 0);
        }

        if (!webcamTextureToMatHelper.IsPlaying() || !webcamTextureToMatHelper.DidUpdateThisFrame())
        {
            return;
        }

        // Get an rgba material from the current camera frame.
        Mat frameMat = webcamTextureToMatHelper.GetMat();

        Imgproc.circle(frameMat, new Point(100, 200), 50, new Scalar(255, 0, 0, 255), 3);


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

    // private Point GetTexturePointFromScreenPoint(Point screenPoint) {

    // }
}
