using UnityEngine;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils;

public class CameraMatProvider : MonoBehaviour
{
    protected Texture2D cameraTexture = null;
    private Mat cameraMat = null;

    public Mat GetMat()
    {
        if (cameraTexture == null)
        {
            return null;
        }

        if (cameraMat == null || cameraMat.width() != cameraTexture.width || cameraMat.height() != cameraTexture.height)
        {
            cameraMat = new Mat(cameraTexture.height, cameraTexture.width, CvType.CV_8UC4, new Scalar(0, 0, 0, 255));
        }

        Utils.fastTexture2DToMat(cameraTexture, cameraMat);
        return cameraMat;
    }
}
