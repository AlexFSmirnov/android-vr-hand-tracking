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

    public RawImage previewImage;
    private Texture2D previewTexture;
    private Point clickedPoint = null;

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
        UpdateClickedPoint();

        if (!webcamTextureToMatHelper.IsPlaying() || !webcamTextureToMatHelper.DidUpdateThisFrame()) {
            return;
        }

        // Get an rgba material from the current camera frame.
        Mat frameMat = webcamTextureToMatHelper.GetMat();

        // Debug.Log($"{frameMat.rows()} {frameMat.cols()}");

        // TODO: Sample color from the clicked point.
        if (clickedPoint != null)
        {
            Debug.Log(clickedPoint);
            clickedPoint = null;
        }


        // Update Quad renderer texture with the processed frame material.
        Utils.fastMatToTexture2D(frameMat, previewTexture);
    }

    private void UpdateClickedPoint()
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            if (Input.touchCount == 1)
            {
                Touch t = Input.GetTouch(0);
                if (t.phase == TouchPhase.Ended && !EventSystem.current.IsPointerOverGameObject(t.fingerId))
                {
                    clickedPoint = new Point(t.position.x, t.position.y);
                }
            }
        }
        else
        {
            if (Input.GetMouseButtonUp(0) && !EventSystem.current.IsPointerOverGameObject())
            {
                clickedPoint = new Point(Input.mousePosition.x, Input.mousePosition.y);
            }
        }
    }
}
