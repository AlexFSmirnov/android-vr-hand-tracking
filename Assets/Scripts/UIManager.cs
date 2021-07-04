using UnityEngine;

public class UIManager : MonoBehaviour
{
    private StageManager stageManager;

    private GameObject colorPickerStage;

    void Start()
    {
        stageManager = GameObject.Find("StageManager").GetComponent<StageManager>();

        colorPickerStage = gameObject.transform.Find("ColorPickerStage").gameObject;
    }

    public void OnColorPickerContinueButtonClick()
    {
        stageManager.GoToNextStage();
        colorPickerStage.SetActive(false);
    }
}
