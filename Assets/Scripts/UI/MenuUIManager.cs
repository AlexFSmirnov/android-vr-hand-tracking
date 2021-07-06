using UnityEngine;
using TMPro;

public class MenuUIManager : MonoBehaviour
{
    private GameManager gameManager;

    private TMP_Dropdown trackerDropdown;

    void Start()
    {
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        trackerDropdown = gameObject.transform.Find("TrackerDropdownContainer/TrackerDropdown").GetComponent<TMP_Dropdown>();
    }

    public void OnStartButtonClick()
    {
        gameManager.SwitchToMainScene();
    }

    public void OnTrackerDropdownChanged()
    {
        switch (trackerDropdown.value)
        {
            case 0:
                gameManager.SetHandTrackerType(GameManager.HandTrackerType.ArUco);
                break;
            case 1:
                gameManager.SetHandTrackerType(GameManager.HandTrackerType.ThresholdHSV);
                break;
            case 2:
                gameManager.SetHandTrackerType(GameManager.HandTrackerType.ThresholdLab);
                break;
            case 3:
                gameManager.SetHandTrackerType(GameManager.HandTrackerType.CamshiftHSV);
                break;
            case 4:
                gameManager.SetHandTrackerType(GameManager.HandTrackerType.CamshiftLab);
                break;
            case 5:
                gameManager.SetHandTrackerType(GameManager.HandTrackerType.OpenPose);
                break;
            case 6:
                gameManager.SetHandTrackerType(GameManager.HandTrackerType.Yolo3);
                break;
            case 7:
                gameManager.SetHandTrackerType(GameManager.HandTrackerType.Yolo3Tiny);
                break;
        }
    }
}
