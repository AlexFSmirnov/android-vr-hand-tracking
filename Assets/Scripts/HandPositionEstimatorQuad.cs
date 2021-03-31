using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils.Helper;
using OpenCVForUnity.UnityUtils;

[RequireComponent (typeof(WebCamTextureToMatHelper))]
public class HandPositionEstimatorQuad : MonoBehaviour
{
    private WebCamTextureToMatHelper webCamTextureToMatHelper;

    private Texture2D texture;
    private Point clickedPoint = null;

    void Start()
    {
        webCamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper>();
        webCamTextureToMatHelper.Initialize();
    }

    public void OnWebCamTextureToMatHelperInitialized()
    {
        Mat webCamTextureMat = webCamTextureToMatHelper.GetMat();

        texture = new Texture2D(webCamTextureMat.cols(), webCamTextureMat.rows(), TextureFormat.RGBA32, false);
        Utils.fastMatToTexture2D(webCamTextureMat, texture);

        gameObject.GetComponent<Renderer>().material.mainTexture = texture;

        gameObject.transform.localScale = new Vector3(webCamTextureMat.cols(), webCamTextureMat.rows(), 1);

        float width = webCamTextureMat.width();
        float height = webCamTextureMat.height();

        // TODO: This part most definitely needs to be reworked to support perspective camera.
        float widthScale = (float)Screen.width / width;
        float heightScale = (float)Screen.height / height;
        if (widthScale < heightScale)
        {
            Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
        }
        else
        {
            Camera.main.orthographicSize = height / 2;
        }
    }

    public void OnWebCamTextureToMatHelperDisposed ()
    {
        if (texture != null)
        {
            Texture2D.Destroy(texture);
            texture = null;
        }
    }

    void Update()
    {
        UpdateClickedPoint();

        if (!webCamTextureToMatHelper.IsPlaying() || !webCamTextureToMatHelper.DidUpdateThisFrame()) {
            return;
        }

        // Get an rgba material from the current camera frame.
        Mat frameMat = webCamTextureToMatHelper.GetMat();

        Debug.Log($"{frameMat.rows()} {frameMat.cols()}");

        // TODO: Sample color from the clicked point.
        if (clickedPoint != null)
        {
            Debug.Log(clickedPoint);
            clickedPoint = null;
        }


        // Update Quad renderer texture with the processed frame material.
        Utils.fastMatToTexture2D(frameMat, texture);
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
