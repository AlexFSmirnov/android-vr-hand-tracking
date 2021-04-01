using UnityEngine;

[ExecuteAlways]
public class PlatformVisibilityManager : MonoBehaviour
{
    public enum Platform { Mobile, Desktop };
    public Platform showOnlyOn = Platform.Mobile;

    void Awake()
    {
        #if UNITY_EDITOR || UNITY_STANDALONE
        SetChildrenActive(showOnlyOn == Platform.Desktop);
        #else
        SetChildrenActive(showOnlyOn == Platform.Mobile);
        #endif
    }

    private void SetChildrenActive(bool isActive)
    {
        for (int i = 0; i < gameObject.transform.childCount; ++i)
        {
            gameObject.transform.GetChild(i).gameObject.SetActive(isActive);
        }
    }
}
