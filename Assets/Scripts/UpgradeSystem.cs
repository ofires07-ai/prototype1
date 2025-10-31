using UnityEngine;

public class UpgradeSystem : MonoBehaviour
{
    // SCV 업그레이드 버튼이 호출할 함수입니다.
    public void PurchaseSCVUpgrade(int tierIndex)
    {
        // TODO: (당신이 할 일) 4등급 자원 시스템에 맞춰 TrySpendResource 함수 호출 로직 구현

        // 임시 로그: 버튼이 정상 작동하는지 확인
        Debug.Log("SCV Upgrade Tier " + (tierIndex + 1) + " 버튼 클릭 감지됨.");
        
        // if (GameManager.Instance.TrySpendResource(1, cost)) { ... }
    }
}
