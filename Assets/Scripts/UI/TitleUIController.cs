// TitleUIController.cs
using UnityEngine;

public class TitleUIController : MonoBehaviour
{
    public void OnClickStart()
    {
        if (GameFlowManager.Instance != null)
        {
            // 🔁 바로 Stage1로 가지 않고, 먼저 스토리 씬으로 이동
            GameFlowManager.Instance.GoToStoryBeforeStage1();
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

