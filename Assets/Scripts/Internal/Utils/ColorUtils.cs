using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;

public class ColorRange
{
    public Scalar hsvLower;
    public Scalar hsvUpper;
    public Scalar labLower;
    public Scalar labUpper;
    public Scalar rgbAverage;

    public ColorRange(Scalar hsvLower, Scalar hsvUpper, Scalar labLower, Scalar labUpper, Scalar rgbAverage)
    {
        this.hsvLower = hsvLower;
        this.hsvUpper = hsvUpper;
        this.labLower = labLower;
        this.labUpper = labUpper;
        this.rgbAverage = rgbAverage;
    }
}

public static class ColorUtils 
{
    public static ColorRange GetColorRangeFromCircle(Point center, int radius, Mat rgbaMat)
    {
        var rgbMat = new Mat();
        var hsvMat = new Mat();
        var labMat = new Mat();

        Imgproc.cvtColor(rgbaMat, rgbMat, Imgproc.COLOR_RGBA2RGB);
        Imgproc.cvtColor(rgbMat, hsvMat, Imgproc.COLOR_RGB2HSV);
        Imgproc.cvtColor(rgbMat, labMat, Imgproc.COLOR_RGB2Lab);

        int minX = Mathf.RoundToInt(Mathf.Max(0, (float)center.x - radius));
        int maxX = Mathf.RoundToInt(Mathf.Min(hsvMat.width(), (float)center.x + radius));
        int minY = Mathf.RoundToInt(Mathf.Max(0, (float)center.y - radius));
        int maxY = Mathf.RoundToInt(Mathf.Min(hsvMat.height(), (float)center.y + radius));

        // HSV
        var allHues = new List<float>();
        var allSaturations = new List<float>();
        var allValues = new List<float>();

        // Lab
        var allLightness = new List<float>();
        var allAlphas = new List<float>();
        var allBetas = new List<float>();

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
                    var labColor = new Scalar(labMat.get(y, x));
                    var rgbColor = new Scalar(rgbMat.get(y, x));

                    allHues.Add((float)hsvColor.val[0]);
                    allSaturations.Add((float)hsvColor.val[1]);
                    allValues.Add((float)hsvColor.val[2]);

                    allLightness.Add((float)labColor.val[0]);
                    allAlphas.Add((float)labColor.val[1]);
                    allBetas.Add((float)labColor.val[2]);

                    totalR += (float)rgbColor.val[0];
                    totalG += (float)rgbColor.val[1];
                    totalB += (float)rgbColor.val[2];
                    totalPixels += 1;
                }
            }
        }

        var (minHue, maxHue) = GetPaddedBounds(allHues, 10, isPaddingMultiplicative: false);
        var (minSat, maxSat) = GetPaddedBounds(allSaturations, 2, isPaddingMultiplicative: true);
        var (minVal, maxVal) = GetPaddedBounds(allValues, 2, isPaddingMultiplicative: true);
        var hsvLower = new Scalar(minHue, minSat, minVal);
        var hsvUpper = new Scalar(maxHue, maxSat, maxVal);

        var (minL, maxL) = GetPaddedBounds(allLightness, 2f, isPaddingMultiplicative: true);
        var (minA, maxA) = GetPaddedBounds(allAlphas, 10, isPaddingMultiplicative: false);
        var (minB, maxB) = GetPaddedBounds(allBetas, 10, isPaddingMultiplicative: false);
        var labLower = new Scalar(minL, minA, minB);
        var labUpper = new Scalar(maxL, maxA, maxB);

        var rgbAverage = new Scalar(
            Mathf.RoundToInt(totalR / totalPixels),
            Mathf.RoundToInt(totalG / totalPixels),
            Mathf.RoundToInt(totalB / totalPixels)
        );

        rgbMat.Dispose();
        hsvMat.Dispose();
        labMat.Dispose();

        return new ColorRange(hsvLower, hsvUpper, labLower, labUpper, rgbAverage);
    }

    private static (float, float) GetPaddedBounds(List<float> values, float padding, bool isPaddingMultiplicative = false)
    {
        values.Sort();

        // Pick the highest and lowest values of the middle third.
        // |----------|x--------x|----------|
        float min = values[Mathf.FloorToInt(values.Count / 3)];
        float max = values[Mathf.FloorToInt(values.Count / 3 * 2)];

        float paddedMin;
        float paddedMax;

        if (isPaddingMultiplicative)
        {
            paddedMin = min / padding;
            paddedMax = max * padding;
        }
        else
        {
            paddedMin = min - padding;
            paddedMax = max + padding;
        }

        return (paddedMin, paddedMax);
    }
}
