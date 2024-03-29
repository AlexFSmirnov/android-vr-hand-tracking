using UnityEngine;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;

[RequireComponent (typeof(WebCamTextureToMatHelper))]
public class DesktopCameraMatProvider : CameraMatProvider
{
    private WebCamTextureToMatHelper webcamTextureToMatHelper;
    private Mat frameMat;

    void Start()
    {
        webcamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper>();

        #if UNITY_ANDROID && !UNITY_EDITOR
        webcamTextureToMatHelper.avoidAndroidFrontCameraLowLightIssue = true;
        #endif

        webcamTextureToMatHelper.Initialize();
    }

    public void OnWebCamTextureToMatHelperInitialized()
    {
        frameMat = webcamTextureToMatHelper.GetMat();
        cameraTexture = new Texture2D(frameMat.width(), frameMat.height(), TextureFormat.RGBA32, false);

        Utils.fastMatToTexture2D(frameMat, cameraTexture);
    }

    public void OnWebCamTextureToMatHelperDisposed ()
    {
        if (cameraTexture != null)
        {
            Texture2D.Destroy(cameraTexture);
            cameraTexture = null;
        }

        if (frameMat != null)
        {
            frameMat.Dispose();
            frameMat = null;
        }
    }

    void Update()
    {
        if (webcamTextureToMatHelper.IsPlaying() && webcamTextureToMatHelper.DidUpdateThisFrame())
        {
            frameMat = webcamTextureToMatHelper.GetMat();
            Utils.fastMatToTexture2D(frameMat, cameraTexture);
        }
    }
}
