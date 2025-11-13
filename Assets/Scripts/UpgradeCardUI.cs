using UnityEngine;
using System;

public class UpgradeCardUI : MonoBehaviour
{
    private Action<int> _onSelected;

    // StageSceneController에서 호출
    public void Open(Action<int> onSelected)
    {
        _onSelected = onSelected;
        gameObject.SetActive(true);
    }

    // 각 카드 버튼에서 호출
    public void SelectCard(int index)
    {
        // index에 따라 다른 업그레이드를 선택했다고 보면 됨
        _onSelected?.Invoke(index);
        gameObject.SetActive(false);
    }
}
