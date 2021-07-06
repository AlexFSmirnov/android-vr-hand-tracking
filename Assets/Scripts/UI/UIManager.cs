using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    private GameManager gameManager;

    private GameObject settingsContainer;
    private GameObject colorPickerStageContainer;
    private Toggle debugToggle;
    private Toggle splitscreenToggle;

    private bool areSettingsVisible = false;

    void Start()
    {
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();

        settingsContainer = gameObject.transform.Find("Settings").gameObject;
        colorPickerStageContainer = gameObject.transform.Find("ColorPickerStage").gameObject;
        debugToggle = settingsContainer.transform.Find("DebugToggle").GetComponent<Toggle>();
        splitscreenToggle = settingsContainer.transform.Find("SplitscreenToggle").GetComponent<Toggle>();

        debugToggle.isOn = gameManager.GetIsDebug();
        splitscreenToggle.isOn = gameManager.GetUseSplitscreen();
    }

    public void OnSettingsButtonClick()
    {
        areSettingsVisible = !areSettingsVisible;

        if (areSettingsVisible)
            settingsContainer.GetComponent<RectTransform>().localPosition = new Vector3(0, 0, 0);
        else
            settingsContainer.GetComponent<RectTransform>().localPosition = new Vector3(100000, 0, 0);
    }

    public void OnDebugToggleChanged()
    {
        gameManager.SetIsDebug(debugToggle.isOn);
    }

    public void OnSplitscreenToggleChanged()
    {
        gameManager.SetUseSplitscreen(splitscreenToggle.isOn);
    }

    public void OnBackToMainMenuButtonClick()
    {
        gameManager.SwitchToMenuScene();
    }

    public void OnColorPickerContinueButtonClick()
    {
        gameManager.GoToNextStage();
        colorPickerStageContainer.SetActive(false);
    }
}
