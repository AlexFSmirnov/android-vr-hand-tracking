using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils.Helper;
using OpenCVForUnity.UnityUtils;

[RequireComponent (typeof(WebCamTextureToMatHelper))]
public class HandPositionEstimator : MonoBehaviour
{
    private WebCamTextureToMatHelper webCamTextureToMatHelper;

    private Texture2D texture;

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
        if (widthScale < heightScale) {
            Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
        } else {
            Camera.main.orthographicSize = height / 2;
        }
    }

    public void OnWebCamTextureToMatHelperDisposed ()
    {
        if (texture != null) {
            Texture2D.Destroy(texture);
            texture = null;
        }
    }

    void Update()
    {
        if (webCamTextureToMatHelper.IsPlaying() && webCamTextureToMatHelper.DidUpdateThisFrame()) {
            // Get an rgba material from the current camera frame.
            Mat rgbaMat = webCamTextureToMatHelper.GetMat();

            // Update Quad renderer texture with the processed rgba material.
            Utils.fastMatToTexture2D(rgbaMat, texture);
        }
    }
}
