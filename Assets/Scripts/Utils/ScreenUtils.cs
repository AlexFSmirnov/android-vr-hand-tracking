using UnityEngine;
using OpenCVForUnity.CoreModule;

public static class ScreenUtils
{
    public static Vector2 GetScreenPointFromFramePoint(Point framePoint, double frameWidth, double frameHeight)
    {
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        float screenAspectRatio = screenWidth / screenHeight;
        float frameAspectRatio = (float)frameWidth / (float)frameHeight;

        bool isFullWidth = screenAspectRatio >= frameAspectRatio;

        float normalizedScreenWidth = isFullWidth ? screenWidth : screenHeight * frameAspectRatio;
        float normalizedScreenHeight = isFullWidth ? screenWidth / frameAspectRatio : screenHeight;

        var scaledPoint = new Vector2(
            (float)(framePoint.x / frameWidth * normalizedScreenWidth),
            normalizedScreenHeight - (float)(framePoint.y / frameHeight * normalizedScreenHeight)
        );

        return new Vector2(
            scaledPoint.x - (normalizedScreenWidth - screenWidth) / 2,
            scaledPoint.y - (normalizedScreenHeight - screenHeight) / 2
        );
    }

    public static Point GetFramePointFromScreenPoint(Point screenPoint, double frameWidth, double frameHeight)
    {
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        float screenAspectRatio = screenWidth / screenHeight;
        float frameAspectRatio = (float)frameWidth / (float)frameHeight;

        bool isFullWidth = screenAspectRatio >= frameAspectRatio;

        float normalizedScreenWidth = isFullWidth ? screenWidth : screenHeight * frameAspectRatio;
        float normalizedScreenHeight = isFullWidth ? screenWidth / frameAspectRatio : screenHeight;

        var offsetPoint = new Point(
            screenPoint.x + (normalizedScreenWidth - screenWidth) / 2,
            screenPoint.y + (normalizedScreenHeight - screenHeight) / 2
        );

        return new Point(
            offsetPoint.x / normalizedScreenWidth * frameWidth,
            frameHeight - (offsetPoint.y / normalizedScreenHeight * frameHeight)
        );
    }

    public static Vector2 GetScreenPointFromFramePoint(Vector2 framePoint, double frameWidth, double frameHeight)
    {
        return GetScreenPointFromFramePoint(new Point(framePoint.x, framePoint.y), frameWidth, frameHeight);
    }

    public static Point GetFramePointFromScreenPoint(Vector2 screenPoint, double frameWidth, double frameHeight)
    {
        return GetFramePointFromScreenPoint(new Point(screenPoint.x, screenPoint.y), frameWidth, frameHeight);
    }
}
