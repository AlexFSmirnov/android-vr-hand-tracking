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
    public Image previewImage;
    public Image colorPickerImage;

    private WebCamTextureToMatHelper webcamTextureToMatHelper;

    private Texture2D previewTexture;

    private Point selectedPoint = null;

    void Start()
    {
        webcamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper>();
        webcamTextureToMatHelper.Initialize();
    }

    public void OnWebCamTextureToMatHelperInitialized()
    {
        Mat webcamTextureMat = webcamTextureToMatHelper.GetMat();

        int webcamWidth = webcamTextureMat.width();
        int webcamHeight = webcamTextureMat.height();

        previewTexture = new Texture2D(webcamWidth, webcamHeight, TextureFormat.RGBA32, false);
        previewImage.material.mainTexture = previewTexture;

        if (((float)Screen.width / Screen.height) > ((float)webcamWidth / webcamHeight))  // Stretch preview image to full width (crop height)
        {
            previewImage.rectTransform.sizeDelta = new Vector2(
                Screen.width,
                (float)webcamHeight / (float)webcamWidth * Screen.width
            );
        }
        else  // Stretch preview image to full height (crop width)
        {
            previewImage.rectTransform.sizeDelta = new Vector2(
                (float)webcamWidth / (float)webcamHeight * Screen.height,
                Screen.height
            );
        }

        Utils.fastMatToTexture2D(webcamTextureMat, previewTexture);
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

        if (selectedPoint != null) {
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
}
