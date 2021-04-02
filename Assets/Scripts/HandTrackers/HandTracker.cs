using System.Collections.Generic;
using OpenCVForUnity.CoreModule;

interface HandTracker
{
    void Initialize();
    void Dispose();

    void GetHandPositions(Mat rgbaMat, out List<HandInfo> hands, bool drawPreview = false);
}
