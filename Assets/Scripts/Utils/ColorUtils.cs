using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;

public class ColorRange
{
    public Scalar hsvLower;
    public Scalar hsvUpper;
    public Scalar rgbAverage;

    public ColorRange(Scalar hsvLower, Scalar hsvUpper, Scalar rgbAverage)
    {
        this.hsvLower = hsvLower;
        this.hsvUpper = hsvUpper;
        this.rgbAverage = rgbAverage;
    }
}

public static class ColorUtils 
{
    public static ColorRange GetColorRangeFromCircle(Point center, int radius, Mat rgbaMat)
    {
        var rgbMat = new Mat();
        var hsvMat = new Mat();

        Imgproc.cvtColor(rgbaMat, rgbMat, Imgproc.COLOR_RGBA2RGB);
        Imgproc.cvtColor(rgbMat, hsvMat, Imgproc.COLOR_RGB2HSV);

        int minX = Mathf.RoundToInt(Mathf.Max(0, (float)center.x - radius));
        int maxX = Mathf.RoundToInt(Mathf.Min(hsvMat.width(), (float)center.x + radius));
        int minY = Mathf.RoundToInt(Mathf.Max(0, (float)center.y - radius));
        int maxY = Mathf.RoundToInt(Mathf.Min(hsvMat.height(), (float)center.y + radius));

        var allHues = new List<float>();
        var allSaturations = new List<float>();
        var allValues = new List<float>();

        float totalR = 0;
        float totalG = 0;
        float totalB = 0;

        float totalPixels = 0;

        for (int y = minY; y < maxY; ++y)
        {
            for (int x = minX; x < maxX; ++x)
            {
                int deltaX = x - (int)center.x;
                int deltaY = y - (int)center.y;

                if (Mathf.Sqrt(deltaX * deltaX + deltaY * deltaY) <= radius)
                {
                    var hsvColor = new Scalar(hsvMat.get(y, x));
                    var rgbColor = new Scalar(rgbMat.get(y, x));

                    allHues.Add((float)hsvColor.val[0]);
                    allSaturations.Add((float)hsvColor.val[1]);
                    allValues.Add((float)hsvColor.val[2]);

                    totalR += (float)rgbColor.val[0];
                    totalG += (float)rgbColor.val[1];
                    totalB += (float)rgbColor.val[2];
                    totalPixels += 1;
                }
            }
        }

        allHues.Sort();
        allSaturations.Sort();
        allValues.Sort();

        // Pick the highest and lowest values of the middle third of the sorted hues and saturations.
        // |----------|x--------x|----------|
        float minHue = allHues[Mathf.FloorToInt(allHues.Count / 3)];
        float maxHue = allHues[Mathf.FloorToInt(allHues.Count / 3 * 2)];
        float minSat = allSaturations[Mathf.FloorToInt(allSaturations.Count / 3)];
        float maxSat = allSaturations[Mathf.FloorToInt(allSaturations.Count / 3 * 2)];
        float minVal = allValues[Mathf.FloorToInt(allValues.Count / 3)];
        float maxVal = allValues[Mathf.FloorToInt(allValues.Count / 3 * 2)];

        // Pad hues and saturations to achieve a bigger color range.
        float paddedMinHue = Mathf.Max(0, minHue - 10);
        float paddedMaxHue = Mathf.Min(179, maxHue + 10);
        float paddedMinSat = Mathf.Max(0, minSat * 0.75f);
        float paddedMaxSat = Mathf.Min(255, maxSat * 1.5f);
        float paddedMinVal = Mathf.Max(0, minVal * 0.75f);
        float paddedMaxVal = Mathf.Min(255, maxVal * 1.5f);

        var hsvLower = new Scalar(paddedMinHue, paddedMinSat, paddedMinVal);
        var hsvUpper = new Scalar(paddedMaxHue, paddedMaxSat, paddedMaxVal);

        var rgbAverage = new Scalar(
            Mathf.RoundToInt(totalR / totalPixels),
            Mathf.RoundToInt(totalG / totalPixels),
            Mathf.RoundToInt(totalB / totalPixels)
        );

        rgbMat.Dispose();
        hsvMat.Dispose();

        return new ColorRange(hsvLower, hsvUpper, rgbAverage);
    }
}
