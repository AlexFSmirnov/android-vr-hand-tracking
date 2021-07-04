using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class GameManager : MonoBehaviour
{
    public enum Stage { Init, ThresholdColorPicker, WorldInstantiation, Main };
    private Stage stage;

    public bool isDebug = true;

    void Start()
    {
        stage = Stage.Init;
    }

    public void SetStage(Stage stage)
    {
        this.stage = stage;
    }

    public Stage GetStage()
    {
        return stage;
    }

    public void GoToNextStage()
    {
        switch (stage)
        {
            case Stage.ThresholdColorPicker:
                // Skip world instantiation for desktop and standalone.
                #if UNITY_EDITOR || UNITY_STANDALONE
                stage = Stage.Main;
                #else
                stage = Stage.WorldInstantiation;
                #endif
                break;
            
            case Stage.WorldInstantiation:
                stage = Stage.Main;
                break;
        }

        if (stage == Stage.WorldInstantiation)
        {
            GameObject.Find("AR").transform.Find("AR Session Origin").GetComponent<ARPlaneManager>().enabled = true;
        }
    }
}
