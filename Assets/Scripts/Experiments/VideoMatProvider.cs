using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.VideoioModule;

public class VideoMatProvider 
{
    private VideoCapture capture;
    private Mat rgbaMat;

    private bool isFirstFrame = true;

    public VideoMatProvider(string filename)
    {
        capture = new VideoCapture(Utils.getFilePath(filename));
    }

    public Mat GetMat()
    {
        rgbaMat = new Mat();
        capture.read(rgbaMat);

        Imgproc.cvtColor(rgbaMat, rgbaMat, Imgproc.COLOR_BGRA2RGBA);

        return rgbaMat;
    }

    public bool GetIsFirstFrame()
    {
        if (!isFirstFrame)
            return false;

        isFirstFrame = false;
        return true;
    }

    public void Dispose()
    {
        if (rgbaMat != null)
        {
            rgbaMat.Dispose();
            rgbaMat = null;
        }

        capture.release();
    }
}
