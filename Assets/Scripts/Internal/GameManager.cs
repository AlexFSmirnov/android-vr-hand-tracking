using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;

public class GameManager : MonoBehaviour
{
    public enum HandTrackerType { ArUco, ThresholdHSV, ThresholdLab, CamshiftHSV, CamshiftLab, OpenPose, Yolo3, Yolo3Tiny };
    public enum Stage { Menu, ColorPicker, WorldInstantiation, Main };

    private HandTrackerType handTrackerType = HandTrackerType.ArUco;
    private Stage stage;

    // TODO: Reset values
    // TODO: Move new movement controller to Main
    // TODO: Move new World to Main
    // TODO: Move new splitscreen settings to main
    private bool isDebug = false;
    private bool useSplitscreen = false;

    private GameObject uiCanvas;
    private GameObject uiColorPickerStage;
    private FpsMonitor fpsMonitor;
    private CanvasGroup splitscreenCanvasGroup;
    private RawImage fullscreenPreviewImage;
    private RawImage splitscreenLeftEyePreviewImage;
    private RawImage splitscreenRightEyePreviewImage;

    void Awake()
    {
        DontDestroyOnLoad(transform.gameObject);
    }

    void Start()
    {
        stage = Stage.Menu;
    }

    public void SwitchToMainScene()
    {
        SceneManager.LoadScene("Main");
    }

    public void SwitchToMenuScene()
    {
        SetStage(Stage.Menu);
        SetHandTrackerType(HandTrackerType.ArUco);
        GameObject.Find("AR").transform.Find("AR Session Origin").GetComponent<ARPlaneManager>().enabled = false;
        SceneManager.LoadScene("StartMenu");
    }

    public void SetStage(Stage stage)
    {
        this.stage = stage;

        // Enable the color picker ui elements once the ColorPicker stage is active.
        if (stage == Stage.ColorPicker)
        {
            uiColorPickerStage.SetActive(true);
        }

        // Enabling the AR Plane Manager allows the user to see the detected planes
        // and use the RaycastWorldInstantiator.
        if (stage == Stage.WorldInstantiation)
        {
            GameObject.Find("AR").transform.Find("AR Session Origin").GetComponent<ARPlaneManager>().enabled = true;
        }

        // The main stage only requires turning on splitscreen (if enabled).
        if (stage == Stage.Main)
        {
            if (useSplitscreen)
            {
                splitscreenCanvasGroup.alpha = 1;
            }
        }

        UpdateCameraPreviewTexturesVisibility();
    }

    public Stage GetStage()
    {
        return stage;
    }

    public void SetFirstVRStage()
    {
        // This function is called once the application enters the VR scene,
        // so we are safe to find all the objects used in that scene.
        uiCanvas = GameObject.Find("UICanvas");
        uiColorPickerStage = uiCanvas.transform.Find("ColorPickerStage").gameObject;
        fpsMonitor = uiCanvas.transform.Find("FpsMonitor").GetComponent<FpsMonitor>();
        splitscreenCanvasGroup = GameObject.Find("Splitscreen").GetComponent<CanvasGroup>();
        fullscreenPreviewImage = GameObject.Find("HandPositionEstimator/PreviewCanvas/PreviewImage").GetComponent<RawImage>();
        splitscreenLeftEyePreviewImage = GameObject.Find("Splitscreen/SplitscreenCanvas/CameraPreviewImages/LeftEyeMask/LeftEyePreviewImage").GetComponent<RawImage>();
        splitscreenRightEyePreviewImage = GameObject.Find("Splitscreen/SplitscreenCanvas/CameraPreviewImages/RightEyeMask/RightEyePreviewImage").GetComponent<RawImage>();

        // Update visibility of debug-related objects.
        SetIsDebug(isDebug);

        switch (handTrackerType)
        {
            case HandTrackerType.ArUco:
            case HandTrackerType.OpenPose:
            case HandTrackerType.Yolo3:
            case HandTrackerType.Yolo3Tiny:
                // Skip world instantiation for desktop and standalone.
                #if UNITY_EDITOR || UNITY_STANDALONE
                SetStage(Stage.Main);
                #else
                SetStage(Stage.WorldInstantiation);
                #endif
                break;

            case HandTrackerType.ThresholdHSV:
            case HandTrackerType.ThresholdLab:
            case HandTrackerType.CamshiftHSV:
            case HandTrackerType.CamshiftLab:
                SetStage(Stage.ColorPicker);
                break;
        }
    }

    public void GoToNextStage()
    {
        switch (stage)
        {
            case Stage.ColorPicker:
                // Skip world instantiation for desktop and standalone.
                #if UNITY_EDITOR || UNITY_STANDALONE
                SetStage(Stage.Main);
                #else
                SetStage(Stage.WorldInstantiation);
                #endif
                break;
            
            case Stage.WorldInstantiation:
                SetStage(Stage.Main);
                break;
        }
    }

    public HandTrackerType GetHandTrackerType()
    {
        return handTrackerType;
    }

    public void SetHandTrackerType(HandTrackerType newType)
    {
        handTrackerType = newType;
    }

    public bool GetIsDebug()
    {
        return isDebug;
    }

    public void SetIsDebug(bool newValue)
    {
        isDebug = newValue;

        if (!uiCanvas)
            return;

        if (isDebug)
            fpsMonitor.Run();
        else
            fpsMonitor.Stop();

        UpdateCameraPreviewTexturesVisibility();
    }

    public bool ShouldShowPreview()
    {
        return isDebug || (stage == Stage.ColorPicker || stage == Stage.WorldInstantiation);
    }

    public bool GetUseSplitscreen()
    {
        return useSplitscreen;
    }

    public void SetUseSplitscreen(bool newValue)
    {
        useSplitscreen = newValue;

        if (splitscreenCanvasGroup)
            splitscreenCanvasGroup.alpha = useSplitscreen ? 1 : 0;
    }

    private void UpdateCameraPreviewTexturesVisibility()
    {
        if (!fullscreenPreviewImage || !splitscreenLeftEyePreviewImage || !splitscreenRightEyePreviewImage)
            return;

        var cameraPreviewImageColor = ShouldShowPreview() ? new Color(255, 255, 255, 0.5f) : new Color(0, 0, 0, 0);
        fullscreenPreviewImage.color = cameraPreviewImageColor;
        splitscreenLeftEyePreviewImage.color = cameraPreviewImageColor;
        splitscreenRightEyePreviewImage.color = cameraPreviewImageColor;
    }
}
