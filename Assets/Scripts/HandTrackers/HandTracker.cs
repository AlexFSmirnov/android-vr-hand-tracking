using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using UnityEngine;

interface HandTracker
{
    void Initialize(int frameWidth, int frameHeight, Camera targetCamera);
    bool IsInitialized();
    void Dispose();

    void GetHandPositions(Mat rgbaMat, out List<HandTransform> hands, bool drawPreview = false);
}
