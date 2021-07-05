using UnityEngine;

public class UIManager : MonoBehaviour
{
    private GameManager gameManager;

    private GameObject colorPickerStage;

    void Start()
    {
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();

        colorPickerStage = gameObject.transform.Find("ColorPickerStage").gameObject;
    }

    public void OnColorPickerContinueButtonClick()
    {
        gameManager.GoToNextStage();
        colorPickerStage.SetActive(false);
    }
}
