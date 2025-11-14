// TitleUIController.cs
using UnityEngine;

public class TitleUIController : MonoBehaviour
{
    public void OnClickStart()
    {
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.StartGame();
        }
        else
        {
            Debug.LogError("[TitleUIController] GameFlowManager.Instance 가 없습니다.");
        }
    }

    public void OnClickExit()
    {
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.ExitGame();
        }
        else
        {
            Debug.LogError("[TitleUIController] GameFlowManager.Instance 가 없습니다.");
        }
    }
}

